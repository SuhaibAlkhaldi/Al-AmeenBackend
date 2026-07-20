namespace DLPManagementSystem.DTO.Auth
{
    public sealed class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public AuthUserDto User { get; set; } = null!;
    }
}
