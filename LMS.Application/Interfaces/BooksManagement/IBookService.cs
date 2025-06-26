using LMS_Backend.LMS.Application.DTOs.BookManagement;
using Microsoft.AspNetCore.JsonPatch;

namespace LMS_Backend.LMS.Application.Interfaces.BooksManagement
{
    public interface IBookService
    {
        public Task<GetBookDTO> GetBookByIdAsync(int id);
        public Task<IEnumerable<GetBookDTO>> GetAllBooksAsync();
        public Task<int> GetTotalBookCountAsync();
        public Task<bool> AddBookAsync(BookDTO book, int createdBy);
        public Task<bool> PatchBookAsync(int id, JsonPatchDocument<BookDTO> patchDoc, int updatedBy);
        public Task<bool> UpdateBookAsync(int id, BookDTO bookDto, int updatedBy);
        public Task<bool> DeleteBookAsync(int id, int deletedBy);

    }
}