using LMS_Backend.LMS.Application.DTOs.BookManagement;
using LMS_Backend.LMS.Application.Interfaces.BooksManagement;
using Microsoft.AspNetCore.JsonPatch;

namespace LMS_Backend.LMS.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<bool> AddBookAsync(BookDTO book, int createdBy)
        {
            return await _bookRepository.AddBookQuery(book, createdBy);
        }

        public async Task<bool> DeleteBookAsync(int id, int deletedBy)
        {
            return await _bookRepository.DeleteBookQuery(id, deletedBy);
        }

        public async Task<IEnumerable<GetBookDTO>> GetAllBooksAsync()
        {
            return await _bookRepository.GetAllBooksQuery();
        }

        public async Task<GetBookDTO> GetBookByIdAsync(int id)
        {
            return await _bookRepository.GetBookByIdQuery(id);
        }

        public async Task<int> GetTotalBookCountAsync()
        {
            return await _bookRepository.GetTotalBookCountQuery();
        }

        public async Task<bool> PatchBookAsync(int id, JsonPatchDocument<BookDTO> patchDoc, int updatedBy)
        {
            return await _bookRepository.PatchBookQuery(id, patchDoc, updatedBy);
        }

        public async Task<bool> UpdateBookAsync(int id, BookDTO bookDto, int updatedBy)
        {
            return await _bookRepository.UpdateBookQuery(id, bookDto, updatedBy);
        }
    }
}