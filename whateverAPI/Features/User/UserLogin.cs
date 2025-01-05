using FastEndpoints;
using FluentValidation;
using whateverAPI.Services;

namespace whateverAPI.Features.User;

public class UserLogin : Endpoint<UserLogin.Request>
{
    // private readonly JwtOptions _jwtOptions;
    private readonly JwtTokenService _jwtTokenService;

    public UserLogin(JwtTokenService jwtTokenService)
    {
        // _jwtOptions = jwtOptions.Value;
        _jwtTokenService = jwtTokenService;
    }

    public override void Configure()
    {
        Post("/user/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Login";
            s.Description = "Login the user";
            s.Response(200, "User logged in successfully");
        });
    }

    public record Request
    {
        public string? Name { get; init; }
        public string? Username { get; init; }
    }

    public class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MinimumLength(2).WithMessage("Name must be at least 5 characters");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(2).WithMessage("Username must be at least 5 characters");
        }
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var jwtToken = _jwtTokenService.GenerateToken(req.Name, req.Username);
        await SendAsync(new { req.Username, Token = jwtToken }, cancellation: ct);
    }
}