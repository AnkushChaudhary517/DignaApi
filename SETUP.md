# DignaApi Setup Guide

This guide will help you set up and run the Digna API locally.

## Prerequisites

Before you begin, ensure you have installed:

1. **.NET 8.0 SDK** - Download from https://dotnet.microsoft.com/download/dotnet/8.0
2. **Visual Studio 2022** (optional) - Download from https://visualstudio.microsoft.com/
   - Or **VS Code** - Download from https://code.visualstudio.com/
3. **Git** - Download from https://git-scm.com/

### Verify Installation

```bash
dotnet --version
git --version
```

## Project Setup

### Step 1: Navigate to Project Directory

```bash
cd DignaApi
```

### Step 2: Restore Dependencies

This downloads all required NuGet packages:

```bash
dotnet restore
```

### Step 3: Run the Application

```bash
dotnet run
```

You should see output like:
```
Building...
Built successfully.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

### Step 4: Access the API

- **Swagger UI**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health (if implemented)

## Development Setup

### Using Visual Studio 2022

1. Open `DignaApi.csproj` in Visual Studio
2. Visual Studio will automatically restore packages
3. Press `F5` to run the application

### Using VS Code

1. Open the folder in VS Code
2. Install the C# extension (Powered by OmniSharp)
3. Open terminal and run:

```bash
dotnet run
```

## Testing API Endpoints

### Option 1: Using Swagger UI

Navigate to `https://localhost:5001/swagger` and use the built-in UI to test endpoints.

### Option 2: Using Postman

1. Download Postman from https://www.postman.com/downloads/
2. Create a new request
3. Example: Login request

**Method:** POST  
**URL:** `https://localhost:5001/api/v1/auth/login`  
**Body (JSON):**
```json
{
  "email": "johndoe@mail.com",
  "password": "securepassword123"
}
```

### Option 3: Using cURL

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "johndoe@mail.com",
    "password": "securepassword123"
  }'
```

### Option 4: Using VS Code REST Client Extension

1. Install "REST Client" extension in VS Code
2. Create a file `requests.http`
3. Add your requests:

```http
### Login
POST https://localhost:5001/api/v1/auth/login
Content-Type: application/json

{
  "email": "johndoe@mail.com",
  "password": "securepassword123"
}
```

## Configuration

### Change Database Provider

Edit `appsettings.json`:

**For InMemory (Default):**
```json
{
  "Database": {
    "Provider": "InMemory"
  }
}
```

**For SQL Server:**
```json
{
  "Database": {
    "Provider": "SqlServer"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DignaDb;Trusted_Connection=true;"
  }
}
```

### Change JWT Secret

⚠️ **Important:** Change the secret key in production!

Edit `appsettings.json`:
```json
{
  "JwtSettings": {
    "SecretKey": "your-new-secret-key-with-at-least-32-characters-length"
  }
}
```

Generate a strong secret:
```bash
# Using PowerShell
[System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString()))

# Using OpenSSL
openssl rand -base64 32
```

## Default Test Users

Login with these credentials:

```
Email: johndoe@mail.com
Password: securepassword123

Email: jane.smith@mail.com
Password: securepassword456
```

## Common Issues & Solutions

### Issue: "It was not possible to find any compatible framework version"

**Solution:** Install .NET 8.0 runtime
```bash
dotnet --list-runtimes
```

### Issue: "Port 5001 is already in use"

**Solution:** Run on different port
```bash
dotnet run --urls "https://localhost:5002"
```

### Issue: SSL Certificate Error

**Solution:** Trust the development certificate
```bash
dotnet dev-certs https --trust
```

### Issue: Database not seeding

**Solution:** Delete in-memory database and restart
```bash
# Just restart the application
dotnet run
```

## Useful Commands

### Build Project
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Clean Build
```bash
dotnet clean
dotnet build
```

### Publish for Production
```bash
dotnet publish -c Release -o ./publish
```

### Add NuGet Package
```bash
dotnet add package PackageName
```

### Remove NuGet Package
```bash
dotnet remove package PackageName
```

### List Installed Packages
```bash
dotnet list package
```

## Entity Framework Core Commands

### Create Migration
```bash
dotnet ef migrations add MigrationName
```

### Update Database
```bash
dotnet ef database update
```

### Drop Database
```bash
dotnet ef database drop
```

### View Migrations
```bash
dotnet ef migrations list
```

## Debugging

### Using Visual Studio
1. Set breakpoints by clicking in the code margin
2. Press `F5` to start debugging
3. Use Debug menu to step through code

### Using VS Code
1. Create `.vscode/launch.json`
2. Install C# extension
3. Press `F5` to debug

## Environment Variables

Create a `.env` file in the root directory:

```
JWT_SECRET_KEY=your-secret-key-here
DATABASE_PROVIDER=InMemory
```

Load in code:
```csharp
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
```

## Troubleshooting Logs

Check application logs for errors:

**In Visual Studio:**
- View > Output
- Select "DignaApi" from dropdown

**In Console:**
- Logs are printed directly

**Log Levels:**
- Debug - Detailed information
- Information - General information
- Warning - Warning messages
- Error - Error messages
- Critical - Critical errors

## Next Steps

1. ✅ Run the API locally
2. ✅ Test endpoints using Swagger
3. ✅ Integrate with frontend application
4. ✅ Implement additional features
5. ✅ Deploy to production

## Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)
- [JWT Authentication](https://jwt.io/)

## Support

If you encounter any issues:

1. Check the error message carefully
2. Review the logs in the console output
3. Check if all prerequisites are installed
4. Try the solutions in "Common Issues" section
5. Search online for the error message
6. Open an issue in the repository

---

**Happy coding! 🚀**
