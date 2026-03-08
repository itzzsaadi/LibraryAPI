using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BookController(LibraryDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET /api/book?page=1&pageSize=10&search=harry
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            // Validation
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.Books
                .Include(b => b.Author)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(b =>
                    b.Title.ToLower().Contains(search) ||
                    b.ISBN.Contains(search) ||
                    b.Author!.Name.ToLower().Contains(search));
            }

            // Total count — pagination ke liye
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Actual data lo
            var books = await query
                .OrderBy(b => b.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookResponseDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    ISBN = b.ISBN,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    CoverImageUrl = b.CoverImagePath != null
                        ? $"{Request.Scheme}://{Request.Host}/uploads/covers/{b.CoverImagePath}"
                        : null,
                    CreatedAt = b.CreatedAt,
                    AuthorId = b.AuthorId,
                    AuthorName = b.Author!.Name
                })
                .ToListAsync();

            return Ok(new
            {
                data = books,
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
        // GET /api/book/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound(new { message = "Book not found" });

            return Ok(new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                CoverImageUrl = book.CoverImagePath != null
                    ? $"{Request.Scheme}://{Request.Host}/uploads/covers/{book.CoverImagePath}"
                    : null,
                CreatedAt = book.CreatedAt,
                AuthorId = book.AuthorId,
                AuthorName = book.Author!.Name
            });
        }

        // POST /api/book
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] BookCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Author exist karta hai?
            var author = await _context.Authors.FindAsync(dto.AuthorId);
            if (author == null)
                return NotFound(new { message = "Author not found" });

            // ISBN unique hai?
            var isbnExists = await _context.Books.AnyAsync(b => b.ISBN == dto.ISBN);
            if (isbnExists)
                return Conflict(new { message = "A book with this ISBN already exists" });

            // Cover image handle karo
            string? coverImagePath = null;
            if (dto.CoverImage != null)
                coverImagePath = await SaveImageAsync(dto.CoverImage);

            var book = new Book
            {
                Title = dto.Title,
                ISBN = dto.ISBN,
                AuthorId = dto.AuthorId,
                TotalCopies = dto.TotalCopies,
                AvailableCopies = dto.TotalCopies, // Shuru mein sab available
                CoverImagePath = coverImagePath,
                CreatedAt = DateTime.UtcNow
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = book.Id }, new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                CoverImageUrl = coverImagePath != null
                    ? $"{Request.Scheme}://{Request.Host}/uploads/covers/{coverImagePath}"
                    : null,
                CreatedAt = book.CreatedAt,
                AuthorId = book.AuthorId,
                AuthorName = author.Name
            });
        }

        // PUT /api/book/1
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] BookUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound(new { message = "Book not found" });

            // Naya cover image?
            if (dto.CoverImage != null)
            {
                // Purani image delete karo
                if (book.CoverImagePath != null)
                    DeleteImage(book.CoverImagePath);

                book.CoverImagePath = await SaveImageAsync(dto.CoverImage);
            }

            // TotalCopies kam ho gayi toh AvailableCopies bhi adjust karo
            var diff = dto.TotalCopies - book.TotalCopies;
            book.AvailableCopies = Math.Max(0, book.AvailableCopies + diff);

            book.Title = dto.Title;
            book.ISBN = dto.ISBN;
            book.TotalCopies = dto.TotalCopies;

            await _context.SaveChangesAsync();

            return Ok(new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                CoverImageUrl = book.CoverImagePath != null
                    ? $"{Request.Scheme}://{Request.Host}/uploads/covers/{book.CoverImagePath}"
                    : null,
                CreatedAt = book.CreatedAt,
                AuthorId = book.AuthorId,
                AuthorName = book.Author!.Name
            });
        }

        // DELETE /api/book/1
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);

            if (book == null)
                return NotFound(new { message = "Book not found" });

            // Cover image bhi delete karo
            if (book.CoverImagePath != null)
                DeleteImage(book.CoverImagePath);

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book deleted successfully" });
        }

        // ==================
        // Helper Methods
        // ==================

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            // Folder banao agar nahi hai
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "covers");
            Directory.CreateDirectory(uploadsFolder);

            // Unique file name — GUID use karo
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            return fileName;
        }

        private void DeleteImage(string fileName)
        {
            var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "covers", fileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}