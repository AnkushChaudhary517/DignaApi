# Digna API - .NET Core 8.0

A modern, extensible REST API built with .NET Core 8.0 for the Digna application.

## Features

- ✅ User Authentication (Login, Sign Up, Social Login)
- ✅ Email Verification
- ✅ Password Management (Change, Reset)
- ✅ JWT Token-based Authentication
- ✅ User Profile Management
- ✅ Profile Picture Upload
- ✅ In-Memory Database (configurable for production)
- ✅ CORS Support
- ✅ Swagger/OpenAPI Documentation

## Project Structure

```
DignaApi/
├── Controllers/           # API Controllers (AuthController, ProfileController)
├── Services/             # Business Logic Services (AuthService, ProfileService, TokenService, EmailService)
├── Models/
│   ├── Requests/        # API Request DTOs
│   ├── Responses/       # API Response DTOs
├── Entities/            # Database Entities (User, UserProfile)
├── Data/                # Database Context (DignaDbContext)
├── appsettings.json     # Application Configuration
└── Program.cs           # Application Entry Point
```

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Git

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd DignaApi
```

### 2. Install dependencies

```bash
dotnet restore
```

### 3. Run the application

```bash
dotnet run
```

The API will be available at: `https://localhost:5001` or `http://localhost:5000`

### 4. Access Swagger Documentation

Navigate to: `https://localhost:5001/swagger` to view the API documentation.

## API Endpoints

### Authentication

- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/register` - User registration
- `POST /api/v1/auth/social-login` - Social provider login (Google, Facebook, Apple)
- `POST /api/v1/auth/send-verification-email` - Send verification email
- `POST /api/v1/auth/verify-email` - Verify user email
- `POST /api/v1/auth/change-password` - Change password (requires auth)
- `POST /api/v1/auth/forgot-password` - Request password reset
- `POST /api/v1/auth/reset-password` - Reset password
- `POST /api/v1/auth/refresh-token` - Refresh JWT token
- `POST /api/v1/auth/logout` - Logout user (requires auth)

### Profile

- `GET /api/v1/profile` - Get user profile (requires auth)
- `PUT /api/v1/profile` - Update user profile (requires auth)
- `POST /api/v1/profile/upload-picture` - Upload profile picture (requires auth)

## Configuration

### Database Configuration

The application uses an in-memory database by default. To switch to SQL Server:

**appsettings.json:**
```json
{
  "Database": {
    "Provider": "SqlServer"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DignaDb;Trusted_Connection=true;"
  }
}
```

Supported providers:
- `InMemory` (default) - For development/testing
- `SqlServer` - For production

### JWT Configuration

Configure JWT settings in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long",
    "ExpiryMinutes": 1440,
    "RefreshTokenExpiryDays": 7
  }
}
```

### Email Configuration

Configure email settings for production in `appsettings.json`:

```json
{
  "EmailSettings": {
    "FromEmail": "noreply@digna.com",
    "FromName": "Digna",
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password"
  }
}
```

## Default Test Credentials

Two test users are seeded by default:

**User 1:**
- Email: `johndoe@mail.com`
- Password: `securepassword123`

**User 2:**
- Email: `jane.smith@mail.com`
- Password: `securepassword456`

## Authentication Flow

1. User calls `POST /api/v1/auth/login` with email/password
2. Server validates credentials and returns JWT access token + refresh token
3. Client includes access token in `Authorization: Bearer <token>` header
4. When token expires, client calls `POST /api/v1/auth/refresh-token` to get new token
5. User calls `POST /api/v1/auth/logout` to end session

## Error Handling

All endpoints return consistent error responses:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "statusCode": 400
  }
}
```

Common error codes:
- `INVALID_CREDENTIALS` - Wrong email or password
- `EMAIL_ALREADY_EXISTS` - Email already registered
- `USER_NOT_FOUND` - User doesn't exist
- `UNAUTHORIZED` - Missing or invalid token
- `VALIDATION_ERROR` - Invalid input data
- `FILE_TOO_LARGE` - Uploaded file exceeds size limit

## Extending the API

### Adding a New Service

1. Create interface in `Services/IMyService.cs`
2. Implement in `Services/MyService.cs`
3. Register in `Program.cs`: `builder.Services.AddScoped<IMyService, MyService>();`

### Adding a New Controller

1. Create controller in `Controllers/MyController.cs`
2. Inherit from `ControllerBase`
3. Use `[Authorize]` attribute for protected endpoints
4. Return `ApiResponse<T>` using `ApiResponseHelper`

### Switching Database Provider

To use SQL Server instead of in-memory:

1. Update `appsettings.json` - set `Database.Provider` to `SqlServer`
2. Add connection string
3. Create EF Core migrations: `dotnet ef migrations add InitialCreate`
4. Apply migrations: `dotnet ef database update`

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Publishing

```bash
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Port Already in Use

If port 5001 is already in use, modify `launchSettings.json` or run with different port:

```bash
dotnet run --urls "https://localhost:5002"
```

### Database Issues with InMemory

The in-memory database is recreated each time the application starts. For persistence during development, switch to SQL Server.

### JWT Token Validation Errors

Ensure the `SecretKey` in `appsettings.json` is at least 32 characters long and matches the token generation key.

## Production Deployment

Before deploying to production:

1. ✅ Change JWT secret key to a strong value
2. ✅ Configure real email service (SendGrid, AWS SES, etc.)
3. ✅ Switch database to SQL Server or cloud database
4. ✅ Enable HTTPS
5. ✅ Configure CORS properly (restrict to specific origins)
6. ✅ Set appropriate logging levels
7. ✅ Enable rate limiting
8. ✅ Configure backup and disaster recovery

## Technology Stack

- **.NET Core 8.0** - Framework
- **Entity Framework Core** - ORM
- **JWT (System.IdentityModel.Tokens.Jwt)** - Authentication
- **BCrypt.Net-Next** - Password hashing
- **Swagger/OpenAPI** - API Documentation

## License

This project is licensed under the MIT License.

## Support

For issues or questions, please open an issue in the repository.
