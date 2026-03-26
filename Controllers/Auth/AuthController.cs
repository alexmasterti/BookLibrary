using BookLibrary.DTOs.Auth;
using BookLibrary.Services.Books;
using BookLibrary.Services.Authors;
using BookLibrary.Services.Auth;
using BookLibrary.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Controllers.Auth;

/// <summary>
/// Handles API authentication — issues JWT tokens to verified callers.
///
/// CONCEPT: JWT Authentication Flow
///   1. Client POSTs credentials to /api/auth/login.
///   2. Server validates credentials, generates a signed JWT, returns it.
///   3. Client includes the token in the Authorization header of future requests:
///        Authorization: Bearer &lt;token&gt;
///   4. JWT middleware validates the signature and populates HttpContext.User.
///   5. [Authorize] attributes allow or deny access based on that identity.
///
/// NOTE ON CREDENTIALS:
///   In this sample, credentials are validated against a fixed value from
///   configuration (ApiCredentials:Username / ApiCredentials:Password).
///   A production system would use ASP.NET Identity, a user store, and
///   password hashing (BCrypt/Argon2). This simplified approach is intentional
///   to keep the focus on the JWT mechanics, not identity management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthController(ITokenService tokenService, IConfiguration configuration)
    {
        _tokenService   = tokenService;
        _configuration  = configuration;
    }

    /// <summary>
    /// Authenticate and receive a JWT token.
    /// </summary>
    /// <remarks>
    /// Sample credentials (set in appsettings.json under ApiCredentials):
    /// Username: admin | Password: BookShelf2024!
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var validUsername = _configuration["ApiCredentials:Username"];
        var validPassword = _configuration["ApiCredentials:Password"];

        if (request.Username != validUsername || request.Password != validPassword)
            return Unauthorized(new { message = "Invalid credentials." });

        var (token, expiresAt) = _tokenService.GenerateToken(request.Username);
        return Ok(new LoginResponse(token, expiresAt));
    }
}
