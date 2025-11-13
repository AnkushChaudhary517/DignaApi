# DignaApi Project Structure

Complete project structure and file organization for the .NET Core 8.0 API.

## Directory Tree

```
DignaApi/
│
├── Controllers/
│   ├── AuthController.cs              # Authentication endpoints
│   └── ProfileController.cs           # User profile endpoints
│
├── Services/
│   ├── IAuthService.cs               # Authentication service interface
│   ├── AuthService.cs                # Authentication service implementation
│   ├── IProfileService.cs            # Profile service interface
│   ├── ProfileService.cs             # Profile service implementation
│   ├── ITokenService.cs              # JWT token service interface
│   ├── TokenService.cs               # JWT token service implementation
│   ├── IEmailService.cs              # Email service interface
│   └── EmailService.cs               # Email service implementation (demo)
│
├── Models/
│   ├── Requests/
│   │   ├── AuthRequests.cs           # Auth request DTOs (Login, Register, etc.)
│   │   └── ProfileRequests.cs        # Profile request DTOs
│   └── Responses/
│       ├── ApiResponse.cs            # Generic API response wrapper
│       ├── AuthResponses.cs          # Auth response DTOs
│       └── ProfileResponses.cs       # Profile response DTOs
│
├── Entities/
│   ├── User.cs                       # User entity (database model)
│   └── UserProfile.cs                # UserProfile entity (database model)
│
├── Data/
│   └── DignaDbContext.cs             # Entity Framework DbContext
│
├── appsettings.json                  # Production configuration
├── appsettings.Development.json      # Development configuration
├── DignaApi.csproj                   # Project file with NuGet packages
├── Program.cs                        # Application entry point & startup config
├── README.md                         # Project README
├── SETUP.md                          # Setup and installation guide
├── PROJECT_STRUCTURE.md              # This file
└── .gitignore                        # Git ignore rules
```

## File Descriptions

### Controllers

#### AuthController.cs
Handles all authentication-related endpoints:
- Login
- Registration
- Social login
- Email verification
- Password change/reset
- Token refresh
- Logout

#### ProfileController.cs
Handles user profile management:
- Get profile (requires authentication)
- Update profile (requires authentication)
- Upload profile picture (requires authentication)

### Services

#### IAuthService & AuthService
Business logic for authentication operations:
- User login with email/password
- User registration
- Social provider login (Google, Facebook, Apple)
- Email verification
- Password management
- Token refresh
- Logout

#### IProfileService & ProfileService
Business logic for profile management:
- Retrieve user profile
- Update user profile information
- Handle profile picture uploads

#### ITokenService & TokenService
JWT token management:
- Generate access tokens
- Generate refresh tokens
- Validate tokens
- Handle token expiration

#### IEmailService & EmailService
Email operations (demo implementation):
- Send verification emails
- Send password reset emails
- Send welcome emails
- Extensible for SendGrid, AWS SES, etc.

### Models

#### Requests (Request DTOs)
- `LoginRequest` - Email & password
- `RegisterRequest` - Name, email, password
- `SocialLoginRequest` - Provider, tokens
- `VerifyEmailRequest` - Email, code
- `SendVerificationEmailRequest` - Email only
- `ChangePasswordRequest` - Current & new password
- `ForgotPasswordRequest` - Email only
- `ResetPasswordRequest` - Email, token, new password
- `RefreshTokenRequest` - Refresh token
- `UpdateProfileRequest` - Profile update fields
- `SocialLinksDto` - Social media links
- `UploadProfilePictureRequest` - File upload

#### Responses (Response DTOs)
- `ApiResponse<T>` - Generic response wrapper
- `ErrorDetails` - Error information
- `LoginResponse` - User data + tokens
- `RegisterResponse` - New user data
- `SocialLoginResponse` - Social login data
- `VerifyEmailResponse` - Verification confirmation
- `SendVerificationEmailResponse` - Email sent confirmation
- `ChangePasswordResponse` - Success confirmation
- `ForgotPasswordResponse` - Reset email sent
- `ResetPasswordResponse` - Reset confirmation
- `RefreshTokenResponse` - New tokens
- `LogoutResponse` - Logout confirmation
- `ProfileResponse` - User profile data
- `UpdateProfileResponse` - Updated profile data
- `UploadProfilePictureResponse` - Upload confirmation

### Entities

#### User.cs
Database entity representing a user:
- `Id` - Unique identifier
- `Email` - User email (unique)
- `FirstName` - First name
- `LastName` - Last name
- `PasswordHash` - Hashed password (BCrypt)
- `EmailVerified` - Email verification status
- `VerificationToken` - Token for email verification
- `ResetToken` - Token for password reset
- `ResetTokenExpiry` - Reset token expiration time
- `CreatedAt` - Account creation timestamp
- `UpdatedAt` - Last update timestamp
- Navigation: `Profile` (1:1 relationship)

