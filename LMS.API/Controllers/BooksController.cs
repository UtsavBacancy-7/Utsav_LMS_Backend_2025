using System.Security.Claims;
using Asp.Versioning;
using LMS_Backend.LMS.Application.DTOs.BookManagement;
using LMS_Backend.LMS.Application.Interfaces.BooksManagement;
using LMS_Backend.LMS.Application.Interfaces.PaginationServices;
using LMS_Backend.LMS.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/books")]
    [Produces("application/json")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly IPaginationService _paginationService;

        public BooksController(IBookService bookService, IPaginationService paginationService)
        {
            _bookService = bookService;
            _paginationService = paginationService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpPost]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> AddBook([FromBody] BookDTO bookDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

         
                int userId = GetLoggedInUserId();
                var result = await _bookService.AddBookAsync(bookDto, userId);

                if (!result)
                    return BadRequest(new { success = false, message = "Book could not be added." });

                return CreatedAtAction(nameof(GetBookById), new { id = bookDto.id }, new { success = true, message = "Book added successfully.", data = bookDto });
        }

        [HttpGet]
        [Authorize(Roles = "Administrator, Librarian, Student")]
        public async Task<IActionResult> GetAllBooks()
        {

                var books = await _bookService.GetAllBooksAsync();
                return Ok(new { success = true, message = "Books fetched successfully.", data = books });
        }

        [HttpGet("total-books")]
        [Authorize(Roles = "Administrator, Librarian, Student")]
        public async Task<IActionResult> GetTotalBookCount()
        {

            var books = await _bookService.GetTotalBookCountAsync();
            return Ok(new { success = true, message = "Books counted successfully.", data = books });
        }

        [HttpGet("get-by-id")]
        [Authorize(Roles = "Administrator, Librarian, Student")]
        public async Task<IActionResult> GetBookById([FromQuery] int id)
        {
           
                var book = await _bookService.GetBookByIdAsync(id);
                if (book == null)
                    return NotFound(new { success = false, message = $"Book with ID {id} not found." });

                return Ok(new { success = true, message = "Book fetched successfully.", data = book });
          
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteBook([FromQuery]int id)
        {
          
                int userId = GetLoggedInUserId();
                var result = await _bookService.DeleteBookAsync(id, userId);

                if (!result)
                    return NotFound(new { success = false, message = $"Book with ID {id} not found or already deleted." });

                return Ok(new { success = true, message = "Book deleted successfully." });
        }

        [HttpPatch]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> PatchBook([FromQuery]int id, [FromBody] JsonPatchDocument<BookDTO> patchDoc)
        {
                if (patchDoc == null)
                    return BadRequest(new { success = false, message = "Patch document is null.", data = (object)null });

                int userId = GetLoggedInUserId();
                var result = await _bookService.PatchBookAsync(id, patchDoc, userId);

                if (!result)
                    return NotFound(new { success = false, message = $"Book with ID {id} not found." });

                return Ok(new { success = true, message = "Book updated successfully." });
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> UpdateBook([FromQuery] int id, [FromBody] BookDTO bookDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

                int userId = GetLoggedInUserId();
                var result = await _bookService.UpdateBookAsync(id, bookDTO, userId);

                if (!result)
                    return NotFound(new { success = false, message = $"Book with ID {id} not found." });

                return Ok(new { success = true, message = "Book updated successfully." });
        }

        [HttpGet("book-page")]
        public async Task<IActionResult> GetBookPage([FromQuery] int bookPerPage, [FromQuery] int pageNo)
        {
                var bookList = await _paginationService.GetPageAsync(bookPerPage, pageNo);
                return Ok(new { success = true, message = "Page is fetch Successfully.", data = bookList });
        }
    }
}