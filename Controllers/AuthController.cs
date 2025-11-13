using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using DignaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DignaApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDynamoDbService _dynamoDbService;

    public AuthController(IAuthService authService, IDynamoDbService dynamoDbService)
    {
        _authService = authService;
        _dynamoDbService = dynamoDbService;

    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
    {
        
        var (success, response, error) = await _authService.LoginAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<LoginResponse>(
                error ?? "LOGIN_FAILED",
                "Login failed. Please check your credentials.",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Login successful"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(RegisterRequest request)
    {
        if(request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(ApiResponseHelper.Error<RegisterResponse>(
                "INVALID_REQUEST",
                "Invalid registration request. Please provide all required fields.",
                400
            ));
        }
        
        var (success, response, error) = await _authService.RegisterAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<RegisterResponse>(
                error ?? "REGISTRATION_FAILED",
                error == "EMAIL_ALREADY_EXISTS"
                    ? "An account with this email already exists"
                    : "Registration failed. Please try again.",
                400
            ));
        }

        return CreatedAtAction(nameof(Register), ApiResponseHelper.Success("Account created successfully. Please verify your email."));
    }

    [HttpPost("social-login")]
    public async Task<ActionResult<ApiResponse<SocialLoginResponse>>> SocialLogin(SocialLoginRequest request)
    {
        var (success, response, error) = await _authService.SocialLoginAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<SocialLoginResponse>(
                error ?? "SOCIAL_LOGIN_FAILED",
                "Social login failed. Please try again.",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Social login successful"));
    }

    [HttpPost("send-verification-email")]
    public async Task<ActionResult<ApiResponse<SendVerificationEmailResponse>>> SendVerificationEmail(SendVerificationEmailRequest request)
    {
        var (success, response, error) = await _authService.SendVerificationEmailAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<SendVerificationEmailResponse>(
                error ?? "SEND_EMAIL_FAILED",
                "Failed to send verification email.",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Verification email sent successfully"));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<VerifyEmailResponse>>> VerifyEmail(VerifyEmailRequest request)
    {
        var (success, response, error) = await _authService.VerifyEmailAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<VerifyEmailResponse>(
                error ?? "VERIFICATION_FAILED",
                error == "USER_NOT_FOUND" 
                    ? "User not found" 
                    : "Email verification failed. Invalid or expired code.",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Email verified successfully"));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<ChangePasswordResponse>>> ChangePassword(ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<ChangePasswordResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        var (success, response, error) = await _authService.ChangePasswordAsync(userId, request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<ChangePasswordResponse>(
                error ?? "PASSWORD_CHANGE_FAILED",
                error == "INVALID_CURRENT_PASSWORD"
                    ? "Current password is incorrect"
                    : "Failed to change password",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, response?.Message ?? "Password changed successfully"));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(ForgotPasswordRequest request)
    {
        var (success, response, error) = await _authService.ForgotPasswordAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<ForgotPasswordResponse>(
                error ?? "FORGOT_PASSWORD_FAILED",
                "Failed to process forgot password request.",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Password reset email sent successfully"));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<ResetPasswordResponse>>> ResetPassword(ResetPasswordRequest request)
    {
        var (success, response, error) = await _authService.ResetPasswordAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<ResetPasswordResponse>(
                error ?? "RESET_PASSWORD_FAILED",
                error == "INVALID_RESET_TOKEN"
                    ? "Invalid reset token"
                    : error == "RESET_TOKEN_EXPIRED"
                    ? "Reset token has expired"
                    : "Failed to reset password",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Password reset successfully"));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken(RefreshTokenRequest request)
    {
        var (success, response, error) = await _authService.RefreshTokenAsync(request);

        if (!success)
        {
            return Unauthorized(ApiResponseHelper.Error<RefreshTokenResponse>(
                error ?? "REFRESH_FAILED",
                "Failed to refresh token",
                401
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Token refreshed successfully"));
    }

    //[Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<LogoutResponse>>> Logout(RefreshTokenRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<LogoutResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        var (success, response, error) = await _authService.LogoutAsync(userId);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<LogoutResponse>(
                error ?? "LOGOUT_FAILED",
                "Failed to logout",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Logged out successfully"));
    }
}
