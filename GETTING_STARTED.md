# Getting Started with DignaApi

Quick start guide to get the API running in 5 minutes.

## 🚀 Quick Start (5 minutes)

### Step 1: Prerequisites
Ensure you have .NET 8.0 SDK installed:
```bash
dotnet --version
```
Should show version 8.0.x or higher.

### Step 2: Navigate to Project
```bash
cd DignaApi
```

### Step 3: Run the API
```bash
dotnet run
```

### Step 4: Test the API
Open your browser and go to:
```
https://localhost:5001/swagger
```

**That's it!** Your API is now running. 🎉

## 📝 Default Test Credentials

Login with these credentials in Swagger or Postman:

```
Email: johndoe@mail.com
Password: securepassword123
```

## 🔌 Making Your First API Call

### Via Swagger UI (Easiest)
1. Open https://localhost:5001/swagger
2. Click on "POST /api/v1/auth/login"
3. Click "Try it out"
4. Enter credentials:
```json
{
  "email": "johndoe@mail.com",
  "password": "securepassword123"
}
```
5. Click "Execute"
6. You'll get a JWT token in response

### Via cURL
```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "johndoe@mail.com",
    "password": "securepassword123"
  }' --insecure
```

### Via Postman
1. Create new POST request
2. URL: `https://localhost:5001/api/v1/auth/login`
3. Go to "Body" tab
4. Select "raw" and "JSON"
5. Paste credentials
6. Click Send

## 🔐 Using the JWT Token

After logging in, you'll get a response like:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "expiresIn": 86400
  },
  "message": "Login successful"
}
```

To access protected endpoints (like `/api/v1/profile`), include the token in headers:

```bash
curl -X GET https://localhost:5001/api/v1/profile \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  --insecure
```

In Swagger, click the "Authorize" button and paste the token.

## 📚 Available Endpoints

### Authentication
- `POST /api/v1/auth/login` - Login
- `POST /api/v1/auth/register` - Sign up
- `POST /api/v1/auth/social-login` - Social login
- `POST /api/v1/auth/verify-email` - Verify email
- `POST /api/v1/auth/change-password` - Change password
- `POST /api/v1/auth/logout` - Logout

### Profile
- `GET /api/v1/profile` - Get profile
- `PUT /api/v1/profile` - Update profile
- `POST /api/v1/profile/upload-picture` - Upload picture

See [API_DOCUMENTATION.md](../API_DOCUMENTATION.md) for full details.

## 🔧 Configuration

### Change Database
Edit `appsettings.json`:

```json
{
  "Database": {
    "Provider": "InMemory"
  }
}
```

Options: `InMemory` (default) or `SqlServer`

### Change JWT Secret
⚠️ **Change this before production!**

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long"
  }
}
```

## 💾 Database

The API uses an in-memory database by default (H2 Database equivalent in .NET).

**Data persists during the session but resets when the application stops.**

To use a real database:
1. Change `Provider` to `SqlServer` in `appsettings.json`
2. Add connection string
3. Create migrations: `dotnet ef migrations add InitialCreate`
4. Run migrations: `dotnet ef database update`

## 📖 Documentation

- **[README.md](README.md)** - Full project documentation
- **[SETUP.md](SETUP.md)** - Detailed setup guide
- **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** - Project organization
- **[../API_DOCUMENTATION.md](../API_DOCUMENTATION.md)** - API reference

## 🎯 Next Steps

1. ✅ Run the API
2. ✅ Test endpoints in Swagger
3. ✅ Review the API documentation
4. ✅ Connect your frontend
5. ✅ Deploy to production

## ⚡ Common Commands

```bash
# Run the API
dotnet run

# Run in watch mode (auto-reload on changes)
dotnet watch run

# Build
dotnet build

# Clean build
dotnet clean && dotnet build

# Run tests
dotnet test

# Publish for production
dotnet publish -c Release -o ./publish
```

## 🐛 Troubleshooting

### Port 5001 already in use?
```bash
dotnet run --urls "https://localhost:5002"
```

### SSL Certificate Error?
```bash
dotnet dev-certs https --trust
```

### InMemory database empty?
The database resets when you stop the app. Just restart it.

### Token not working?
- Make sure token is valid (not expired)
- Include "Bearer " prefix in Authorization header
- Check the header format: `Authorization: Bearer <token>`

## 📱 Frontend Integration

To connect your frontend:

1. **Update API URL** in your frontend config:
```javascript
const API_URL = 'https://localhost:5001/api/v1';
```

2. **CORS is already enabled** for all origins (development mode)

3. **Store token** after login:
```javascript
const response = await fetch(`${API_URL}/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});

const data = await response.json();
localStorage.setItem('token', data.data.token);
```

4. **Use token** for protected endpoints:
```javascript
const response = await fetch(`${API_URL}/profile`, {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

## 🚀 Deployment

When deploying to production:

1. ✅ Change database to SQL Server or Azure SQL
2. ✅ Generate strong JWT secret key
3. ✅ Configure real email service
4. ✅ Update CORS to specific origins
5. ✅ Enable HTTPS
6. ✅ Set up logging and monitoring
7. ✅ Configure backups

See [README.md](README.md) for production checklist.

## ❓ FAQ

**Q: Can I use this API with my React/Vue/Angular app?**  
A: Yes! The API is completely independent. Just call the endpoints from your frontend.

**Q: How do I add more fields to the user profile?**  
A: Add properties to the `UserProfile` entity, create a migration, and update the API response models.

**Q: Is the in-memory database suitable for production?**  
A: No. Use SQL Server, PostgreSQL, or a cloud database for production.

**Q: How do I reset the database?**  
A: For in-memory database, just restart the application. For SQL Server, run `dotnet ef database drop --force`.

**Q: Can I use this with a mobile app?**  
A: Absolutely! The API is RESTful and works with any client that can make HTTP requests.

**Q: How do I add authentication for my mobile app users?**  
A: The same way as web apps - register and login via the API endpoints. Mobile apps can store JWT in secure storage.

## 🆘 Getting Help

- Check [SETUP.md](SETUP.md) for detailed setup instructions
- Review [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for project organization
- Check logs in console output for errors
- Open an issue in the repository

---

**Ready to build? Let's go! 🚀**
