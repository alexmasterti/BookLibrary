namespace BookLibrary.DTOs;

/// <summary>Inbound DTO for POST /api/auth/login.</summary>
public record LoginRequest(string Username, string Password);
