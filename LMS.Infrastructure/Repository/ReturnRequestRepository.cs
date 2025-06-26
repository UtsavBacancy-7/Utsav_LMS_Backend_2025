using AutoMapper;
using LMS_Backend.LMS.Application.DTOs.BookTransaction;
using LMS_Backend.LMS.Application.Interfaces.BookTransactions;
using LMS_Backend.LMS.Application.Interfaces.SystemConfiguration;
using LMS_Backend.LMS.Application.Interfaces.WishListAndNotification;
using LMS_Backend.LMS.Common.Exceptions;
using LMS_Backend.LMS.Domain.Entities;
using LMS_Backend.LMS.Infrastructure.Context;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.LMS.Infrastructure.Repository
{
    public class ReturnRequestRepository : GenericRepository<ReturnRequest>, IReturnRequestRepository
    {
        private readonly IMapper _mapper;
        private readonly ISystemConfigService _systemConfig;
        private readonly IWishlistAndNotificationService _wishlistAndNotificationService;
        public ReturnRequestRepository(ApplicationDBContext context, IMapper mapper, ISystemConfigService systemConfig, IWishlistAndNotificationService wishlistAndNotificationService): base(context)
        {
            _wishlistAndNotificationService = wishlistAndNotificationService;
            _systemConfig = systemConfig;
            _mapper = mapper;
        }

        public async Task<bool> AddReturnRequestQuery(ReturnRequestCreateDTO request, int createdBy)
        {
            // Check if pending return request already exists
            var existingRequest = await _context.ReturnRequests
                .AnyAsync(r => r.BorrowRequestId == request.BorrowRequestId
                             && (r.Status == Domain.Enums.RequestStatus.Pending)
                             && !r.IsDeleted);

            if (existingRequest)
            {
                throw new InvalidOperationException("A pending return request already exists for this borrow request.");
            }

            // Get the borrow request details
            var borrowRequest = await _context.BorrowRequests
                .FirstOrDefaultAsync(br => br.BorrowRequestId == request.BorrowRequestId && !br.IsDeleted && br.Status == Domain.Enums.RequestStatus.Approved);

            if (borrowRequest == null)
            {
                throw new DataNotFoundException<BorrowRequest>("Borrow request not found.");
            }

            // Map and create new return request using AutoMapper
            var returnRequest = _mapper.Map<ReturnRequest>(request);
            returnRequest.RequestedDate = DateOnly.FromDateTime(DateTime.Today);
            returnRequest.Status = Domain.Enums.RequestStatus.Pending;
            returnRequest.CreatedAt = DateTime.UtcNow;
            returnRequest.CreatedBy = createdBy;

            await _context.ReturnRequests.AddAsync(returnRequest);

            // Map and create book review using AutoMapper
            var bookReview = _mapper.Map<BookReview>(request);
            bookReview.BookId = borrowRequest.BookId;
            bookReview.UserId = borrowRequest.UserId;
            bookReview.CreatedAt = DateTime.UtcNow;
            bookReview.CreatedBy = createdBy;

            await _context.BookReviews.AddAsync(bookReview);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to create return request and book review.", ex);
            }
        }

        public async Task<bool> DeleteReturnRequestQuery(int id, int deletedBy)
        {
            var existingRequest = await _context.ReturnRequests.FindAsync(id);

            if (existingRequest == null)
                throw new DataNotFoundException<string>($"No Request Found with {id} id.");

            existingRequest.IsDeleted = true;
            existingRequest.DeletedAt = DateTime.UtcNow;
            existingRequest.DeletedBy = deletedBy;

            _context.ReturnRequests.Update(existingRequest);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ReturnResponseDTO>> GetAllReturnRequestsQuery()
        {
            var requestList = await _context.ReturnRequests
                .Where(s => !s.IsDeleted)
                .Include(s => s.BorrowRequest)
                    .ThenInclude(br => br.User)
                .Include(s => s.BorrowRequest)
                    .ThenInclude(br => br.Book)
                .Select(s => new ReturnResponseDTO
                {
                    ReturnRequestId = s.ReturnRequestId,
                    FirstName = s.BorrowRequest.User.FirstName,
                    Title = s.BorrowRequest.Book.Title,
                    LastName = s.BorrowRequest.User.LastName,
                    Email = s.BorrowRequest.User.Email,
                    Status = s.Status,
                    RequestDate = s.RequestedDate,
                    ApprovedDate = s.ApprovedDate,
                    DueDate = s.BorrowRequest.DueDate,
                })
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            if (requestList == null)
                throw new DataNotFoundException<string>("No Data Found.");

            return requestList;
        }

        public async Task<ReturnResponseDTO?> GetReturnRequestByIdQuery(int id)
        {
            var request = await _context.ReturnRequests
                    .Where(s => !s.IsDeleted && s.ReturnRequestId == id)
                    .Include(s => s.BorrowRequest)
                        .ThenInclude(br => br.User)
                    .Include(s => s.BorrowRequest)
                        .ThenInclude(br => br.Book)
                    .Select(s => new ReturnResponseDTO
                    {
                        ReturnRequestId = s.ReturnRequestId,
                        FirstName = s.BorrowRequest.User.FirstName,
                        Title = s.BorrowRequest.Book.Title,
                        LastName = s.BorrowRequest.User.LastName,
                        Email = s.BorrowRequest.User.Email,
                        Status = s.Status,
                        RequestDate = s.RequestedDate,
                        ApprovedDate = s.ApprovedDate,
                        DueDate = s.BorrowRequest.DueDate,
                    }).FirstOrDefaultAsync();

            if (request == null)
                throw new DataNotFoundException<string>("No Data Found.");

            return request;
        }

        public async Task<IEnumerable<ReturnResponseDTO>> GetReturnRequestsByUserIdQuery(int userId)
        {
            var requestData = await _context.ReturnRequests
                               .Include(s => s.BorrowRequest)
                                   .ThenInclude(br => br.User)
                               .Include(s => s.BorrowRequest)
                                   .ThenInclude(br => br.Book)
                               .Where(s => !s.IsDeleted && s.BorrowRequest.UserId == userId)
                               .Select(s => new ReturnResponseDTO
                               {
                                   ReturnRequestId = s.ReturnRequestId,
                                   FirstName = s.BorrowRequest.User.FirstName,
                                   Title = s.BorrowRequest.Book.Title,
                                   LastName = s.BorrowRequest.User.LastName,
                                   Email = s.BorrowRequest.User.Email,
                                   Status = s.Status,
                                   RequestDate = s.RequestedDate,
                                   ApprovedDate = s.ApprovedDate,
                                   DueDate = s.BorrowRequest.DueDate,
                               }).ToListAsync();

            return requestData;
        }

        public async Task<bool> PatchReturnRequestQuery(int id, JsonPatchDocument<ReturnRequestUpdateStatusDTO> patchDoc, int updatedBy)
        {
            // Get the existing return request
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.BorrowRequest)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id && !r.IsDeleted);

            if (returnRequest == null)
            {
                throw new DataNotFoundException<ReturnRequest>($"Return request with ID {id} not found.");
            }

            // Create DTO and apply patch
            var returnRequestToPatch = new ReturnRequestUpdateStatusDTO
            {
                ReturnRequestId = returnRequest.ReturnRequestId,
                Status = returnRequest.Status
            };

            patchDoc.ApplyTo(returnRequestToPatch);

            if (returnRequest.Status != returnRequestToPatch.Status)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    returnRequest.Status = returnRequestToPatch.Status;
                    returnRequest.UpdatedAt = DateTime.UtcNow;
                    returnRequest.UpdatedBy = updatedBy;

                    if (returnRequestToPatch.Status == Domain.Enums.RequestStatus.Approved)
                    {
                        returnRequest.ApprovedBy = updatedBy;
                        returnRequest.ApprovedDate = DateOnly.FromDateTime(DateTime.Today); 

                        if (returnRequest.BorrowRequest != null)
                        {
                            var borrowRequest = returnRequest.BorrowRequest;
                            var returnDate = DateOnly.FromDateTime(DateTime.Today);

                            // Update borrow request status and return date
                            borrowRequest.Status = Domain.Enums.RequestStatus.Returned;
                            borrowRequest.ReturnDate = returnDate;
                            borrowRequest.UpdatedAt = DateTime.UtcNow;
                            borrowRequest.UpdatedBy = updatedBy;

                            // Calculate penalty if return date is after due date
                            if (borrowRequest.DueDate < returnDate)
                            {
                                decimal rate = await _systemConfig.GetPenaltyPerDayAsync();
                                var daysLate = (borrowRequest.ReturnDate.Value.ToDateTime(TimeOnly.MinValue) -
                                              borrowRequest.DueDate.Value.ToDateTime(TimeOnly.MinValue)).Days;

                                borrowRequest.Penalty = daysLate * rate;
                            }
                            else
                            {
                                borrowRequest.Penalty = 0;
                            }

                            // Increase available copies in Books table
                            var book = await _context.Books.FindAsync(borrowRequest.BookId);
                            if (book != null)
                            {  
                                book.AvailableCopies++;
                                book.UpdatedAt = DateTime.UtcNow;
                                book.UpdatedBy = updatedBy;
                                await _wishlistAndNotificationService.ProcessReturnedBookNotificationsAsync(book.BookId);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return false;
        }
    }
}