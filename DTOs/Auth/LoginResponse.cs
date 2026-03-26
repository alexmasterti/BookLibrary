namespace BookLibrary.DTOs.Auth;

/// <summary>Outbound DTO for a successful POST /api/auth/login.</summary>
public record LoginResponse(string Token, DateTime ExpiresAt);
