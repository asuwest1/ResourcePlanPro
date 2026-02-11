using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ResourcePlanProContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ResourcePlanProContext context,
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Username and password are required"
                    });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed for username: {Username}", request.Username);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for username: {Username}", request.Username);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                // Update last login date
                user.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = _authService.GenerateToken(user);

                _logger.LogInformation("User {Username} logged in successfully", user.Username);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again."
                });
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public async Task<ActionResult<UserDto>> ValidateToken()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized();
                }

                return Ok(new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, "An error occurred while validating the token");
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In a JWT-based system, logout is handled client-side by removing the token
            _logger.LogInformation("User logged out");
            return Ok(new { message = "Logout successful" });
        }
    }
}
