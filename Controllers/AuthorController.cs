using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public AuthorController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET /api/author — Sab authors
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var authors = await _context.Authors
                .Include(a => a.Books)
                .Select(a => new AuthorResponseDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Bio = a.Bio,
                    CreatedAt = a.CreatedAt,
                    TotalBooks = a.Books.Count
                })
                .ToListAsync();

            return Ok(authors);
        }

        // GET /api/author/1 — Ek author
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return NotFound(new { message = "Author not found" });

            var response = new AuthorResponseDto
            {
                Id = author.Id,
                Name = author.Name,
                Bio = author.Bio,
                CreatedAt = author.CreatedAt,
                TotalBooks = author.Books.Count
            };

            return Ok(response);
        }

        // POST /api/author — Naya author
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] AuthorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var author = new Author
            {
                Name = dto.Name,
                Bio = dto.Bio,
                CreatedAt = DateTime.UtcNow
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = author.Id }, new AuthorResponseDto
            {
                Id = author.Id,
                Name = author.Name,
                Bio = author.Bio,
                CreatedAt = author.CreatedAt,
                TotalBooks = 0
            });
        }

        // PUT /api/author/1 — Author update
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] AuthorUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var author = await _context.Authors.FindAsync(id);

            if (author == null)
                return NotFound(new { message = "Author not found" });

            author.Name = dto.Name;
            author.Bio = dto.Bio;

            await _context.SaveChangesAsync();

            return Ok(new AuthorResponseDto
            {
                Id = author.Id,
                Name = author.Name,
                Bio = author.Bio,
                CreatedAt = author.CreatedAt,
                TotalBooks = 0
            });
        }

        // DELETE /api/author/1 — Author delete
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return NotFound(new { message = "Author not found" });

            // Books bhi hain toh delete mat karo
            if (author.Books.Any())
                return BadRequest(new { message = "Cannot delete author with existing books. Remove books first." });

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Author deleted successfully" });
        }
    }
}