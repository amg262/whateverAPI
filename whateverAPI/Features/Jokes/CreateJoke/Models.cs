using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.CreateJoke;

public record Request
{
    public required string Content { get; init; } = string.Empty;
    public JokeType Type { get; init; }
    public List<string>? Tags { get; init; } = [];
    public int? LaughScore { get; init; }
}

public class Mapper : Mapper<Request, JokeResponse, Joke>
{
    public override Joke ToEntity(Request r) => new()
    {
        Id = Guid.CreateVersion7(),
        Content = r.Content,
        Type = r.Type,
        CreatedAt = DateTime.UtcNow,
        LaughScore = r.LaughScore ?? 0,
        Tags = r.Tags?.Select(tagName => new Tag { Id = Guid.CreateVersion7(), Name = tagName }).ToList() ?? []
    };

    public override JokeResponse FromEntity(Joke j) => new()
    {
        Id = j.Id,
        Content = j.Content,
        Type = j.Type,
        Tags = j.Tags?.Select(t => t.Name).ToList() ?? [],
        CreatedAt = j.CreatedAt,
        LaughScore = j.LaughScore
    };
}

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");
        // .MinimumLength(10).WithMessage("Content must be at least 10 characters")
        // .MaximumLength(500).WithMessage("Content cannot exceed 500 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid joke type");

        // RuleFor(x => x.Tags)
        //     .Must(tags => tags.Count <= 5)
        //     .WithMessage("Maximum 5 tags allowed")
        //     .When(x => x.Tags != null);
        //
        // RuleForEach(x => x.Tags)
        //     .MaximumLength(20)
        //     .WithMessage("Tag length cannot exceed 20 characters")
        //     .When(x => x.Tags != null);
    }
}