using LMS_Backend.LMS.Application.DTOs.BookManagement;
using Microsoft.AspNetCore.JsonPatch;

namespace LMS_Backend.LMS.Application.Interfaces.BooksManagement
{
    public interface IBookRepository
    {
        public Task<GetBookDTO> GetBookByIdQuery(int id);
        public Task<IEnumerable<GetBookDTO>> GetAllBooksQuery();
        public Task<int> GetTotalBookCountQuery();
        public Task<bool> AddBookQuery(BookDTO book, int createdBy);
        public Task<bool> PatchBookQuery(int id, JsonPatchDocument<BookDTO> patchDoc, int updatedBy);
        public Task<bool> UpdateBookQuery(int id, BookDTO bookDto, int updatedBy);
        public Task<bool> DeleteBookQuery(int id, int deletedBy);
    }
}