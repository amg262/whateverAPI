using FastEndpoints;
using FluentValidation;

namespace whateverAPI.Features.Jokes.CreateJoke;

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