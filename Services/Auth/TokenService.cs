using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookLibrary.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookLibrary.Services.Auth;

/// <summary>
/// Generates signed JWT tokens for authenticated API clients.
///
/// CONCEPT: JWT (JSON Web Token)
///   A JWT is a compact, self-contained token with three parts:
///   1. Header  — algorithm (HS256) and token type.
///   2. Payload — claims: who the user is, when it expires, etc.
///   3. Signature — HMAC-SHA256(header + payload, secret key).
///
///   The server validates the signature on every request.
///   No session state is stored server-side — the token IS the session.
///
/// CONCEPT: Options Pattern
///   JwtOptions is injected via IOptions&lt;JwtOptions&gt;, binding
///   cleanly from appsettings.json without magic strings.
///
/// PRINCIPLE: Single Responsibility
///   This service only creates tokens. Validation is handled by
///   the JWT Bearer middleware configured in Program.cs.
/// </summary>
public interface ITokenService
{
    /// <summary>Generates a signed JWT for the given username.</summary>
    (string token, DateTime expiresAt) GenerateToken(string username);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
        => _options = options.Value;

    public (string token, DateTime expiresAt) GenerateToken(string username)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _options.Issuer,
            audience:           _options.Audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
