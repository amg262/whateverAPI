using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using whateverAPI;
using whateverAPI.Data;
using whateverAPI.Endpoints;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Options;
using whateverAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


builder.Services
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
            var policy = policyBuilder
                .WithOrigins(corsOptions?.AllowedOrigins ?? ["*"])
                .WithMethods(corsOptions?.AllowedMethods ?? ["GET", "POST", "PUT", "DELETE", "OPTIONS"])
                .WithHeaders(corsOptions?.AllowedHeaders ?? ["*"]);
            if (corsOptions?.AllowCredentials ?? true)
                policy.AllowCredentials();
            else
                policy.AllowAnyOrigin();
        });
    })
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
    .AddPolicy("RequireAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("admin")))
    .AddPolicy("RequireModeratorOrAbove", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("admin") ||
            context.User.IsInRole("moderator")))
    .AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());


builder.Services
    .AddOpenApi()
    .AddHttpContextAccessor()
    .AddValidatorsFromAssemblyContaining<Program>()
    .AddExceptionHandler<GlobalException>()
    .AddControllers();


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

if (app.Environment.IsDevelopment() || !app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Theme = ScalarTheme.Saturn;
        opts.WithHttpBearerAuthentication(bearer => { bearer.Token = Helper.AuthToken; });
        opts.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opts.Favicon = "/favicon.ico";
        opts.OperationSorter = OperationSorter.Method;
        opts.TagSorter = TagSorter.Alpha;
        opts.Layout = ScalarLayout.Modern;
        opts.DefaultFonts = true;
        opts.ShowSidebar = true;
        opts.Title = "Whatever bruh API";
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication().UseAuthorization();
app.MapControllers();

await app.InitializeDatabaseRetryAsync();

JokeEndpoints.MapEndpoints(app);
UserEndpoints.MapEndpoints(app);
TagEndpoints.MapEndpoints(app);
RoleEndpoints.MapEndpoints(app);
AuthEndpoints.MapEndpoints(app);

app.Run();