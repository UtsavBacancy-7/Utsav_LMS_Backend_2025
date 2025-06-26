using LMS_Backend.LMS.Application.Interfaces.UserManagement;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LMS_Backend.LMS.Application.Interfaces.BookTransactions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using LMS_Backend.LMS.Application.DTOs.BookTransaction;
using Microsoft.AspNetCore.JsonPatch;
using LMS_Backend.LMS.Domain.Entities;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/transactions")]
    [Produces("application/json")]
    public class BorrowRequestController : ControllerBase
    {
        private readonly IBorrowService _borrowService;
        private readonly ITransactionHistoryService _transactionHistoryService;

        public BorrowRequestController(IBorrowService borrowService, ITransactionHistoryService transactionHistoryService)
        {
            _transactionHistoryService = transactionHistoryService;
            _borrowService = borrowService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet("borrow-requests")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> GetAllBorrowRequests()
        {
            var requests = await _borrowService.GetAllBorrowRequestsAsync();
            return Ok(requests);
        }

        [HttpGet("borrow-requests-by-userId")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetBorrowRequestByUserId([FromQuery] int id)
        {
            var loggedInUser = GetLoggedInUserId();
            var request = await _borrowService.GetBorrowRequestByUserIdAsync(loggedInUser);
            return Ok(request);
        }

        [HttpGet("borrow-requests-by-id")]
        [Authorize(Roles = "Administrator, Librarian, Student")]
        public async Task<IActionResult> GetBorrowRequestById([FromQuery] int id)
        {
            var request = await _borrowService.GetBorrowRequestByIdAsync(id);
            return Ok(request);
        }

        [HttpPost("borrow-requests")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateBorrowRequest([FromBody] BorrowRequestCreateDTO request)
        {
            var createdBy = GetLoggedInUserId();
            await _borrowService.AddBorrowRequestAsync(request, createdBy);
            return NoContent();
        }

        [HttpPatch("borrow-requests")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> UpdateBorrowRequest([FromQuery] int id, [FromBody] JsonPatchDocument<BorrowRequestUpdateStatusDTO> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest("Patch document is required");
            }

            var updatedBy = GetLoggedInUserId();
            await _borrowService.PatchBorrowRequestAsync(id, patchDoc, updatedBy);
            return NoContent();
        }

        [HttpDelete("borrow-requests")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteBorrowRequest([FromQuery] int id)
        {
            var deletedBy = GetLoggedInUserId();
            await _borrowService.DeleteBorrowRequestAsync(id, deletedBy);
            return NoContent();
        }

        [HttpGet("get-all")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllTransaction()
        {
            var transaction = await _transactionHistoryService.GetAllTransactionAsync();
            return Ok(transaction);
        }

        [HttpGet("get-by-userId")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTransactionByUserId()
        {
            var loggedInUser = GetLoggedInUserId();
            var transaction = await _transactionHistoryService.GetAllTransactionByUserIdAsync(loggedInUser);
            return Ok(transaction);
        }
    }
}