using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Options;
using whateverAPI.Services;

// try to redeploy
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

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
        context.ProblemDetails.Extensions.TryAdd("timestamp", DateTimeOffset.UtcNow);
        var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});

builder.Services.AddApplicationInsightsTelemetry();

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
    }).Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddExceptionHandler<GlobalException>();

builder.Services.AddOptions<JwtOptions>().BindConfiguration(nameof(JwtOptions));
builder.Services.AddOptions<GoogleOptions>().BindConfiguration(nameof(GoogleOptions));
builder.Services.AddOptions<MicrosoftOptions>().BindConfiguration(nameof(MicrosoftOptions));
builder.Services.AddOptions<FacebookOptions>().BindConfiguration(nameof(FacebookOptions));

builder.Services.AddScoped(typeof(ValidationFilter<>));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IMicrosoftAuthService, MicrosoftAuthService>();
builder.Services.AddScoped<IFacebookAuthService, FacebookAuthService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JokeService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<JokeApiService>();

builder.Services.AddHttpClient<IGoogleAuthService, GoogleAuthService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<IMicrosoftAuthService, MicrosoftAuthService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<IFacebookAuthService, FacebookAuthService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<JokeApiService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.BaseAddress = new Uri(builder.Configuration["JokeApiOptions:BaseUrl"] ?? string.Empty);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddStandardResilienceHandler();


// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(Helper.CorsPolicy, policyBuilder =>
//     {
//         policyBuilder
//             .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
//             .WithMethods(builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? [])
//             .WithHeaders(builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? [])
//             .AllowCredentials();
//     });
// });


var app = builder.Build();

// Configure the HTTP request pipeline.
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

// app.UseExceptionHandler();
// app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication().UseAuthorization();
app.MapControllers();

await app.InitializeDatabaseRetryAsync();
// Endpoints
var apiGroup = app.MapGroup("/api");
var jokeGroup = apiGroup.MapGroup("/jokes").WithTags("Jokes");
var userGroup = apiGroup.MapGroup("/user").WithTags("User");
var tagGroup = apiGroup.MapGroup("/tags").WithTags("Tags");
var googleAuthGroup = app.MapGroup("/api/auth/google").WithTags("Authentication");
var microsoftAuthGroup = app.MapGroup("/api/auth/microsoft").WithTags("Authentication");
var facebookAuthGroup = app.MapGroup("/api/auth/facebook").WithTags("Authentication");

// Get All Jokes
jokeGroup.MapGet("/", async Task<IResult> (
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct = default) =>
    {
        var jokes = await jokeService.GetJokesAsync(ct);
        return jokes is not null && jokes.Count != 0
            // ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
            ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
            : context.CreateNotFoundProblem(nameof(Joke), "all");
    })
    .WithName("GetJokesAsync")
    .WithDescription("Retrieves all jokes from the database with pagination")
    .WithSummary("Get all jokes")
    .WithOpenApi()
    .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status401Unauthorized);
// .RequireAuthorization();

// Get Joke by ID
jokeGroup.MapGet("/{id:guid}", async Task<IResult> (
        [FromRoute] Guid id,
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var joke = await jokeService.GetJokeById(id, ct);
        return joke is not null
            ? TypedResults.Ok(Joke.ToResponse(joke))
            // ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
            : context.CreateNotFoundProblem(nameof(Joke), id.ToString());
    })
    .WithName("GetJokeById")
    .WithDescription("Retrieves a specific joke by its unique identifier")
    .WithSummary("Get a joke by ID")
    .WithOpenApi()
    .Produces<JokeResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest);

// Create New Joke
jokeGroup.MapPost("/", async Task<IResult> (
        CreateJokeRequest request,
        JokeService jokeService,
        HttpContext context,
        UserService userService,
        CancellationToken ct) =>
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        User? user = null;

        if (userId != null && Guid.TryParse(userId, out var userGuid))
        {
            user = await userService.GetUserById(userGuid, ct);
        }

        var joke = Joke.FromCreateRequest(request, user);
        // var joke = Mapper.CreateRequestToJoke(request);
        var created = await jokeService.CreateJoke(joke, ct);
        var response = Joke.ToResponse(created);
        // var response = JokeResponse.FromJoke(created);
        // var response = Mapper.JokeToJokeResponse(created);
        return response is not null
            ? TypedResults.Created($"/api/jokes/{created.Id}", response)
            : context.CreateUnprocessableEntityProblem("Create Joke");
    })
    .WithName("CreateJoke")
    .WithDescription("Creates a new joke with the provided content and metadata")
    .WithSummary("Create a new joke")
    .WithOpenApi()
    .Accepts<CreateJokeRequest>("application/json")
    .Produces<JokeResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>()
    .RequireAuthorization();

