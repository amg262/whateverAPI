using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.User.Logout;

public class Response
{
    public string Message { get; set; }
}

public class Endpoint : EndpointWithoutRequest<Response>
{
    private readonly JwtTokenService _jwtTokenService;

    public Endpoint(JwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public override void Configure()
    {
        Post("/user/logout");
        // AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Logout";
            s.Description = "Logout the user";
            s.Response<Response>(200, "User logged out successfully");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var token = _jwtTokenService.GetToken();

        if (token is null)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        _jwtTokenService.InvalidateToken(token);
        await SendAsync(new Response { Message = "User logged out successfully" }, cancellation: ct);
    }
}