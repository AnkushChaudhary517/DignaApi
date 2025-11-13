using Microsoft.EntityFrameworkCore;
using DignaApi.Data;
using DignaApi.Entities;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;

namespace DignaApi.Services;

public class AuthService : IAuthService
{
    private readonly DignaDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IDynamoDbService _dynamoDbService;

    public AuthService(DignaDbContext context, ITokenService tokenService, IDynamoDbService dynamoDbService)
    {
        _context = context;
        _tokenService = tokenService;
        _dynamoDbService = dynamoDbService;

    }

    public async Task<(bool success, LoginResponse? response, string? error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _dynamoDbService.GetUserByEmail(request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return (false, null, "INVALID_CREDENTIALS");
            }

            var token = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var response = new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImage = user.Profile?.ProfileImageUrl ?? "",
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = 86400
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, RegisterResponse? response, string? error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existinguser = await _dynamoDbService.GetUserByEmail(request.Email);
            if (existinguser != null)
            {
                return (false, null, "EMAIL_ALREADY_EXISTS");
            }

            //var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            //if (existingUser != null)
            //{
            //    return (false, null, "EMAIL_ALREADY_EXISTS");
            //}

            var user = new User
            {
                Email = request.Email,
                FirstName = request.Name.Split(' ')[0],
                LastName = request.Name.Contains(' ') ? request.Name.Split(' ')[1] : "",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                VerificationToken = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _dynamoDbService.CreateUserAsync(user);
            //_context.Users.Add(user);
            //await _context.SaveChangesAsync();

            var response = new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Name = request.Name,
                EmailVerified = false,
                VerificationToken = user.VerificationToken,
                CreatedAt = user.CreatedAt
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, SocialLoginResponse? response, string? error)> SocialLoginAsync(SocialLoginRequest request)
    {
        try
        {
            // For demo purposes, extract email from idToken (in production, validate with provider)
            var demoEmail = $"social_{request.Provider}_{Guid.NewGuid().ToString()[..8]}@digna.com";
            
            var user = await _context.Users.Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == demoEmail);

            bool isNewUser = false;
            if (user == null)
            {
                isNewUser = true;
                user = new User
                {
                    Email = demoEmail,
                    FirstName = $"{request.Provider.ToUpper()} User",
                    LastName = Guid.NewGuid().ToString()[..4],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Profile = new UserProfile
                    {
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var response = new SocialLoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImage = user.Profile?.ProfileImageUrl ?? "",
                Token = token,
                RefreshToken = refreshToken,
                IsNewUser = isNewUser
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, VerifyEmailResponse? response, string? error)> VerifyEmailAsync(VerifyEmailRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }

            // For demo purposes, accept any code
            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var response = new VerifyEmailResponse
            {
                UserId = user.Id,
                Email = user.Email,
                EmailVerified = true,
                VerifiedAt = DateTime.UtcNow
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, SendVerificationEmailResponse? response, string? error)> SendVerificationEmailAsync(SendVerificationEmailRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }

            // For demo purposes, just return success
            var response = new SendVerificationEmailResponse
            {
                Email = request.Email,
                VerificationSent = true,
                ExpiresIn = 3600
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, ChangePasswordResponse? response, string? error)> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return (false, null, "INVALID_CURRENT_PASSWORD");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var response = new ChangePasswordResponse
            {
                UserId = user.Id,
                Message = "Password changed successfully"
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, ForgotPasswordResponse? response, string? error)> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var response = new ForgotPasswordResponse
            {
                Email = request.Email,
                ResetTokenSent = true,
                ExpiresIn = 3600
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, ResetPasswordResponse? response, string? error)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.ResetToken == request.ResetToken);
            if (user == null)
            {
                return (false, null, "INVALID_RESET_TOKEN");
            }

            if (user.ResetTokenExpiry < DateTime.UtcNow)
            {
                return (false, null, "RESET_TOKEN_EXPIRED");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var response = new ResetPasswordResponse
            {
                Email = request.Email,
                PasswordReset = true
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, RefreshTokenResponse? response, string? error)> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            // For demo purposes, accept any refresh token and generate new one
            // In production, validate and store refresh tokens in database
            
            var newAccessToken = _tokenService.GenerateAccessToken(new User 
            { 
                Id = "user_temp", 
                Email = "temp@demo.com",
                FirstName = "Demo",
                LastName = "User"
            });
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var response = new RefreshTokenResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 86400
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, LogoutResponse? response, string? error)> LogoutAsync(string userId)
    {
        try
        {
            // For demo purposes, just return success
            var response = new LogoutResponse
            {
                UserId = userId,
                LoggedOut = true
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}
