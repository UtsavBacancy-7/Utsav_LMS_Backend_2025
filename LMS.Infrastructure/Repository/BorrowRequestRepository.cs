using AutoMapper;
using Azure.Core;
using LMS_Backend.LMS.Application.DTOs.BookTransaction;
using LMS_Backend.LMS.Application.DTOs.NewFolder;
using LMS_Backend.LMS.Application.Interfaces.BookTransactions;
using LMS_Backend.LMS.Application.Interfaces.SystemConfiguration;
using LMS_Backend.LMS.Common.Exceptions;
using LMS_Backend.LMS.Domain.Entities;
using LMS_Backend.LMS.Domain.Enums;
using LMS_Backend.LMS.Infrastructure.Context;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace LMS_Backend.LMS.Infrastructure.Repository
{
    public class BorrowRequestRepository : GenericRepository<BorrowRequest>, IBorrowRequestRepository
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly IMapper _mapper;
        public BorrowRequestRepository(ApplicationDBContext context, ISystemConfigService systemConfigService, IMapper mapper) : base(context)
        {
            _mapper = mapper;
            _systemConfigService = systemConfigService;
        }

        public async Task<bool> AddBorrowRequestQuery(BorrowRequestCreateDTO request, int createdBy)
        {
            var requestList = _context.BorrowRequests.Where
                                        (s => s.UserId == createdBy && 
                                            (s.Status == Domain.Enums.RequestStatus.Approved || 
                                             s.Status == Domain.Enums.RequestStatus.Pending)
                                        ).ToList();

            var requestedBook = await _context.Books.FindAsync(request.BookId);

            int countLimit = await _systemConfigService.GetMaxBorrowLimitAsync();

            if (requestList.Count() >= countLimit)
            {
                throw new ApplicationException("You have reach the maximum request limit.");
            } else if (requestedBook == null)
            {
                throw new DataNotFoundException<string>($"Not book found with {request.BookId} id.");
            }
            else if (requestedBook.AvailableCopies <= 0)
            {
                throw new ApplicationException("Book is not available for Borrow.");
            }

            var newRequest = new BorrowRequest
            {
                BookId = request.BookId,
                UserId = createdBy,
                Status = Domain.Enums.RequestStatus.Pending,
                RequestDate = DateOnly.FromDateTime(DateTime.Today),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await _context.BorrowRequests.AddAsync(newRequest);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteBorrowRequestQuery(int id, int deletedBy)
        {
            var existingRequest = await _context.BorrowRequests.FirstOrDefaultAsync(s => s.BorrowRequestId == id && !s.IsDeleted);

            if(existingRequest == null)
            {
                throw new DataNotFoundException<string>($"Request is not found with {id}.");
            }

            existingRequest.IsDeleted = true;
            existingRequest.DeletedBy = deletedBy;
            existingRequest.DeletedAt = DateTime.UtcNow;

            _context.BorrowRequests.Update(existingRequest);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<BorrowResponseDTO>> GetAllBorrowRequestsQuery()
        {
            var requestList = await _context.BorrowRequests.Include(s => s.User).Include(s => s.Book).Where(s => !s.IsDeleted).Select(s => new BorrowResponseDTO
            {
                BorrowRequestId = s.BorrowRequestId,
                UserId = s.UserId,
                FirstName = s.User.FirstName,
                Title = s.Book.Title,
                LastName = s.User.LastName,
                Email = s.User.Email,
                Status = s.Status,
                RequestDate = s.RequestDate,
                ApprovedDate = s.ApprovedDate,
                DueDate = s.DueDate,
                ReturnDate = s.ReturnDate
            })
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            if (requestList == null)
                throw new DataNotFoundException<string>($"No data found.");

            return requestList;
        }

        public async Task<BorrowResponseDTO?> GetBorrowRequestByIdQuery(int id)
        {
            var request = await _context.BorrowRequests.Where(s => s.BorrowRequestId == id && !s.IsDeleted).Include(s => s.User).Include(s => s.Book).Select(s => new BorrowResponseDTO
            {
                BorrowRequestId = s.BorrowRequestId,
                UserId = s.UserId,
                FirstName = s.User.FirstName,
                LastName = s.User.LastName,
                Title = s.Book.Title,
                Email = s.User.Email,
                Status = s.Status,
                RequestDate = s.RequestDate,
                ApprovedDate = s.ApprovedDate,
                DueDate = s.DueDate,
                ReturnDate = s.ReturnDate
            }).FirstOrDefaultAsync();

            if (request == null)
                throw new DataNotFoundException<string>($"No data found with id {id}.");

            return request;
        }   

        public async Task<IEnumerable<BorrowResponseDTO>?> GetBorrowRequestByUserIdQuery(int id)
        {
            var requestList = await _context.BorrowRequests.Where(s => s.UserId == id  && !s.IsDeleted).Include(s => s.User).Include(s => s.Book).Select(s => new BorrowResponseDTO
            {
                BorrowRequestId = s.BorrowRequestId,
                UserId = s.UserId,
                FirstName = s.User.FirstName,
                LastName = s.User.LastName,
                Title = s.Book.Title,
                Email = s.User.Email,
                Status = s.Status,
                RequestDate = s.RequestDate,
                ApprovedDate = s.ApprovedDate,
                DueDate = s.DueDate,
                ReturnDate = s.ReturnDate
            }).ToListAsync();

            if (requestList == null)
                throw new DataNotFoundException<string>($"No data found with userId: {id}.");

            return requestList;
        }

        public async Task<bool> PatchBorrowRequestQuery(int id, JsonPatchDocument<BorrowRequestUpdateStatusDTO> patchDoc, int updatedBy)
        {
            var existingRequest = await _context.BorrowRequests
                .Include(b => b.Book) // Include Book for updates
                .FirstOrDefaultAsync(s => s.BorrowRequestId == id && !s.IsDeleted);

            if (existingRequest == null) return false;

            var originalStatus = existingRequest.Status;
            var originalDueDate = existingRequest.DueDate;

            // Map entity to DTO
            var requestDto = _mapper.Map<BorrowRequestUpdateStatusDTO>(existingRequest);

            // Apply patch to DTO
            patchDoc.ApplyTo(requestDto);

            // Map changes back to entity
            _mapper.Map(requestDto, existingRequest);

            // Now check if status changed
            if (existingRequest.Status != originalStatus)
            {
                switch (existingRequest.Status)
                {
                    case RequestStatus.Approved:
                        existingRequest.ApprovedBy = updatedBy;
                        existingRequest.ApprovedDate = DateOnly.FromDateTime(DateTime.UtcNow);

                        var bookData = await _context.Books.FindAsync(existingRequest.BookId);
                        if (bookData != null && bookData.AvailableCopies > 0)
                        {
                            bookData.AvailableCopies--;
                            _context.Books.Update(bookData);
                        }
                        else
                        {
                            existingRequest.Status = Domain.Enums.RequestStatus.Rejected;
                            throw new NotImplementedException("No Copies available for borrow.");
                        }

                        int borrowingPeriod = await _systemConfigService.GetMaxBorrowPeriodAsync();

                        existingRequest.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(borrowingPeriod));
                        break;

                    case RequestStatus.Returned:
                        existingRequest.ReturnDate = DateOnly.FromDateTime(DateTime.UtcNow);

                        if (existingRequest.DueDate.HasValue ||
                            existingRequest.ReturnDate > existingRequest.DueDate)
                        {  
                            if (existingRequest.Book != null)
                            {
                                existingRequest.Book.AvailableCopies++;
                            }
                        }
                        break;

                    case RequestStatus.Rejected:
                    case RequestStatus.Completed:
                        break;
                }
            }

            existingRequest.UpdatedAt = DateTime.UtcNow;
            existingRequest.UpdatedBy = updatedBy;

            return await _context.SaveChangesAsync() > 0;
        }
    }
}