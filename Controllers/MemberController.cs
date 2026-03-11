using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;
using System.Security.Claims;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberController : ControllerBase
    {
        private readonly UserManager<Member> _userManager;
        private readonly LibraryDbContext _context;

        public MemberController(UserManager<Member> userManager, LibraryDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // =====================
        // ADMIN ENDPOINTS
        // =====================

        // GET /api/member — Sab members (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(m =>
                    m.FullName.ToLower().Contains(search) ||
                    m.Email!.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var members = await query
                .OrderBy(m => m.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MemberResponseDto
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email!,
                    Phone = m.Phone,
                    JoinDate = m.JoinDate,
                    MembershipType = m.MembershipType,
                    Status = m.Status,
                    MembershipExpiryDate = m.MembershipExpiryDate,
                    MaxBooksAllowed = m.MaxBooksAllowed,
                    CurrentBooksCount = m.CurrentBooksCount
                })
                .ToListAsync();

            return Ok(new
            {
                data = members,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            });
        }

        // GET /api/member/{id} — Ek member (Admin)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(string id)
        {
            var member = await _userManager.FindByIdAsync(id);

            if (member == null)
                return NotFound(new { message = "Member not found" });

            return Ok(new MemberResponseDto
            {
                Id = member.Id,
                FullName = member.FullName,
                Email = member.Email!,
                Phone = member.Phone,
                JoinDate = member.JoinDate,
                MembershipType = member.MembershipType,
                Status = member.Status,
                MembershipExpiryDate = member.MembershipExpiryDate,
                MaxBooksAllowed = member.MaxBooksAllowed,
                CurrentBooksCount = member.CurrentBooksCount
            });
        }

        // PUT /api/member/{id}/status — Status update (Admin)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusDto dto)
        {
            var member = await _userManager.FindByIdAsync(id);

            if (member == null)
                return NotFound(new { message = "Member not found" });

            member.Status = dto.Status;
            await _userManager.UpdateAsync(member);

            return Ok(new { message = $"Member status updated to {dto.Status}" });
        }

        // DELETE /api/member/{id} — Member delete (Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var member = await _userManager.FindByIdAsync(id);

            if (member == null)
                return NotFound(new { message = "Member not found" });

            // Active borrowed books hain?
            if (member.CurrentBooksCount > 0)
                return BadRequest(new { message = "Cannot delete member with borrowed books. Return books first." });

            await _userManager.DeleteAsync(member);

            return Ok(new { message = "Member deleted successfully" });
        }

        // =====================
        // MEMBER SELF-SERVICE
        // =====================

        // GET /api/member/profile — Apni profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = await _userManager.FindByIdAsync(userId!);

            if (member == null)
                return NotFound(new { message = "Member not found" });

            return Ok(new MemberResponseDto
            {
                Id = member.Id,
                FullName = member.FullName,
                Email = member.Email!,
                Phone = member.Phone,
                JoinDate = member.JoinDate,
                MembershipType = member.MembershipType,
                Status = member.Status,
                MembershipExpiryDate = member.MembershipExpiryDate,
                MaxBooksAllowed = member.MaxBooksAllowed,
                CurrentBooksCount = member.CurrentBooksCount
            });
        }

        // PUT /api/member/profile — Profile update
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] MemberProfileUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = await _userManager.FindByIdAsync(userId!);

            if (member == null)
                return NotFound(new { message = "Member not found" });

            member.FullName = dto.FullName;
            member.Phone = dto.Phone;
            await _userManager.UpdateAsync(member);

            return Ok(new { message = "Profile updated successfully" });
        }
    }
}