#### UserProfile.cs
Database entity for user profile information:
- `Id` - Unique identifier
- `UserId` - Foreign key to User
- `ProfileImageUrl` - URL to profile picture
- `Bio` - User biography
- `Website` - Personal website URL
- `Instagram` - Instagram profile URL
- `Twitter` - Twitter/X profile URL
- `Youtube` - YouTube channel URL
- `Pinterest` - Pinterest profile URL
- `Newsletter` - Newsletter subscription status
- `CreatedAt` - Profile creation timestamp
- `UpdatedAt` - Last update timestamp
- Navigation: `User` (1:1 relationship)

### Data

#### DignaDbContext.cs
Entity Framework Core DbContext:
- `DbSet<User>` - Users table
- `DbSet<UserProfile>` - User profiles table
- Model configuration and relationships
- Seed data for test users
- Support for multiple database providers (InMemory, SqlServer)

### Configuration Files

#### DignaApi.csproj
NuGet package dependencies:
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.InMemory
- Microsoft.EntityFrameworkCore.SqlServer
- System.IdentityModel.Tokens.Jwt
- BCrypt.Net-Next

#### appsettings.json
Production configuration:
- Logging levels
- Database provider (InMemory/SqlServer)
- Connection strings
- JWT settings (secret key, expiry)
- Email settings (SMTP configuration)

#### appsettings.Development.json
Development-specific overrides:
- Debug logging
- Development database configuration

#### Program.cs
Application startup configuration:
- Service registration
- Database configuration
- JWT authentication setup
- CORS configuration
- Middleware pipeline
- Database seeding

## Data Flow

### Authentication Flow
```
LoginRequest → AuthController → AuthService → User Entity (Database)
                ↓
            TokenService (Generate JWT)
                ↓
            LoginResponse (with token)
```

### Profile Update Flow
```
UpdateProfileRequest → ProfileController → ProfileService → Database
                      ↓
                  EF Core
                      ���
                  UserProfile Entity
```

### File Upload Flow
```
MultipartForm (File) → ProfileController → ProfileService → File Storage
                            ↓
                       Database (URL)
```

## API Routes

### Authentication Routes
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/social-login`
- `POST /api/v1/auth/send-verification-email`
- `POST /api/v1/auth/verify-email`
- `POST /api/v1/auth/change-password`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `POST /api/v1/auth/refresh-token`
- `POST /api/v1/auth/logout`

### Profile Routes
- `GET /api/v1/profile`
- `PUT /api/v1/profile`
- `POST /api/v1/profile/upload-picture`

## Design Patterns Used

1. **Dependency Injection** - Services injected via constructor
2. **Repository Pattern** - DbContext abstracts data access
3. **DTO Pattern** - Separate request/response models
4. **Service Pattern** - Business logic in services
5. **Factory Pattern** - ApiResponseHelper creates responses
6. **Middleware Pattern** - Authentication via JWT middleware
7. **Configuration Pattern** - Settings via appsettings.json

## Technology Stack

| Technology | Purpose |
|-----------|---------|
| .NET 8.0 | Framework |
| ASP.NET Core | Web framework |
| Entity Framework Core | ORM |
| JWT | Authentication |
| BCrypt | Password hashing |
| Swagger | API documentation |
| InMemory Database | Development |
| SQL Server | Production (optional) |

## Extensibility Points

### Adding a New Entity
1. Create class in `Entities/`
2. Add `DbSet<Entity>` to `DignaDbContext`
3. Configure in `OnModelCreating()`

### Adding a New Service
1. Create `INewService.cs` interface
2. Create `NewService.cs` implementation
3. Register in `Program.cs`

### Adding a New Controller
1. Create `NewController.cs`
2. Inject dependencies via constructor
3. Create endpoints using `[HttpMethod]` attributes

### Switching Database Provider
1. Update `appsettings.json`
2. Uncomment SqlServer registration in `Program.cs`
3. Create EF migrations
4. Apply migrations

## Security Features

- ✅ Password hashing with BCrypt
- ✅ JWT token-based authentication
- ✅ Authorization attributes on protected endpoints
- ✅ CORS configuration
- ✅ Token expiration handling
- ✅ Password reset with time-limited tokens
- ✅ Email verification

## Future Enhancements

- [ ] Real email service integration (SendGrid, AWS SES)
- [ ] Rate limiting
- [ ] Audit logging
- [ ] Two-factor authentication
- [ ] API key authentication
- [ ] Role-based access control (RBAC)
- [ ] Resource-based access control (RBAC)
- [ ] Caching layer (Redis)
- [ ] GraphQL endpoint
- [ ] Message queue integration (RabbitMQ)
- [ ] Health checks endpoint
- [ ] Metrics and monitoring (Application Insights)

---

This structure follows clean architecture principles and is designed for scalability and maintainability.
