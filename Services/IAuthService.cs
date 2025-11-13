using DignaApi.Models.Requests;
using DignaApi.Models.Responses;

namespace DignaApi.Services;

public interface IAuthService
{
    Task<(bool success, LoginResponse? response, string? error)> LoginAsync(LoginRequest request);
    Task<(bool success, RegisterResponse? response, string? error)> RegisterAsync(RegisterRequest request);
    Task<(bool success, SocialLoginResponse? response, string? error)> SocialLoginAsync(SocialLoginRequest request);
    Task<(bool success, VerifyEmailResponse? response, string? error)> VerifyEmailAsync(VerifyEmailRequest request);
    Task<(bool success, SendVerificationEmailResponse? response, string? error)> SendVerificationEmailAsync(SendVerificationEmailRequest request);
    Task<(bool success, ChangePasswordResponse? response, string? error)> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<(bool success, ForgotPasswordResponse? response, string? error)> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<(bool success, ResetPasswordResponse? response, string? error)> ResetPasswordAsync(ResetPasswordRequest request);
    Task<(bool success, RefreshTokenResponse? response, string? error)> RefreshTokenAsync(RefreshTokenRequest request);
    Task<(bool success, LogoutResponse? response, string? error)> LogoutAsync(string userId);
}
