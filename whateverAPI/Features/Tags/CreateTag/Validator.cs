using FastEndpoints;
using FluentValidation;

namespace whateverAPI.Features.Tags.CreateTag;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}