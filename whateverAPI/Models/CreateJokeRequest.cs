using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Models;

public record CreateJokeRequest
{
    public required string Content { get; init; }
    public JokeType Type { get; init; }
    public List<string>? Tags { get; init; }
    public int? LaughScore { get; init; }

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

public record UpdateJokeRequest
{
    public required string Content { get; init; }
    public JokeType Type { get; init; }
    public List<string>? Tags { get; init; }
    public int? LaughScore { get; init; }

    public class Validator : AbstractValidator<UpdateJokeRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
        }
    }
}