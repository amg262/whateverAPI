using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Models;

/// <summary>
/// Represents a request to create a new tag in the system, implementing validation rules
/// to ensure data consistency and security.
/// </summary>
public record CreateTagRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// Validates tag creation requests to ensure they meet system requirements
    /// and maintain data consistency.
    /// </summary>
    public class Validator : AbstractValidator<CreateTagRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tag name is required")
                .MinimumLength(2).WithMessage("Tag name must be at least 2 characters")
                .MaximumLength(50).WithMessage("Tag name cannot exceed 50 characters")
                .Matches("^[a-zA-Z0-9-_]+$")
                .WithMessage("Tag name can only contain letters, numbers, hyphens and underscores");
        }
    }
}

/// <summary>
/// Represents a request to update an existing tag in the system, with validation rules
/// </summary>
public record UpdateTagRequest
{
    public required string Name { get; init; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Validates tag update requests to ensure data consistency and security.
    /// </summary>
    public class Validator : AbstractValidator<UpdateTagRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tag name is required")
                .MinimumLength(2).WithMessage("Tag name must be at least 2 characters")
                .MaximumLength(50).WithMessage("Tag name cannot exceed 50 characters")
                .Matches("^[a-zA-Z0-9-_]+$")
                .WithMessage("Tag name can only contain letters, numbers, hyphens and underscores");
        }
    }
}

/// <summary>
/// Represents a request to create a new joke in the system, with comprehensive
/// validation for content, type, and associated tags.
/// </summary>
public record CreateJokeRequest
{
    public required string Content { get; init; }
    public JokeType Type { get; init; }
    public List<string>? Tags { get; init; }
    public int? LaughScore { get; init; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Validates joke creation requests to ensure content quality and system integrity.
    /// </summary>
    public class Validator : AbstractValidator<CreateJokeRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required")
                .MinimumLength(5).WithMessage("Content must be at least 10 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid joke type");

            RuleFor(x => x.Tags)
                .Must(tags => tags?.Count <= 10)
                .WithMessage("Maximum 5 tags allowed")
                .When(x => x.Tags != null);

            RuleForEach(x => x.Tags)
                .MaximumLength(20)
                .WithMessage("Tag length cannot exceed 20 characters")
                .When(x => x.Tags != null);
        }
    }
}

/// <summary>
/// Represents a request to update an existing joke in the system, with validation rules
/// </summary>
public record UpdateJokeRequest
{
    public required string Content { get; init; }
    public JokeType Type { get; init; }
    public List<string>? Tags { get; init; }
    public int? LaughScore { get; init; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Validates joke update requests to ensure data consistency and security.
    /// </summary>
    public class Validator : AbstractValidator<UpdateJokeRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
        }
    }
}

/// <summary>
/// Represents a request to delete a joke from the system, with validation rules
/// </summary>
public record DeleteJokeRequest
{
    public Guid Id { get; init; }

    public class Validator : AbstractValidator<DeleteJokeRequest>
    {
        /// <summary>
        /// Validates joke deletion requests to ensure the ID is provided.
        /// </summary>
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("The ID cannot be empty.");
            // .Must(id => id != Guid.Empty)
            // .WithMessage("The ID must be a valid GUID.");
        }
    }
}

/// <summary>
/// Represents a request to filter and sort jokes based on multiple criteria,
/// supporting pagination for efficient data retrieval.
/// </summary>
public record FilterRequest
{
    public bool? Active { get; init; } = true;
    public JokeType? Type { get; init; }
    public string? Query { get; init; }
    public int? PageSize { get; init; }
    public int? PageNumber { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; } = false;

    /// <summary>
    /// Validates filter requests to ensure efficient and valid queries.
    /// </summary>
    public class Validator : AbstractValidator<FilterRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid joke type. Available types are: Joke, FunnySaying, Discouragement, SelfDeprecating");

            When(x => x.PageSize.HasValue, () =>
            {
                RuleFor(x => x.PageSize!.Value)
                    .InclusiveBetween(1, 100)
                    .WithMessage("Page size must be between 1 and 100");
            });

            When(x => x.PageNumber.HasValue, () =>
            {
                RuleFor(x => x.PageNumber!.Value)
                    .GreaterThan(0)
                    .WithMessage("Page number must be greater than 0");

                RuleFor(x => x.PageSize)
                    .NotNull()
                    .WithMessage("Page size is required when using pagination");
            });

            When(x => !string.IsNullOrEmpty(x.SortBy), () =>
            {
                RuleFor(x => x.SortBy)
                    .Must(sortBy =>
                        new[] { "createdAt", "modifiedAt", "laughScore", "content", "tag", "active" }.Contains(
                            sortBy?.ToLower()))
                    .WithMessage("Sort by field must be one of: createdAt, laughScore, content");

                // RuleFor(x => x.SortDescending)
                //     .NotNull()
                //     .WithMessage("Sort direction is required when using sorting");
            });
        }
    }
}

/// <summary>
/// Represents a user login request with comprehensive validation for
/// username, email, and password requirements.
/// </summary>
public record UserLoginRequest
{
    public string Username { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }

    /// <summary>
    /// Validates login requests to ensure security and data quality.
    /// </summary>
    public class Validator : AbstractValidator<UserLoginRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
        }
    }
}