// Update Joke
jokeGroup.MapPut("/{id:guid}", async Task<IResult> (
        [FromRoute] Guid id,
        UpdateJokeRequest request,
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var joke = Joke.FromUpdateRequest(id, request);
        // var joke = Mapper.UpdateRequestToJoke(id, request);
        var updated = await jokeService.UpdateJoke(joke, ct);
        return updated is not null
            ? TypedResults.Ok(Joke.ToResponse(updated))
            // ? TypedResults.Ok(Mapper.JokeToJokeResponse(updated))
            : context.CreateNotFoundProblem(nameof(Joke), id.ToString());
    })
    .WithName("UpdateJoke")
    .WithDescription("Updates an existing joke's content and metadata")
    .WithSummary("Update a joke")
    .WithOpenApi()
    .Accepts<UpdateJokeRequest>("application/json")
    .Produces<JokeResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
    .AddEndpointFilter<ValidationFilter<UpdateJokeRequest>>();

// Delete Joke
jokeGroup.MapDelete("/{id:guid}", async Task<IResult> (
        [FromRoute] Guid id,
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var result = await jokeService.DeleteJoke(id, ct);
        return result
            ? TypedResults.NoContent()
            : context.CreateNotFoundProblem(nameof(Joke), id.ToString());
    })
    .WithName("DeleteJoke")
    .WithDescription("Permanently removes a joke from the database")
    .WithSummary("Delete a joke")
    .WithOpenApi()
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest);

// Get Random Joke
jokeGroup.MapGet("/random", async Task<IResult> (
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var joke = await jokeService.GetRandomJoke(ct);
        return joke is not null
            ? TypedResults.Ok(Joke.ToResponse(joke))
            // ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
            : context.CreateNotFoundProblem(nameof(Joke), "random");
    })
    .WithName("GetRandomJoke")
    .WithDescription("Retrieves a random joke from the available collection")
    .WithSummary("Get a random joke")
    .WithOpenApi()
    .Produces<JokeResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

jokeGroup.MapGet("/klump", async Task<IResult> (
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var joke = await jokeService.GetRandomJoke(ct);
        return joke is not null
            ? TypedResults.Ok(joke.Content.Replace("\n", " "))
            // ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
            : context.CreateNotFoundProblem(nameof(Joke), "klump");
    })
    .WithName("Klump")
    .WithDescription("Klump")
    .WithSummary("Klump")
    .WithOpenApi()
    .Produces<string>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

// Search Jokes
jokeGroup.MapGet("/search", async Task<IResult> (
        [FromQuery(Name = "q")] string query,
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return context.CreateBadRequestProblem("Search query cannot be empty");
        }

        var jokes = await jokeService.SearchJokes(query, ct);
        return jokes?.Count > 0
            ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
            // ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
            : context.CreateNotFoundProblem(nameof(Joke), $"matching query '{query}'");
    })
    .WithName("SearchJokes")
    .WithDescription("Searches for jokes containing the specified query in their content or tags")
    .WithSummary("Search for jokes")
    .WithOpenApi()
    .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest);


jokeGroup.MapPost("/find", async Task<IResult> (
        [AsParameters] FilterRequest request,
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var jokes = await jokeService.SearchAndFilter(request, ct);
        return jokes.Count != 0
            ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
            : context.CreateNotFoundProblem(nameof(Joke),
                $"matching criteria (Type={request.Type}, Query={request.Query ?? "none"})");
    })
    .WithName("SearchAndFilterJokes")
    .WithDescription("Searches and filters jokes with optional text search, type filtering, sorting, and pagination")
    .WithSummary("Search and filter jokes")
    .WithOpenApi()
    .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .AddEndpointFilter<ValidationFilter<FilterRequest>>();

