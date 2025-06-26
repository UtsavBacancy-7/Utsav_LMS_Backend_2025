using System.Security.Claims;
using Asp.Versioning;
using LMS_Backend.LMS.Application.Interfaces;
using LMS_Backend.LMS.Application.Interfaces.UserManagement;
using LMS_Backend.LMS.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/dashboard")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IDashboardStatService _dashboardStatService;

        public AdminController(IUserService userService, IDashboardStatService dashboardStatService)
        {
            _dashboardStatService = dashboardStatService;
            _userService = userService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
                int loggedInUser = GetLoggedInUserId();

                if (loggedInUser == 0)
                {
                    return Unauthorized(new { success = false, message = "User is not authenticated" });
                }

                var profileData = await _userService.GetUserByIdAsync(loggedInUser);

                if (profileData == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                return Ok(new { success = true, message = "User profile fetched successfully", data = profileData });
           
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Administrator, Librarian")]
        public async Task<IActionResult> DashboardStats()
        {
          
                var statsData =  await _dashboardStatService.GetAllStatsAsync();
                return Ok(new { success = true, message = "Data fetched successfully", data = statsData });
        }
    }
}