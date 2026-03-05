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

        public AuthController(UserManager<Member> userManager, JwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
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

            return Ok(new { message = "Registration successful" });
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

            // 4. User ka role lo
            var roles = await _userManager.GetRolesAsync(user);

            // 5. JWT Token banao
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
    }
}