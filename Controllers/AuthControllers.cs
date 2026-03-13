using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LibraryAPI.Models;
using LibraryAPI.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Member> _userManager;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        public AuthController(UserManager<Member> userManager, JwtService jwtService, EmailService emailService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. Model validation check
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 2. Email already exists?
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return Conflict(new { message = "Email already registered" });

            // 3. User object banao
            var user = new Member
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Phone = dto.Phone,
                JoinDate = DateTime.UtcNow
            };

            // 4. User save karo (password automatically hash hoga)
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 5. Default role assign karo
            await _userManager.AddToRoleAsync(user, "Member");

            // 6. OTP generate karo
            var otp = new Random().Next(100000, 999999).ToString();
            user.EmailOtp = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(1); // OTP 1 minutes ke liye valid rahega
            await _userManager.UpdateAsync(user);

            //8. OTP email bhejo
            await _emailService.SendEmailAsync(
        user.Email,
        "Verify Your Email - LibraryAPI",
        $@"
        <h2>Welcome to LibraryAPI, {user.FullName}!</h2>
        <p>Your verification code is:</p>
        <h1 style='color: #4F46E5; letter-spacing: 8px;'>{otp}</h1>
        <p>This code will expire in <strong>1 minute</strong>.</p>
        "
    );

            return Ok(new { message = "Please check your email to verify your account." });
        }
        // POST /api/auth/resend-otp
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. User dhundo
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // 2. Pehle se verified toh nahi?
            if (user.EmailConfirmed)
                return BadRequest(new { message = "Email already verified" });

            // 3. OTP abhi valid hai? Mat bhejo dobara
            if (user.OtpExpiry.HasValue && user.OtpExpiry > DateTime.UtcNow)
            {
                var remainingSeconds = (int)(user.OtpExpiry.Value - DateTime.UtcNow).TotalSeconds;
                return BadRequest(new { message = $"Please wait {remainingSeconds} seconds before requesting a new OTP." });
            }

            // 4. Naya OTP generate karo
            var otp = new Random().Next(100000, 999999).ToString();
            user.EmailOtp = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(1);
            await _userManager.UpdateAsync(user);

            // 5. Email bhejo
            await _emailService.SendEmailAsync(
                user.Email!,
                "New OTP - LibraryAPI",
                $@"
        <h2>New Verification Code</h2>
        <p>Your new verification code is:</p>
        <h1 style='color: #4F46E5; letter-spacing: 8px;'>{otp}</h1>
        <p>This code will expire in <strong>1 minute</strong>.</p>
        "
            );

            return Ok(new { message = "New OTP sent. Please check your email." });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 1. Model validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 2. User exist karta hai?
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            // ✅ Account lockout check (Pehle se lock toh nahi?)
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = user.LockoutEnd!.Value.UtcDateTime;
                var remainingMinutes = (int)(lockoutEnd - DateTime.UtcNow).TotalMinutes + 1;
                return Unauthorized(new { message = $"Account locked. Try again in {remainingMinutes} minutes." });
            }

            // 3. Password sahi hai?
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordCorrect)
            {
                // Failed attempt record karo (Database mein counter barhayega)
                await _userManager.AccessFailedAsync(user);

                // Check karo kya is attempt ke baad account abhi lock hua?
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return Unauthorized(new { message = "Too many failed attempts. Account locked for 15 minutes." });
                }

                // Baki bache huye attempts calculate karke user ko batao
                var attemptsLeft = 5 - await _userManager.GetAccessFailedCountAsync(user);
                return Unauthorized(new { message = $"Invalid email or password. {attemptsLeft} attempts remaining." });
            }

            // 4. Email verified hai?
            if (!user.EmailConfirmed)
                return Unauthorized(new { message = "Please verify your email before logging in." });

            // ✅ Login successful — Failed attempts reset karo (Taki next time 0 se shuru ho)
            await _userManager.ResetAccessFailedCountAsync(user);

            // 5. User ka role lo
            var roles = await _userManager.GetRolesAsync(user);

            // 6. JWT Token banao
            var token = _jwtService.GenerateToken(user, roles);

            // 7. Refresh Token banao aur save karo database mein
            var refreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // Refresh token 7 din ke liye valid rahega
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                accessToken = token,
                refreshToken,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    roles
                }
            });
        }
        // POST /api/auth/verify-otp
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. User dhundo
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // 2. Pehle se verified toh nahi?
            if (user.EmailConfirmed)
                return BadRequest(new { message = "Email already verified" });

            // 3. OTP sahi hai?
            if (user.EmailOtp != dto.Otp)
                return BadRequest(new { message = "Invalid OTP" });

            // 4. OTP expire toh nahi hua?
            if (user.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "OTP has expired. Please request a new one." });

            // 5. Account activate karo
            user.EmailConfirmed = true;
            user.EmailOtp = null;      // OTP delete karo
            user.OtpExpiry = null;     // Expiry delete karo
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Email verified successfully. You can now login." });
        }
        // POST /api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Security! — User mile ya na mile, same response bhejo
            // Warna hacker ko pata chalega ke email exist karta hai ya nahi
            if (user == null)
                return Ok(new { message = "If this email exists, a reset code has been sent." });

            // Rate limiting — OTP abhi valid hai?
            if (user.PasswordResetOtpExpiry.HasValue && user.PasswordResetOtpExpiry > DateTime.UtcNow)
            {
                var remainingSeconds = (int)(user.PasswordResetOtpExpiry.Value - DateTime.UtcNow).TotalSeconds;
                return BadRequest(new { message = $"Please wait {remainingSeconds} seconds before requesting a new code." });
            }

            // OTP generate karo
            var otp = new Random().Next(100000, 999999).ToString();
            user.PasswordResetOtp = otp;
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1);
            await _userManager.UpdateAsync(user);

            // Email bhejo
            await _emailService.SendEmailAsync(
                user.Email!,
                "Password Reset Code - LibraryAPI",
                $@"
        <h2>Password Reset Request</h2>
        <p>Your password reset code is:</p>
        <h1 style='color: #DC2626; letter-spacing: 8px;'>{otp}</h1>
        <p>This code will expire in <strong>1 minute</strong>.</p>
        <p>If you did not request this, please ignore this email.</p>
        "
            );

            return Ok(new { message = "If this email exists, a reset code has been sent." });
        }

        // POST /api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. User dhundo
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // 2. OTP sahi hai?
            if (user.PasswordResetOtp != dto.Otp)
                return BadRequest(new { message = "Invalid reset code" });

            // 3. OTP expire toh nahi hua?
            if (user.PasswordResetOtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Reset code has expired. Please request a new one." });

            // 4. Password reset karo
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 5. OTP delete karo
            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Password reset successful. You can now login." });
        }
        // POST /api/auth/change-password
        [Authorize]  // ✅ Sirf logged-in user access kar sakta hai
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Token se current user ka Id lo
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // 2. Purana password sahi hai?
            var isCorrect = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCorrect)
                return BadRequest(new { message = "Current password is incorrect" });

            // 3. Naya password purane jaisa toh nahi?
            if (dto.CurrentPassword == dto.NewPassword)
                return BadRequest(new { message = "New password must be different from current password" });

            // 4. Password change karo
            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Password changed successfully" });
        }
        // POST /api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Database mein Refresh Token dhundo
            var user = _userManager.Users
                .FirstOrDefault(u => u.RefreshToken == dto.RefreshToken);

            if (user == null)
                return Unauthorized(new { message = "Invalid refresh token" });

            // 2. Expire toh nahi hua?
            if (user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token has expired. Please login again." });

            // 3. Naya Access Token banao
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtService.GenerateToken(user, roles);

            // 4. Naya Refresh Token banao — Rotation!
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }
        // POST /api/auth/google
        [HttpPost("google")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 1. Google Token verify karo
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") }
                };

                var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

                // 2. User dhundo ya banao
                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = new Member
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        FullName = payload.Name,
                        EmailConfirmed = true, // Google ne verify kiya hai
                        JoinDate = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return BadRequest(createResult.Errors);

                    await _userManager.AddToRoleAsync(user, "Member");
                }

                // 3. JWT banao
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtService.GenerateToken(user, roles);

                var refreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                return Ok(new
                {
                    accessToken = token,
                    refreshToken,
                    isGoogleUser = true,
                    user = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        roles
                    }
                });
            }
            catch (Exception)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }
        }
    }
}