// Get External Joke
jokeGroup.MapGet("/whatever", async Task<IResult> (
        JokeApiService jokeApiService,
        HttpContext context,
        CancellationToken ct) =>
    {
        try
        {
            var joke = await jokeApiService.GetExternalJoke(ct);
            return joke is not null
                ? TypedResults.Ok(joke)
                : context.CreateNotFoundProblem("External Joke", "random");
        }
        catch (HttpRequestException ex)
        {
            return context.CreateExternalServiceProblem(
                "Joke API",
                "Failed to fetch joke from external service",
                ex);
        }
    })
    .WithName("GetWhateverJoke")
    .WithDescription("Retrieves a random joke from a third-party API")
    .WithSummary("Get a joke from a third-party API")
    .WithOpenApi()
    .Produces<Joke>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status502BadGateway);

// User Login
userGroup.MapPost("/login", async Task<IResult> (
        [FromBody] UserLoginRequest request,
        IJwtTokenService jwtTokenService,
        HttpContext context) =>
    {
        var jwtToken = jwtTokenService.GenerateToken(request.Username, request.Email, Guid.CreateVersion7().ToString(), "local");
        return !string.IsNullOrEmpty(jwtToken)
            ? TypedResults.Ok(new { request.Username, Token = jwtToken })
            : context.CreateUnauthorizedProblem("Invalid credentials provided");
    })
    .WithName("UserLogin")
    .WithDescription("Authenticates a user and returns a JWT token for subsequent requests")
    .WithSummary("Login user")
    .WithOpenApi()
    .Accepts<UserLoginRequest>("application/json")
    .Produces<object>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .AddEndpointFilter<ValidationFilter<UserLoginRequest>>();

// User Logout
userGroup.MapPost("/logout", async Task<IResult> (
        [FromServices] IJwtTokenService jwtTokenService,
        HttpContext context) =>
    {
        var token = jwtTokenService.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            return context.CreateUnauthorizedProblem("No valid authentication token found");
        }

        jwtTokenService.InvalidateToken(token);
        return TypedResults.Ok();
    })
    .WithName("UserLogout")
    .WithDescription("Invalidates the current user's JWT token")
    .WithSummary("Logout user")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized);

// Get all tags
tagGroup.MapGet("/", async Task<IResult> (
        TagService tagService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var tags = await tagService.GetAllTagsAsync(ct);
        return tags.Count != 0
            ? TypedResults.Ok(Tag.ToTagResponses(tags))
            : context.CreateNotFoundProblem(nameof(Tag), "all");
    })
    .WithName("GetTags")
    .WithDescription("Retrieves all tags")
    .WithSummary("Get all tags")
    .WithOpenApi()
    .Produces<List<TagResponse>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .RequireAuthorization();

// Get tag by ID
tagGroup.MapGet("/{id:guid}", async Task<IResult> (
        [FromRoute] Guid id,
        TagService tagService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var tag = await tagService.GetTagByIdAsync(id, ct);
        return tag is not null
            ? TypedResults.Ok(Tag.ToResponse(tag))
            : context.CreateNotFoundProblem(nameof(Tag), id.ToString());
    })
    .WithName("GetTagById")
    .WithDescription("Retrieves a specific tag by its unique identifier")
    .WithSummary("Get a tag by ID")
    .WithOpenApi()
    .Produces<TagResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

// Create new tag
tagGroup.MapPost("/", async Task<IResult> (
        CreateTagRequest request,
        TagService tagService,
        HttpContext context,
        CancellationToken ct) =>
    {
        try
        {
            var tag = await tagService.CreateTagAsync(request, ct);

            return TypedResults.Created($"/api/tags/{tag.Id}", Tag.ToResponse(tag) as object);
        }
        catch (InvalidOperationException ex)
        {
            return context.CreateBadRequestProblem(ex.Message);
        }
    })
    .WithName("CreateTag")
    .WithDescription("Creates a new tag")
    .WithSummary("Create a tag")
    .WithOpenApi()
    .Accepts<CreateTagRequest>("application/json")
    .Produces<TagResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .AddEndpointFilter<ValidationFilter<CreateTagRequest>>();

