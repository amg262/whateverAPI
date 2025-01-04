using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.User.Login;

public class Endpoint : Endpoint<Request>
{
    // private readonly JwtOptions _jwtOptions;
    private readonly JwtTokenService _jwtTokenService;

    public Endpoint(JwtTokenService jwtTokenService)
    {
        // _jwtOptions = jwtOptions.Value;
        _jwtTokenService = jwtTokenService;
    }

    public override void Configure()
    {
        Post("/user/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // if (await myAuthService.CredentialsAreValid(req.Username, req.Password, ct))
        // {
        // var jwtToken = JwtBearer.CreateToken(
        //     o =>
        //     {
        //         o.SigningKey = _jwtOptions.Secret;
        //         o.ExpireAt = DateTime.UtcNow.AddDays(_jwtOptions?.ExpirationInDays ?? 90);
        //         o.User.Roles.Add("Manager", "Auditor");
        //         o.User.Claims.Add(("UserName", req.Username));
        //         o.User["UserId"] = "001";
        //     });
        //
        // // Set the auth cookie
        // HttpContext.Response.Cookies.Append(
        //     "AuthCookieName",
        //     jwtToken,
        //     new CookieOptions
        //     {
        //         HttpOnly = true,
        //         Secure = true,
        //         SameSite = SameSiteMode.Strict,
        //         Expires = DateTime.UtcNow.AddDays(_jwtOptions?.ExpirationInDays ?? 90)
        //     });

        var jwtToken = _jwtTokenService.GenerateToken(req.Name, req.Username);
        // var tokenIssued = _jwtTokenService.IssueToken(jwtToken);

        await SendAsync(
            new
            {
                req.Username,
                Token = jwtToken
            }, cancellation: ct);
        // }
        // else
        //     ThrowError("The supplied credentials are invalid!");
    }
}