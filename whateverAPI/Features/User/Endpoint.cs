using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity.Data;

namespace whateverAPI.Features.User;

public class Endpoint : Endpoint<Request>
{
    public override void Configure()
    {
        Post("/user/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // if (await myAuthService.CredentialsAreValid(req.Username, req.Password, ct))
        // {
        var jwtToken = JwtBearer.CreateToken(
            o =>
            {
                o.SigningKey = "supersecretsupersecretsupersecretsupersecretsupersecretsupersecret";
                o.ExpireAt = DateTime.UtcNow.AddDays(1);
                o.User.Roles.Add("Manager", "Auditor");
                o.User.Claims.Add(("UserName", req.Username));
                o.User["UserId"] = "001"; //indexer based claim setting
            });

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