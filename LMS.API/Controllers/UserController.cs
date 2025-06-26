using System.Security.Claims;
using Asp.Versioning;
using LMS_Backend.LMS.Application.DTOs.User;
using LMS_Backend.LMS.Application.Interfaces.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/users")]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AddUser([FromBody] UserDataDTO userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdBy = GetLoggedInUserId();
            var userId = await _userService.AddUserAsync(userDto, createdBy);
            return Ok(new { success = true, message = "User added successfully.", userId });
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(new { success = true, message = "Users fetched successfully.", data = users });
        }

        [HttpGet("get-by-id")]
        [Authorize(Roles = "Administrator, Librarian, Student")]
        public async Task<IActionResult> GetUserById([FromQuery] int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(new { success = true, message = "User fetched successfully.", data = user });
        }

        [HttpGet("get-by-role")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> GetUsersByRole([FromQuery] string role)
        {
            var users = await _userService.GetUsersByRoleAsync(role);
            return Ok(new { success = true, message = $"Users with role '{role}' fetched successfully.", data = users });
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Librarian, Student")]
        public async Task<IActionResult> UpdateUser([FromQuery] int id, [FromBody] UserDataDTO userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            userDto.Id = id;
            var updatedBy = GetLoggedInUserId();
            await _userService.UpdateUserAsync(userDto, updatedBy);
            return Ok(new { success = true, message = "User updated successfully." });
        }

        [HttpPatch]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PatchUser([FromQuery] int id, [FromBody] JsonPatchDocument<UserDataDTO> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest(new { success = false, message = "Invalid patch document." });

            var updatedBy = GetLoggedInUserId();
            await _userService.PatchUserAsync(id, patchDoc, updatedBy);
            return Ok(new { success = true, message = "User patched successfully." });
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteUser([FromQuery] int id)
        {
            var deletedBy = GetLoggedInUserId();
            await _userService.DeleteUserAsync(id, deletedBy);
            return Ok(new { success = true, message = "User deleted successfully." });
        }
    }
}