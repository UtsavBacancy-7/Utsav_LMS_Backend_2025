using AutoMapper;
using LMS_Backend.LMS.Application.DTOs.BookManagement;
using LMS_Backend.LMS.Application.Interfaces.BooksManagement;
using LMS_Backend.LMS.Common.Exceptions;
using LMS_Backend.LMS.Domain.Entities;
using LMS_Backend.LMS.Infrastructure.Context;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.LMS.Infrastructure.Repository
{
    public class BookRepository : GenericRepository<Book>, IBookRepository
    {
        private readonly IMapper _mapper;

        public BookRepository(ApplicationDBContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<bool> AddBookQuery(BookDTO bookDto, int createdBy)
        {
            var existingBook = await _context.Books
                .FirstOrDefaultAsync(s => s.Title == bookDto.Title);

            if (existingBook != null)
                throw new AlreadyExistsException<string>("Book already exists.");

            var newBook = _mapper.Map<Book>(bookDto);
            newBook.CreatedBy = createdBy;
            newBook.CreatedAt = DateTime.UtcNow;

            await _context.Books.AddAsync(newBook);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteBookQuery(int id, int deletedBy)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return false;

            book.IsDeleted = true;
            book.DeletedAt = DateTime.UtcNow;
            book.DeletedBy = deletedBy;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<GetBookDTO>> GetAllBooksQuery()
        {
            var books = await _context.Books.Include(s => s.Genre)
                .Where(b => !b.IsDeleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<GetBookDTO>>(books);
        }

        public async Task<int> GetTotalBookCountQuery()
        {
            var count = await _context.Books.Where(s => !s.IsDeleted).CountAsync();

            return count;
        }

        public async Task<GetBookDTO> GetBookByIdQuery(int id)
        {
            var book = await _context.Books
                .Include(s => s.Genre)
                .FirstOrDefaultAsync(b => b.BookId == id && !b.IsDeleted);

            return _mapper.Map<GetBookDTO>(book);
        }

        public async Task<bool> PatchBookQuery(int id, JsonPatchDocument<BookDTO> patchDoc, int updatedBy)
        {
            var existingBook = await _context.Books.FindAsync(id);
            if (existingBook == null) return false;

            var bookDto = _mapper.Map<BookDTO>(existingBook);
            patchDoc.ApplyTo(bookDto);

            _mapper.Map(bookDto, existingBook);
            existingBook.UpdatedAt = DateTime.UtcNow;
            existingBook.UpdatedBy = updatedBy;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateBookQuery(int id, BookDTO bookDto, int updatedBy)
        {
            var existingBook = await _context.Books.FindAsync(id);
            if (existingBook == null) return false;

            bookDto.id = existingBook.BookId; 

            _mapper.Map(bookDto, existingBook);
            existingBook.UpdatedAt = DateTime.UtcNow;
            existingBook.UpdatedBy = updatedBy;

            return await _context.SaveChangesAsync() > 0;
        }
    }
}