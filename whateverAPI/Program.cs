using System.Net.Http.Headers;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using whateverAPI.Data;
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
            o => o.EnableRetryOnFailure())
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging());

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<JokeApiService>();
builder.Services.AddScoped<IJokeService, JokeService>();

builder.Services.AddScoped(typeof(ValidationFilter<>));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHttpClient<JokeApiService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.BaseAddress = new Uri(builder.Configuration["JokeApiOptions:BaseUrl"] ?? string.Empty);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddStandardResilienceHandler();

builder.Services
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
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtOptions:Secret"] ?? string.Empty))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var jwtTokenService = context.HttpContext.RequestServices.GetRequiredService<JwtTokenService>();
                jwtTokenService.GetToken(context);
                return Task.CompletedTask;
            }
        };
    }).Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Theme = ScalarTheme.Saturn;
        opts.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecurityScheme = "ApiKey",
            ApiKey = new ApiKeyOptions { Token = ProjectHelper.AuthToken }
        };

        opts.WithHttpBearerAuthentication(bearer => { bearer.Token = ProjectHelper.AuthToken; });
        opts.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opts.Favicon = "/favicon.ico";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication().UseAuthorization();
app.MapControllers();

// Endpoints
var apiGroup = app.MapGroup("/api");
var jokeGroup = apiGroup.MapGroup("/jokes").WithTags("Jokes");
var userGroup = apiGroup.MapGroup("/user").WithTags("User");


jokeGroup.MapGet("/", async Task<IResult> (IJokeService jokeService, CancellationToken ct) =>
    {
        var jokes = await jokeService.GetJokes(ct);
        return jokes is not null && jokes.Count != 0
            ? TypedResults.Ok(EntityMapper.JokesToJokeReponses(jokes))
            : TypedResults.NotFound();
    })
    .WithName("GetJokes")
    .WithDescription("Retrieves all jokes from the database with pagination")
    .WithSummary("Get all jokes")
    .WithOpenApi()
    .RequireAuthorization();

jokeGroup.MapGet("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, IJokeService jokeService, CancellationToken ct) =>
    {
        var joke = await jokeService.GetJokeById(id, ct);
        return joke is not null ? TypedResults.Ok(EntityMapper.JokeToJokeResponse(joke)) : TypedResults.NotFound();
    })
    .WithName("GetJokeById")
    .WithDescription("Retrieves a specific joke by its unique identifier")
    .WithSummary("Get a joke by ID")
    .WithOpenApi();

jokeGroup.MapPost("/", async Task<IResult> (CreateJokeRequest request, IJokeService jokeService, CancellationToken ct) =>
    {
        var joke = EntityMapper.CreateRequestToJoke(request);
        var created = await jokeService.CreateJoke(joke, ct);
        var response = EntityMapper.JokeToJokeResponse(created);
        return response is not null ? TypedResults.Created($"/api/jokes/{created.Id}", response) : TypedResults.BadRequest();
    })
    .WithName("CreateJoke")
    .WithDescription("Creates a new joke with the provided content and metadata")
    .WithSummary("Create a new joke")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>();

jokeGroup.MapGet("/type", async Task<IResult> ([AsParameters] FilterRequest request, IJokeService jokeService,
        CancellationToken ct) =>
    {
        var jokes = await jokeService.GetJokesByType(request, ct);
        return jokes.Count != 0 ? TypedResults.Ok(EntityMapper.JokesToJokeReponses(jokes)) : TypedResults.NotFound();
    })
    .WithName("GetJokesByType")
    .WithDescription("Retrieves jokes filtered by type with optional sorting and pagination")
    .WithSummary("Get jokes by type")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<FilterRequest>>();

jokeGroup.MapPut("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, UpdateJokeRequest request, IJokeService jokeService,
        CancellationToken ct) =>
    {
        var joke = EntityMapper.UpdateRequestToJoke(id, request);
        var updated = await jokeService.UpdateJoke(joke, ct);
        return updated is not null ? TypedResults.Ok(EntityMapper.JokeToJokeResponse(updated)) : TypedResults.NotFound();
    })
    .WithName("UpdateJoke")
    .WithDescription("Updates an existing joke's content and metadata")
    .WithSummary("Update a joke")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<UpdateJokeRequest>>();

jokeGroup.MapDelete("/{id:guid}", async Task<IResult> ([AsParameters] DeleteJokeRequest request, IJokeService jokeService,
        CancellationToken ct) =>
    {
        var result = await jokeService.DeleteJoke(request.Id, ct);
        return result ? TypedResults.NoContent() : TypedResults.NotFound();
    })
    .WithName("DeleteJoke")
    .WithDescription("Permanently removes a joke from the database")
    .WithSummary("Delete a joke")
    .WithOpenApi();

jokeGroup.MapGet("/random", async Task<IResult> (IJokeService jokeService, CancellationToken ct) =>
    {
        var joke = await jokeService.GetRandomJoke(ct);
        return joke is not null ? TypedResults.Ok(EntityMapper.JokeToJokeResponse(joke)) : TypedResults.NotFound();
    })
    .WithName("GetRandomJoke")
    .WithDescription("Retrieves a random joke from the available collection")
    .WithSummary("Get a random joke")
    .WithOpenApi();

jokeGroup.MapGet("/search", async Task<IResult> (string query, IJokeService jokeService, CancellationToken ct) =>
    {
        var jokes = await jokeService.SearchJokes(query, ct);
        return jokes?.Count > 0 ? TypedResults.Ok(EntityMapper.JokesToJokeReponses(jokes)) : TypedResults.NotFound();
    })
    .WithName("SearchJokes")
    .WithDescription("Searches for jokes containing the specified query in their content or tags")
    .WithSummary("Search for jokes")
    .WithOpenApi();

jokeGroup.MapGet("/whatever", async Task<IResult> (JokeApiService jokeApiService, CancellationToken ct) =>
    {
        var joke = await jokeApiService.GetExternalJoke(ct);
        return joke is not null ? TypedResults.Ok(joke) : TypedResults.NotFound();
    })
    .WithName("GetWhateverJoke")
    .WithDescription("Retrieves a random joke from a third-party API")
    .WithSummary("Get a joke from a third-party API")
    .WithOpenApi();

userGroup.MapPost("/login", async Task<IResult> ([FromBody] UserLoginRequest request, JwtTokenService jwtTokenService) =>
    {
        var jwtToken = jwtTokenService.GenerateToken(request.Username, request.Email);
        return !string.IsNullOrEmpty(jwtToken)
            ? TypedResults.Ok(new { request.Username, Token = jwtToken })
            : TypedResults.Unauthorized();
    })
    .WithName("UserLogin")
    .WithDescription("Authenticates a user and returns a JWT token for subsequent requests")
    .WithSummary("Login user")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<UserLoginRequest>>();

userGroup.MapPost("/logout", async Task<IResult> ([FromServices] JwtTokenService jwtTokenService) =>
    {
        var token = jwtTokenService.GetToken();

        if (string.IsNullOrEmpty(token)) return TypedResults.Unauthorized();
        jwtTokenService.InvalidateToken(token);
        return TypedResults.Ok();
    })
    .WithName("UserLogout")
    .WithDescription("Invalidates the current user's JWT token")
    .WithSummary("Logout user")
    .WithOpenApi();


await DbInitializer.InitDb(app);
app.Run();