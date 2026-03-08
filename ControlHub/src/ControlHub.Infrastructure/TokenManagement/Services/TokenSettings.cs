namespace ControlHub.Infrastructure.TokenManagement.Services
{
    public class TokenSettings
    {
        public int AccessTokenMinutes { get; set; }
        public int RefreshTokenDays { get; set; }
        public int ResetPasswordMinutes { get; set; }
        public int VerifyEmailHours { get; set; }
    }
}
