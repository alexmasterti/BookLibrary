namespace BookLibrary.Options;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// Bound from appsettings.json section "Jwt".
///
/// SECURITY NOTE:
///   The Key value must NEVER be stored in source control for a real application.
///   Use environment variables (Jwt__Key) or dotnet user-secrets in development:
///     dotnet user-secrets set "Jwt:Key" "your-32-char-secret-here"
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 characters (256 bits).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Token issuer — identifies who created the token.</summary>
    public string Issuer { get; set; } = "BookLibrary";

    /// <summary>Token audience — identifies the intended recipients.</summary>
    public string Audience { get; set; } = "BookLibraryApiClients";

    /// <summary>How long the token is valid (minutes). Default: 60.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
