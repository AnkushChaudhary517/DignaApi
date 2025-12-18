using DignaApi.Entities;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using DignaApi.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DignaApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDynamoDbService _dynamoDbService;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProfileService _profileService;

    public AuthController(IAuthService authService, IDynamoDbService dynamoDbService,
        IConfiguration config, IHttpClientFactory httpClientFactory,
        IProfileService profileService)
    {
        _authService = authService;
        _dynamoDbService = dynamoDbService;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _profileService = profileService;
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
    // STEP 1: Redirect to Google
    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string redirect_uri)
    {
        var googleAuthUrl =
            "https://accounts.google.com/o/oauth2/v2/auth" +
            "?response_type=code" +
            $"&client_id={_config["GoogleAuth:ClientId"]}" +
            $"&redirect_uri={Uri.EscapeDataString(GetCallbackUrl())}" +
            "&scope=openid%20email%20profile" +
            "&access_type=offline" +
            "&prompt=consent" +
            $"&state={Uri.EscapeDataString(redirect_uri)}";

        return Redirect(googleAuthUrl);
    }

    // STEP 2: Google callback
    [HttpGet("callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string state)
    {
        var client = _httpClientFactory.CreateClient();

        // Exchange code for tokens
        var tokenResponse = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", _config["GoogleAuth:ClientId"] },
                { "client_secret", _config["GoogleAuth:ClientSecret"] },
                { "redirect_uri", GetCallbackUrl() },
                { "grant_type", "authorization_code" }
            })
        );

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        Console.WriteLine(tokenData.ToString());
        var idToken = tokenData.GetProperty("id_token").GetString();

        // OPTIONAL: Validate ID token (recommended)
        // You can use GoogleJsonWebSignature here

        // TODO:
        // 1. Create / find user
        // 2. Generate your own JWT
        


        // Redirect back to frontend
        //return Redirect($"{state}?token={idToken}");
        // After creating user and generating JWT tokens:
        return Redirect($"{state}?token={Uri.EscapeDataString(idToken)}");
    }
    [HttpPost("google/exchange")]
    public async Task<IActionResult> ExchangeGoogleToken([FromBody] GoogleTokenExchangeRequest request)
    {
        // Validate Google ID token
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _config["GoogleAuth:ClientId"] }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

        var response = await _authService.GoogleLoginAsync(new GoogleLoginRequest()
        {
            Name = payload.Name,
            Email = payload.Email   
        });

        return Ok(new
        {
            success = true,
            data = new
            {
                userId = response.UserId,
                email = response.Email,
                firstName = response.FirstName,
                lastName = response.LastName,
                profileImage = response.ProfileImage ?? payload.Picture,
                token = response.Token,
                refreshToken = response.RefreshToken
            }
        });
    }

    [HttpGet("facebook")]
    public IActionResult FacebookLogin([FromQuery] string redirect_uri)
    {
        var facebookAuthUrl =
            "https://www.facebook.com/v18.0/dialog/oauth" +
            $"?client_id={_config["FacebookAuth:AppId"]}" +
            $"&redirect_uri={GetFacebookCallbackUrl()}" +
            "&scope=email,public_profile" +
            $"&state={Uri.EscapeDataString(redirect_uri)}";

        return Redirect(facebookAuthUrl);
    }
    [HttpGet("facebook/callback")]
    public async Task<IActionResult> FacebookCallback(
    [FromQuery] string code,
    [FromQuery] string state)
    {
        var client = _httpClientFactory.CreateClient();
        var r = "https://graph.facebook.com/v18.0/oauth/access_token" +
            $"?client_id={_config["FacebookAuth:AppId"]}" +
            $"&redirect_uri={GetFacebookCallbackUrl()}" +
            $"&client_secret={_config["FacebookAuth:AppSecret"]}" + 
            $"&code={code}";
        var res = await client.GetAsync(r);
        var tokenResponse = await res.Content.ReadAsStringAsync();

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponse);
        var accessToken = tokenData.GetProperty("access_token").GetString();

        // Fetch user info
        var userResponse = await client.GetStringAsync(
            "https://graph.facebook.com/me" +
            $"?fields=id,name,email,picture" +
            $"&access_token={accessToken}"
        );

        var userData = JsonSerializer.Deserialize<JsonElement>(userResponse);

        var email = userData.GetProperty("email").GetString();
        var name = userData.GetProperty("name").GetString();
        var facebookId = userData.GetProperty("id").GetString();

        //// TODO:
        //// 1. Create or find user
        //// 2. Generate your JWT

        //var response = await _authService.GoogleLoginAsync(new GoogleLoginRequest()
        //{
        //    Name = name,
        //    Email = email
        //});

        return Redirect($"{state}?email={email}&token={accessToken}&provider=facebook");
    }


    // Controllers/AuthController.cs
    [HttpPost("facebook/exchange")]public async Task<IActionResult> ExchangeFacebookToken([FromBody] FacebookTokenExchangeRequest request)
    {    
        if (string.IsNullOrEmpty(request.AccessToken))   
        {       
            return BadRequest(new {             success = false,             error = new { message = "Access token is required" }         });  
        }    
        try   
        {        
            var client = _httpClientFactory.CreateClient();     
            // Verify token and get user info from Facebook Graph API
            var userInfoResponse = await client.GetAsync($"https://graph.facebook.com/v18.0/me?fields=id,name,email,first_name,last_name,picture&access_token={Uri.EscapeDataString(request.AccessToken)}"        );
            if (!userInfoResponse.IsSuccessStatusCode)        
            {            
                var errorContent = await userInfoResponse.Content.ReadAsStringAsync();
                //_logger.LogError("Facebook Graph API error: {Error}", errorContent);
                return Unauthorized(new {                 success = false,                 error = new { message = "Invalid Facebook token" }             });       
            }        
            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();  
            var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
            // Extract user data from Facebook response
            var facebookId = userInfo.GetProperty("id").GetString();
            var email = userInfo.TryGetProperty("email", out var emailElement)             ? emailElement.GetString()             : null; 
            var firstName = userInfo.TryGetProperty("first_name", out var firstNameElement)             ? firstNameElement.GetString()             : "";  
            var lastName = userInfo.TryGetProperty("last_name", out var lastNameElement)             ? lastNameElement.GetString()             : "";  
            var name = userInfo.TryGetProperty("name", out var nameElement)             ? nameElement.GetString()             : "";     
            // Parse name if first_name/last_name not available
            if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(name))        
            {            
                var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);            
                firstName = nameParts.Length > 0 ? nameParts[0] : "";            
                lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";  
            }        
            // Get profile picture
            
            string profileImage = null;
            if (userInfo.TryGetProperty("picture", out var pictureElement))
            {
                if (pictureElement.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("url", out var urlElement))
                    {
                        profileImage = urlElement.GetString();
                    }
                }
            }
            var user = await _authService.GoogleLoginAsync(new GoogleLoginRequest()
            {
                Name = name,
                Email = email
            });

            await _profileService.UpdateProfileAsync(user.UserId, new UpdateProfileRequest()
            {
                ImageUrl = profileImage
            });
            return Ok(new       
             {            success = true,            
                 data = new          
                    {               
                     userId = user.UserId,         
                     email = user.Email,            
                     firstName = user.FirstName,    
                     lastName = user.LastName,    
                     profileImage = user.ProfileImage ?? profileImage,     
                     token = user.Token,            
                     refreshToken = user.RefreshToken
                 }        });
        }    catch (JsonException ex)   
        {      
            //_logger.LogError(ex, "Error parsing Facebook response");
            return Unauthorized(new {             success = false,             error = new { message = "Invalid Facebook response" } 
            });   
        }    catch (Exception ex) 
        {        
            //_logger.LogError(ex, "Error exchanging Facebook token");     
            return StatusCode(500, new {             success = false,    
                error = new { message = "Internal server error" }      
            });  
        }}


    public class GoogleTokenExchangeRequest
    {
        public string IdToken { get; set; }
    }

    private string GetFacebookCallbackUrl()
    {
        return "http://digna-photography-s3.s3-website.ap-south-1.amazonaws.com:5000/api/v1/facebook/auth/callback";
        //return $"{Request.Scheme}://{Request.Host}/api/v1/auth/facebook/callback";
    }

    private string GetCallbackUrl()
    {
        return "http://digna-photography-s3.s3-website.ap-south-1.amazonaws.com:5000/api/v1/auth/callback";
        //return $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback";
    }
    private async Task<GoogleJsonWebSignature.Payload> ValidateIdToken(string idToken)
    {
        return await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["GoogleAuth:ClientId"] }
            });
    }
}
