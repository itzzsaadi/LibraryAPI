using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LibraryAPI.Models;
using LibraryAPI.Services;

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
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10); // OTP 10 minutes ke liye valid rahega
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
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            // 5. Email bhejo
            await _emailService.SendEmailAsync(
                user.Email!,
                "New OTP - LibraryAPI",
                $@"
        <h2>New Verification Code</h2>
        <p>Your new verification code is:</p>
        <h1 style='color: #4F46E5; letter-spacing: 8px;'>{otp}</h1>
        <p>This code will expire in <strong>10 minutes</strong>.</p>
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

            // 3. Password sahi hai?
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordCorrect)
                return Unauthorized(new { message = "Invalid email or password" });

            // 4. Email verified hai?
            if (!user.EmailConfirmed)
                return Unauthorized(new { message = "Please verify your email before logging in." });

            // 5. User ka role lo
            var roles = await _userManager.GetRolesAsync(user);

            // 6. JWT Token banao
            var token = _jwtService.GenerateToken(user, roles);

            return Ok(new
            {
                token,
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
    }
}