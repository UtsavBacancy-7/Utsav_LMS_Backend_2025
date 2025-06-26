using System.Security.Claims;
using Asp.Versioning;
using LMS_Backend.LMS.Application.DTOs.BookTransaction;
using LMS_Backend.LMS.Application.Interfaces.BookTransactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/transactions")]
    [Produces("application/json")]
    public class ReturnRequestController : ControllerBase
    {
        private readonly IReturnService _returnService;

        public ReturnRequestController(IReturnService returnService)
        {
            _returnService = returnService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet("return-requests")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> GetAllReturnRequests()
        {
            var returnRequests = await _returnService.GetAllReturnRequestsAsync();
            return Ok(returnRequests);
        }

        [HttpGet("return-requests-by-id")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> GetReturnRequestById([FromQuery] int id)
        {
            var returnRequest = await _returnService.GetReturnRequestByIdAsync(id);
            return Ok(returnRequest);
        }

        [HttpGet("users/return-requests")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetReturnRequestsByUserId([FromQuery] int userId)
        {
            var returnRequests = await _returnService.GetReturnRequestsByUserIdAsync(userId);
            return Ok(returnRequests);
        }

        [HttpPost("return-requests")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateReturnRequest([FromBody] ReturnRequestCreateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdBy = GetLoggedInUserId();
            await _returnService.AddReturnRequestAsync(request, createdBy);
            return CreatedAtAction(nameof(GetReturnRequestById), new { id = request.BorrowRequestId }, null);
        }

        [HttpPatch("return-requests")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> UpdateReturnRequestStatus(
            [FromQuery] int id,
            [FromBody] JsonPatchDocument<ReturnRequestUpdateStatusDTO> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest("Patch document is required");
            }

            var updatedBy = GetLoggedInUserId();
            await _returnService.PatchReturnRequestAsync(id, patchDoc, updatedBy);
            return NoContent();
        }

        [HttpDelete("return-requests")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteReturnRequest([FromQuery] int id)
        {
            var deletedBy = GetLoggedInUserId();
            await _returnService.DeleteReturnRequestAsync(id, deletedBy);
            return NoContent();
        }
    }
}