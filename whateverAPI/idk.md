# WhateverAPI - A Modern Joke Management System

<img src="whateverAPI/wwwroot/mascot.svg" alt="whateverAPI" width="256" height="256">

## Overview
WhateverAPI is a robust RESTful API built with .NET 9.0 that provides comprehensive joke management capabilities. It features advanced querying, tagging, and engagement tracking systems, along with integration with third-party joke providers.

## Core Features

### Joke Management
- Complete CRUD operations for jokes with validation
- Advanced filtering and pagination
- Tag-based organization
- Engagement tracking with laugh scores
- Rich search capabilities
- Active/Inactive state management

### Authentication & Security
- JWT-based authentication
- Google OAuth2.0 integration
- Secure token management
- Role-based access control
- CORS policy management

### Technical Features
- Built on .NET 9.0
- Entity Framework Core with PostgreSQL
- Fluent Validation for request validation
- Comprehensive error handling with Problem Details
- Swagger/OpenAPI documentation with Scalar
- Docker containerization
- Polly for resilient HTTP calls
- Structured logging

## Project Structure

```
src/
├── Data/
│   ├── AppDbContext.cs           # EF Core database context
│   ├── BaseRepository.cs         # Generic repository pattern implementation
│   ├── DbInitializer.cs         # Database initialization and seeding
│   ├── JokeRepository.cs        # Joke-specific repository implementation
│   └── IEntity.cs               # Base entity interface
├── Entities/
│   ├── Joke.cs                  # Joke entity model
│   ├── Tag.cs                   # Tag entity model
│   ├── JokeTag.cs              # Many-to-many relationship entity
│   └── JokeType.cs             # Joke type enumeration
├── Services/
│   ├── JokeService.cs          # Business logic for jokes
│   ├── TagService.cs           # Tag management service
│   ├── JwtTokenService.cs      # JWT token handling
│   ├── GoogleAuthService.cs    # Google OAuth implementation
│   └── JokeApiService.cs       # External joke API integration
├── Models/
│   ├── Request.cs              # Request DTOs
│   └── Response.cs             # Response DTOs
├── Helpers/
│   ├── Extensions.cs           # Extension methods
│   ├── GlobalException.cs      # Global exception handler
│   ├── ProblemDetailsHelper.cs # Problem Details formatting
│   ├── QueryHelper.cs          # Query building utilities
│   └── ValidationFilter.cs     # Request validation filter
└── Program.cs                  # Application entry point and configuration
```

## API Endpoints

### Joke Management

#### Create Joke
```http
POST /api/jokes
```
Creates a new joke with content, type, and optional tags.

**Request Body:**
```json
{
  "content": "Why did the developer quit his job? He didn't get arrays!",
  "type": "Programming",
  "tags": ["programming", "work"],
  "isActive": true
}
```

#### Get Joke
```http
GET /api/jokes/{id}
```
Retrieves a specific joke by ID.

#### Search and Filter Jokes
```http
POST /api/jokes/find
```
Advanced search with multiple criteria:

**Request Body:**
```json
{
  "type": "Programming",
  "query": "developer",
  "active": true,
  "pageSize": 10,
  "pageNumber": 1,
  "sortBy": "laughScore",
  "sortDescending": true
}
```

### Tag Management

#### Create Tag
```http
POST /api/tags
```
Creates a new tag for categorizing jokes.

**Request Body:**
```json
{
  "name": "programming",
  "isActive": true
}
```

### Authentication

#### User Login
```http
POST /api/user/login
```
Authenticates a user and returns a JWT token.

#### Google OAuth Login
```http
GET /api/auth/google/login
```
Initiates Google OAuth2.0 authentication flow.

## Setup and Installation

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 15+
- Docker (optional)
- IDE (Visual Studio 2022 or JetBrains Rider recommended)

### Local Development

1. Clone the repository:
```bash
git clone https://github.com/yourusername/whateverAPI.git
cd whateverAPI
```

2. Update database connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=whateverdb;Username=postgres;Password=postgrespw;"
  }
}
```

3. Run the application:
```bash
dotnet restore
dotnet run
```

### Docker Deployment

1. Build using docker-compose:
```bash
docker-compose build
```

2. Run the containers:
```bash
docker-compose up -d
```

## Configuration

### JWT Settings
Configure JWT authentication in `appsettings.json`:
```json
{
  "JwtOptions": {
    "Secret": "your-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "ExpirationInDays": 90
  }
}
```

### Google OAuth
Configure Google OAuth in `appsettings.json`:
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "RedirectUri": "https://your-domain/api/auth/google/callback"
    }
  }
}
```

### CORS Policy
Configure CORS in `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:8080",
      "https://your-frontend-domain"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["Content-Type", "Authorization"]
  }
}
```

## Error Handling

The API uses RFC 7807 Problem Details for error responses:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Joke with identifier '123' was not found",
  "instance": "/api/jokes/123",
  "traceId": "00-1234567890abcdef-abcdef1234567890-00",
  "timestamp": "2024-01-10T12:00:00Z"
}
```

## Validation

Request validation is implemented using FluentValidation:

```csharp
public class CreateJokeValidator : AbstractValidator<CreateJokeRequest>
{
    public CreateJokeValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("Content must be at least 5 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid joke type");

        RuleFor(x => x.Tags)
            .Must(tags => tags?.Count <= 10)
            .WithMessage("Maximum 10 tags allowed");
    }
}
```

## Database Schema

### Jokes Table
- Id (GUID)
- Content (string)
- Type (enum)
- CreatedAt (datetime)
- ModifiedAt (datetime)
- LaughScore (int?)
- IsActive (bool)

### Tags Table
- Id (GUID)
- Name (string)
- CreatedAt (datetime)
- ModifiedAt (datetime)
- IsActive (bool)

### JokeTags Table (Many-to-Many)
- JokeId (GUID)
- TagId (GUID)

## Testing

The project includes a custom API tester tool (`api_tester.py`) for performance and load testing:

```bash
python api_tester.py http://your-api-url -i 1.0 -n 100
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

### Coding Standards
- Follow C# best practices and conventions
- Include XML documentation for public APIs
- Add appropriate logging
- Include unit tests for new features
- Update documentation as needed

## License

This project is licensed under the MIT License.

## Support

For support, please open an issue in the GitHub repository.