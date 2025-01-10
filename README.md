# whateverAPI - A Joke Management API. or whatever

<img src="whateverAPI/wwwroot/mascot.svg" alt="whateverAPI" width="256" height="256">

[![wakatime](https://wakatime.com/badge/user/633fcbd8-9377-4acb-9977-248bcf7b615b/project/865bcd59-7dfb-43a6-995c-ed1fb3762774.svg)](https://wakatime.com/badge/user/633fcbd8-9377-4acb-9977-248bcf7b615b/project/865bcd59-7dfb-43a6-995c-ed1fb3762774)
## Overview
whateverAPI is a robust, RESTful API built with .NET 9.0 that specializes in managing and serving various types of humorous content. It provides a comprehensive set of endpoints for creating, retrieving, and managing jokes with advanced features like categorization, tagging, and engagement tracking.

## Features

### Core Functionality
- ✨ Full CRUD operations for joke management
- 🎲 Random joke retrieval with filtering options
- 🏷️ Advanced categorization system
- 📊 Engagement tracking with laugh scores
- 🔍 Flexible search and filtering capabilities

### Technical Features
- 🚀 Built on .NET 9.0 for optimal performance
- ⚡ FastEndpoints for efficient endpoint handling
- ✅ Comprehensive validation using FluentValidation
- 📝 Detailed Swagger/OpenAPI documentation
- 🐳 Docker support for easy deployment

## Project Structure

```
whateverAPI/
├── Features/
│   └── Jokes/
│       ├── CreateJoke/
│       │   ├── CreateJokeEndpoint.cs
│       │   ├── CreateJokeRequest.cs
│       │   ├── CreateJokeResponse.cs
│       │   ├── CreateJokeValidator.cs
│       │   └── CreateJokeMapper.cs
│       ├── GetJoke/
│       │   ├── GetJokeEndpoint.cs
│       │   ├── GetJokeRequest.cs
│       │   ├── GetJokeResponse.cs
│       │   ├── GetJokeValidator.cs
│       │   └── GetJokeMapper.cs
│       ├── GetJokesByType/
│       │   └── [Similar structure]
│       └── GetRandomJoke/
│           └── [Similar structure]
├── Models/
│   ├── JokeEntry.cs
│   └── JokeType.cs
├── Services/
│   ├── IJokeService.cs
│   └── JokeService.cs
└── Program.cs
```

## API Endpoints

### Create Joke
```http
POST /api/jokes
```
Create a new joke entry.

**Request Body:**
```json
{
  "content": "Why did the developer quit his job? He didn't get arrays!",
  "type": "Joke",
  "tags": ["programming", "work"]
}
```

**Response (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "Why did the developer quit his job? He didn't get arrays!",
  "type": "Joke",
  "tags": ["programming", "work"],
  "createdAt": "2024-12-29T10:30:00Z",
  "laughScore": 0
}
```

### Get Random Joke
```http
GET /api/jokes/random
```
Retrieve a random joke from the collection.

**Response (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "...",
  "type": "Joke",
  "tags": ["programming", "work"],
  "createdAt": "2024-12-29T10:30:00Z",
  "laughScore": 42
}
```

### Get Jokes By Type
```http
GET /api/jokes/type/{type}
```
Retrieve jokes of a specific type with optional filtering and pagination.

**Parameters:**
- `type` (path): Joke type (Joke, FunnySaying, Discouragement, SelfDeprecating)
- `pageSize` (query, optional): Number of items per page (default: 10)
- `pageNumber` (query, optional): Page number (default: 1)
- `sortBy` (query, optional): Property to sort by (createdAt, laughScore, content)
- `sortDescending` (query, optional): Sort direction (default: false)

**Example:**
```http
GET /api/jokes/type/Joke?pageSize=10&pageNumber=1&sortBy=laughScore&sortDescending=true
```

## Setup and Installation

### Prerequisites
- .NET 9.0 SDK
- Docker (optional)
- An IDE (Visual Studio 2022 or JetBrains Rider recommended)

### Local Development Setup

1. **Clone the Repository**
```bash
git clone https://github.com/yourusername/whateverAPI.git
cd whateverAPI
```

2. **Restore Dependencies**
```bash
dotnet restore
```

3. **Build the Project**
```bash
dotnet build
```

4. **Run the Project**
```bash
dotnet run --project whateverAPI
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger

### Docker Setup

1. **Build the Docker Image**
```bash
docker build -t whatever-api .
```

2. **Run the Container**
```bash
docker run -p 8080:80 whatever-api
```

## Validation Rules

### Create Joke Validation
- Content:
    - Required
    - Minimum length: 10 characters
    - Maximum length: 500 characters
- Type:
    - Must be a valid JokeType enum value
- Tags:
    - Maximum 5 tags
    - Each tag maximum length: 20 characters
    - No special characters in tags

### Get Jokes By Type Validation
- Type:
    - Must be a valid JokeType enum value
- PageSize:
    - Range: 1-100
- PageNumber:
    - Must be greater than 0
- SortBy:
    - Must be one of: createdAt, laughScore, content

## Error Handling

The API uses standard HTTP status codes and returns detailed error messages:

```json
{
  "errors": {
    "Content": ["Content must be between 10 and 500 characters."],
    "Type": ["Invalid joke type specified."],
    "Tags": ["Maximum 5 tags allowed."]
  }
}
```

Common Status Codes:
- 200: Success
- 201: Created
- 400: Bad Request (validation error)
- 404: Not Found
- 500: Internal Server Error

## Development Guidelines

### Adding a New Endpoint

1. Create a new feature folder under `Features/Jokes/`
2. Create the necessary files:
    - `*Endpoint.cs`
    - `*Request.cs`
    - `*Response.cs`
    - `*Validator.cs`
    - `*Mapper.cs`
3. Implement the endpoint following the existing patterns
4. Update the service layer if needed
5. Add appropriate tests

### Code Style
- Use C# latest features and best practices
- Follow FastEndpoints conventions
- Implement proper validation
- Include XML documentation
- Add appropriate logging

## Testing

Run the test suite:
```bash
dotnet test
```

### Test Categories
- Unit Tests: Testing individual components
- Integration Tests: Testing endpoint behavior
- Service Tests: Testing business logic

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Contribution Guidelines
- Follow the existing code style
- Add/update tests as needed
- Update documentation
- Follow conventional commits

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- FastEndpoints team for the excellent framework
- The .NET community for continuous support
- All contributors who help improve this project

## Support

For support, please open an issue in the GitHub repository or contact the maintainers.