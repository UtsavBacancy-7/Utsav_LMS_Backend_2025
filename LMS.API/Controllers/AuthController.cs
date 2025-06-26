using Asp.Versioning;
using LMS_Backend.LMS.Application.DTOs.Authentication;
using LMS_Backend.LMS.Application.Interfaces.Authentication;
using LMS_Backend.LMS.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.LMS.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;

        public AuthController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(RegisterDTO register)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

                var message = await _authRepository.RegisterAsync(register);
                return Ok(new { success = true, Message = message });
          
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login(LoginDTO userLoginDTO)
        {
           
                var loginResponse = await _authRepository.LoginAsync(userLoginDTO);
                return Ok(new { success = true, message = "Login Successfully", data = loginResponse });
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(ForgotPwdDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
                var message = await _authRepository.SendOtpToEmail(dto);
                return Ok(new { success = true, message = "OTP sent to email." });
        }

        [HttpPost("ResetPassword")]
        public async Task<ActionResult<string>> ResetPassword(ResetPwdDTO resetPwdDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

                var result = await _authRepository.ResetPassword(resetPwdDTO);
                return Ok(new { success = true, Message = result });
        }
    }
}