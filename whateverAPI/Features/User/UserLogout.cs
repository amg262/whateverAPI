using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.User;

public class UserLogout : EndpointWithoutRequest<UserLogout.LogoutResponse>
{
    private readonly JwtTokenService _jwtTokenService;

    public UserLogout(JwtTokenService jwtTokenService)
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
            s.Response<LogoutResponse>(200, "User logged out successfully");
        });
    }

    public record LogoutResponse
    {
        public string Message { get; set; }
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
        await SendAsync(new LogoutResponse { Message = "User logged out successfully" }, cancellation: ct);
    }
}