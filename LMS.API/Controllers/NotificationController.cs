using LMS_Backend.LMS.Application.Interfaces.WishListAndNotification;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/notifications")]
    [Produces("application/json")]
    public class NotificationController : ControllerBase
    {
        private readonly IWishlistAndNotificationService _wishlistAndNotificationService;

        public NotificationController(IWishlistAndNotificationService wishlistAndNotificationService)
        {
            _wishlistAndNotificationService = wishlistAndNotificationService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet("my-notifications")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetNotifications()
        {
            var loggedInUser = GetLoggedInUserId();
            var notifications = await _wishlistAndNotificationService.GetUserNotificationsAsync(loggedInUser);
            return Ok(new { success = true, message = "Notifications fetched successfully", data = notifications });
        }

        [HttpPost("read")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReadNotification([FromQuery] int notificationId)
        {
            var loggedInUser = GetLoggedInUserId();
            await _wishlistAndNotificationService.MarkNotificationAsReadAsync(notificationId);
            return Ok(new { success = true, message = "Notification marked as read" });
        }

        [HttpGet("unread")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UnReadNotification()
        {
            var loggedInUser = GetLoggedInUserId();
            var notifications = await _wishlistAndNotificationService.GetUnreadNotificationCountAsync(loggedInUser);
            return Ok(new { success = true, message = "Unread notifications count fetched", data = notifications });
        }
    }
}