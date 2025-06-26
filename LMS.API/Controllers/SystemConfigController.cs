using System.Security.Claims;
using Asp.Versioning;
using LMS_Backend.LMS.Application.DTOs.BookTransaction;
using LMS_Backend.LMS.Application.Interfaces.SystemConfiguration;
using LMS_Backend.LMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/config")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrator")]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;

        public SystemConfigController(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllConfigs()
        {
            var configs = await _systemConfigService.GetAllConfigsAsync();
            return Ok(new { success = true, data = configs });
        }

        [HttpGet("key")]
        public async Task<IActionResult> GetConfigByKey([FromQuery] string key)
        {
            var value = await _systemConfigService.GetConfigValueByKeyAsync(key);
            return Ok(new { success = true, key, value });
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateConfig([FromBody] List<SystemConfig> dtos)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, errors = ModelState });

            var loggedInUser = GetLoggedInUserId();
            foreach (var dto in dtos)
            {
                await _systemConfigService.UpdateSystemConfigAsync(dto, loggedInUser);
            }
            return Ok(new { success = true, message = "System config updated successfully" });
        }
    }
}