// Update tag
tagGroup.MapPut("/{id:guid}", async Task<IResult> (
        [FromRoute] Guid id,
        UpdateTagRequest request,
        TagService tagService,
        HttpContext context,
        CancellationToken ct) =>
    {
        try
        {
            var tag = await tagService.UpdateTagAsync(id, request, ct);
            return tag is not null
                ? TypedResults.Ok(Tag.ToResponse(tag))
                : context.CreateNotFoundProblem(nameof(Tag), id.ToString());
        }
        catch (InvalidOperationException ex)
        {
            return context.CreateBadRequestProblem(ex.Message);
        }
    })
    .WithName("UpdateTag")
    .WithDescription("Updates an existing tag")
    .WithSummary("Update a tag")
    .WithOpenApi()
    .Accepts<UpdateTagRequest>("application/json")
    .Produces<TagResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .AddEndpointFilter<ValidationFilter<UpdateTagRequest>>();

// Delete tag
tagGroup.MapDelete("/{id:guid}", async Task<IResult> (
        [FromRoute] Guid id,
        TagService tagService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var result = await tagService.DeleteTagAsync(id, ct);
        return result
            ? TypedResults.NoContent()
            : context.CreateNotFoundProblem(nameof(Tag), id.ToString());
    })
    .WithName("DeleteTag")
    .WithDescription("Deletes a tag")
    .WithSummary("Delete a tag")
    .WithOpenApi()
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound);


