using System.Net.Http.Headers;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using whateverAPI.Data;
using whateverAPI.Helpers;
using whateverAPI.Options;
using whateverAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


builder.Services
    .AddOpenApi()
    .AddHttpContextAccessor()
    .AddValidatorsFromAssemblyContaining<Program>()
    .AddExceptionHandler<GlobalException>()
    .AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString(Helper.DefaultConnection), o =>
                o.EnableRetryOnFailure())
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging())
    .AddCors(options =>
    {
        var corsOptions = builder.Configuration.GetSection(nameof(CorsOptions)).Get<CorsOptions>();
        options.AddPolicy(Helper.DefaultPolicy, policyBuilder =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }
            else
            {
                policyBuilder
                    .WithOrigins(corsOptions?.AllowedOrigins ?? ["https://whateverbruh.azurewebsites.net"])
                    .WithMethods(corsOptions?.AllowedMethods ?? ["GET", "POST", "PUT", "DELETE", "OPTIONS"])
                    .WithHeaders(corsOptions?.AllowedHeaders ?? ["*"])
                    .AllowCredentials();
            }
        });
    }) // Wh
    .AddApplicationInsightsTelemetry()
    .AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsConfig>();
            configuration.EnrichWithContext(context.ProblemDetails, context.HttpContext);
        };
    })
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtOptions:Issuer"],
            ValidAudience = builder.Configuration["JwtOptions:Audience"],
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtOptions:Secret"] ?? string.Empty)),
            ValidateActor = true,
            RequireSignedTokens = true,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var jwtTokenService = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>();
                jwtTokenService.GetToken(context);
                return Task.CompletedTask;
            }
        };
    }).Services.AddAuthorizationBuilder()
    .AddPolicy(Helper.RequireAdmin, policy =>
        policy.RequireAssertion(context => context.User.IsInRole(Helper.AdminRole)))
    .AddPolicy(Helper.RequireModeratorOrAbove, policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(Helper.AdminRole) ||
            context.User.IsInRole(Helper.ModeratorRole)))
    .AddPolicy(Helper.RequireAuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());


builder.Services.AddOptions<JwtOptions>().BindConfiguration(nameof(JwtOptions));
builder.Services.AddOptions<GoogleOptions>().BindConfiguration(nameof(GoogleOptions));
builder.Services.AddOptions<MicrosoftOptions>().BindConfiguration(nameof(MicrosoftOptions));
builder.Services.AddOptions<FacebookOptions>().BindConfiguration(nameof(FacebookOptions));
builder.Services.AddOptions<JokeApiOptions>().BindConfiguration(nameof(JokeApiOptions));
builder.Services.AddOptions<CorsOptions>().BindConfiguration(nameof(CorsOptions));


builder.Services
    .AddSingleton<ProblemDetailsConfig>()
    .AddScoped(typeof(ValidationFilter<>))
    .AddScoped<IJwtTokenService, JwtTokenService>()
    .AddScoped<IGoogleAuthService, GoogleAuthService>()
    .AddScoped<IMicrosoftAuthService, MicrosoftAuthService>()
    .AddScoped<IFacebookAuthService, FacebookAuthService>()
    .AddScoped<UserService>()
    .AddScoped<JokeService>()
    .AddScoped<TagService>()
    .AddScoped<JokeApiService>()
    .AddScoped<RoleService>();


builder.Services.AddHttpClient<IGoogleAuthService, GoogleAuthService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<IMicrosoftAuthService, MicrosoftAuthService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<IFacebookAuthService, FacebookAuthService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<JokeApiService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.BaseAddress = new Uri(builder.Configuration["JokeApiOptions:BaseUrl"] ?? string.Empty);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddStandardResilienceHandler();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.MapOpenApi();
app.MapScalarApiReference(opts =>
{
    opts
        .WithTheme(ScalarTheme.Saturn)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithFavicon("/favicon.ico")
        .WithOperationSorter(OperationSorter.Method)
        .WithTagSorter(TagSorter.Alpha)
        .WithLayout(ScalarLayout.Modern)
        .WithTitle("Whatever bruh API")
        .WithDefaultFonts(true)
        .WithPreferredScheme("Bearer")
        .WithHttpBasicAuthentication(basic =>
        {
            basic.Username = "admin@admin.com";
            basic.Password = "admin@admin.com";
        })
        .WithHttpBearerAuthentication(bearer => bearer.Token = Helper.AuthToken)
        .WithOAuth2Authentication(oauth =>
        {
            oauth.Scopes = ["openid", "email", "profile"];
            oauth.ClientId = builder.Configuration["GoogleOptions:ClientId"];
        });
});


app
    .UseExceptionHandler()
    .UseHttpsRedirection()
    .UseStaticFiles()
    .UseAuthentication()
    .UseAuthorization()
    .UseCors(Helper.DefaultPolicy);

await app.InitializeDatabaseRetryAsync();

app.MapEndpointsFromAssembly();

app.Run();