// Endpoint to start the OAuth flow
googleAuthGroup.MapGet("/login", async Task<IResult> (
        IGoogleAuthService authService,
        HttpResponse response,
        HttpContext context) =>
    {
        // Generate the Google OAuth URL and redirect the user to it
        var authUrl = authService.GenerateOAuthUrl();
        return !string.IsNullOrEmpty(authUrl)
            ? TypedResults.Ok(authUrl)
            : context.CreateNotFoundProblem("Google OAuth URL", string.Empty);
    })
    .WithName("GoogleLogin")
    .WithDescription("Initiates the Google OAuth2 authentication flow by generating an authorization URL")
    .WithSummary("Start Google authentication")
    .WithOpenApi()
    .Produces<string>(StatusCodes.Status200OK, "application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

// Endpoint to handle the OAuth callback
googleAuthGroup.MapGet("/callback", async Task<IResult> (
        HttpRequest request,
        IGoogleAuthService authService,
        IJwtTokenService jwtService,
        UserService userService,
        HttpContext context,
        CancellationToken ct) =>
    {
        // Get the authorization code from the query string
        var code = request.Query["code"].ToString();

        if (string.IsNullOrEmpty(code))
        {
            return context.CreateNotFoundProblem("Authorization code", string.Empty);
            return TypedResults.BadRequest("No authorization code provided");
        }

        try
        {
            // Exchange the code for user information
            var googleUser = await authService.HandleCallbackAsync<GoogleUserInfo>(code);

            var newUser = OAuthUserInfo.FromUserInfoAsync(googleUser);

            if (newUser == null)
            {
                return context.CreateBadRequestProblem("User account be created");
            }

            var user = await userService.GetOrCreateUserFromOAuthAsync(newUser, ct);

            // Generate your application's JWT
            var token = jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Name, Helper.GoogleProvider);

            // Return the user information and token
            return TypedResults.Ok(new
            {
                token,
                user
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    })
    .WithName("GoogleCallback")
    .WithDescription(
        "Handles the OAuth2 callback from Google, exchanging the authorization code for user information and generating a JWT token")
    .WithSummary("Complete google authentication")
    .WithOpenApi()
    .Produces<GoogleUserInfo>(StatusCodes.Status200OK, "application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status500InternalServerError);


// Endpoint to start the Microsoft OAuth flow
microsoftAuthGroup.MapGet("/login", async Task<IResult> (
        IMicrosoftAuthService authService,
        HttpResponse response,
        HttpContext context) =>
    {
        // Generate the Microsoft OAuth URL for the initial authentication request
        var authUrl = authService.GenerateOAuthUrl();
        return !string.IsNullOrEmpty(authUrl)
            ? TypedResults.Ok(authUrl)
            : context.CreateNotFoundProblem("Microsoft OAuth URL", string.Empty);
    })
    .WithName("MicrosoftLogin")
    .WithDescription("Initiates the Microsoft OAuth2 authentication flow by generating an authorization URL")
    .WithSummary("Start Microsoft authentication")
    .WithOpenApi()
    .Produces<string>(StatusCodes.Status200OK, "application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

// Endpoint to handle the Microsoft OAuth callback
microsoftAuthGroup.MapGet("/callback", async Task<IResult> (
        HttpRequest request,
        IMicrosoftAuthService authService,
        IJwtTokenService jwtService,
        UserService userService,
        HttpContext context) =>
    {
        var code = request.Query["code"].ToString();

        if (string.IsNullOrEmpty(code))
        {
            return context.CreateNotFoundProblem("Authorization code", string.Empty);
            return TypedResults.BadRequest("No authorization code provided");
        }

        try
        {
            var microsoftUser = await authService.HandleCallbackAsync<MicrosoftUserInfo>(code);

            var authUser = OAuthUserInfo.FromUserInfoAsync(microsoftUser);

            // var newUser = OAuthUserInfo.FromMicrosoftUserInfo(microsoftUser);

            if (authUser == null)
            {
                return context.CreateBadRequestProblem("User account cannot be created");
            }

            var user = await userService.GetOrCreateUserFromOAuthAsync(authUser);

            var token = jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Name, Helper.GoogleProvider);


            return TypedResults.Ok(new
            {
                token,
                user
            });
        }
        catch (Exception ex)
        {
            return context.CreateBadRequestProblem(ex.Message);
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    })
    .WithName("MicrosoftCallback")
    .WithDescription(
        "Handles the OAuth2 callback from Microsoft, exchanging the authorization code for user information and generating a JWT token")
    .WithSummary("Complete Microsoft authentication")
    .WithOpenApi()
    .Produces<MicrosoftUserInfo>(StatusCodes.Status200OK, "application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status500InternalServerError);


facebookAuthGroup.MapGet("/login", async Task<IResult> (
        IFacebookAuthService authService,
        HttpResponse response,
        HttpContext context) =>
    {
        // Generate the Facebook OAuth URL that the user will use to authenticate
        var authUrl = authService.GenerateOAuthUrl();

        return !string.IsNullOrEmpty(authUrl)
            ? TypedResults.Ok(authUrl)
            : context.CreateNotFoundProblem("Facebook OAuth URL", string.Empty);
    })
    .WithName("FacebookLogin")
    .WithDescription("Initiates the Facebook OAuth2 authentication flow by generating an authorization URL")
    .WithSummary("Start Facebook authentication")
    .WithOpenApi()
    .Produces<string>(StatusCodes.Status200OK, "application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

// Callback endpoint - Handles the OAuth response
facebookAuthGroup.MapGet("/callback", async Task<IResult> (
        HttpRequest request,
        IFacebookAuthService authService,
        IJwtTokenService jwtService,
        UserService userService,
        HttpContext context,
        CancellationToken ct) =>
    {
        // Extract the authorization code from the callback
        var code = request.Query["code"].ToString();

        if (string.IsNullOrEmpty(code))
        {
            return context.CreateNotFoundProblem("Authorization code", string.Empty);
        }

        try
        {
            // Exchange the authorization code for user information using the Facebook API
            var facebookUser = await authService.HandleCallbackAsync<FacebookUserInfo>(code);

            // Convert the Facebook-specific user info to our common OAuth format
            var authUser = OAuthUserInfo.FromUserInfoAsync(facebookUser);

            if (authUser == null)
            {
                return context.CreateBadRequestProblem("User account cannot be created");
            }

            // Create or update the user in our system
            var user = await userService.GetOrCreateUserFromOAuthAsync(authUser, ct);

            // Generate a JWT token for subsequent API calls
            var token = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Name,
                Helper.FacebookProvider);

            // Return both the user information and the JWT token
            return TypedResults.Ok(new
            {
                token,
                user
            });
        }
        catch (Exception ex)
        {
            return context.CreateExternalServiceProblem(
                "Facebook Authentication",
                "Failed to process Facebook authentication",
                ex);
        }
    })
    .WithName("FacebookCallback")
    .WithDescription(
        "Handles the OAuth2 callback from Facebook, exchanging the authorization code for user information and generating a JWT token")
    .WithSummary("Complete Facebook authentication")
    .WithOpenApi()
    .Produces<object>(StatusCodes.Status200OK, "application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.Run();