using System.Net.Http.Headers;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Services.AddOptions<JwtOptions>().BindConfiguration(nameof(JwtOptions)).ValidateOnStart();

builder.Services.AddOptions<GoogleOptions>().BindConfiguration(nameof(GoogleOptions));
//.ValidateDataAnnotations().ValidateOnStart();

// builder.Services.ConfigureOptions<JwtOptions>();
// builder.Services.ConfigureOptions<GoogleOptions>();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<JokeApiService>();
builder.Services.AddScoped<JokeService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<GoogleAuthService>();


builder.Services.AddScoped(typeof(ValidationFilter<>));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalException>();


// Add memory cache for state parameter


builder.Services.AddHttpClient<GoogleAuthService>();


builder.Services.AddHttpClient<JokeApiService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.BaseAddress = new Uri(builder.Configuration["JokeApiOptions:BaseUrl"] ?? string.Empty);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddStandardResilienceHandler();

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(ProjectHelper.CorsPolicy, policyBuilder =>
//     {
//         policyBuilder
//             .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
//             .WithMethods(builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? [])
//             .WithHeaders(builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? [])
//             .AllowCredentials();
//     });
// });

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
if (app.Environment.IsDevelopment() || !app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Theme = ScalarTheme.Saturn;
        opts.WithHttpBearerAuthentication(bearer => { bearer.Token = ProjectHelper.AuthToken; });
        opts.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opts.Favicon = "/favicon.ico";
        opts.OperationSorter = OperationSorter.Method;
        opts.TagSorter = TagSorter.Alpha;
        // opts.DefaultOpenAllTags = true;
        opts.Layout = ScalarLayout.Modern;
        opts.DefaultFonts = true;
        opts.ShowSidebar = true;
        opts.Title = "Whatever MAH API";
    });
}
// test again
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

// Get All Jokes
jokeGroup.MapGet("/", async Task<IResult> (
        JokeService jokeService,
        HttpContext context,
        CancellationToken ct) =>
    {
        var jokes = await jokeService.GetJokes(ct);
        return jokes is not null && jokes.Count != 0
            // ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
            ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
            : context.CreateNotFoundProblem("Jokes", "all");
    })
    .WithName("GetJokes")
    .WithDescription("Retrieves all jokes from the database with pagination")
    .WithSummary("Get all jokes")
    .WithOpenApi()
    .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

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
            : context.CreateNotFoundProblem("Joke", id.ToString());
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
        CancellationToken ct) =>
    {
        var joke = Joke.FromCreateRequest(request);
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
    .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>();

// Get Jokes by Type
// jokeGroup.MapGet("/type", async Task<IResult> (
//     [AsParameters] FilterRequest request, 
//     JokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var jokes = await jokeService.GetJokesByType(request, ct);
//     return jokes.Count != 0
//         ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
//             // ? TypedResults.Ok(JokeResponse.FromJokes(jokes))
//             // ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//         : context.CreateNotFoundProblem("Jokes", $"type {request.Type}");
// })
// .WithName("GetJokesByType")
// .WithDescription("Retrieves jokes filtered by type with optional sorting and pagination")
// .WithSummary("Get jokes by type")
// .WithOpenApi()
// .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest)
// .AddEndpointFilter<ValidationFilter<FilterRequest>>();

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
            : context.CreateNotFoundProblem("Joke", id.ToString());
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
            : context.CreateNotFoundProblem("Joke", id.ToString());
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
            : context.CreateNotFoundProblem("Jokes", "random");
    })
    .WithName("GetRandomJoke")
    .WithDescription("Retrieves a random joke from the available collection")
    .WithSummary("Get a random joke")
    .WithOpenApi()
    .Produces<JokeResponse>(StatusCodes.Status200OK)
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
            : context.CreateNotFoundProblem("Jokes", $"matching query '{query}'");
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
            : context.CreateNotFoundProblem("Jokes",
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
        JwtTokenService jwtTokenService,
        HttpContext context) =>
    {
        var jwtToken = jwtTokenService.GenerateToken(request.Username, request.Email);
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
        [FromServices] JwtTokenService jwtTokenService,
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
            : context.CreateNotFoundProblem("Tags", "all");
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
            ? TypedResults.Ok(tag.ToResponse())
            : context.CreateNotFoundProblem("Tag", id.ToString());
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
            return TypedResults.Created($"/api/tags/{tag.Id}", tag.ToResponse());
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
                ? TypedResults.Ok(tag.ToResponse())
                : context.CreateNotFoundProblem("Tag", id.ToString());
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
            : context.CreateNotFoundProblem("Tag", id.ToString());
    })
    .WithName("DeleteTag")
    .WithDescription("Deletes a tag")
    .WithSummary("Delete a tag")
    .WithOpenApi()
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound);


// Create an API group for authentication endpoints
var authGroup = app.MapGroup("/api/auth/google").WithTags("Authentication");

// Endpoint to start the OAuth flow
authGroup.MapGet("/login", async Task<IResult> (
        GoogleAuthService googleAuthService,
        HttpResponse response) =>
    {
        // Generate the Google OAuth URL and redirect the user to it
        var authUrl = googleAuthService.GenerateGoogleOAuthUrl();
        return TypedResults.Ok(authUrl);
    })
    .WithName("GoogleLogin")
    .WithOpenApi();

// Endpoint to handle the OAuth callback
authGroup.MapGet("/callback", async Task<IResult> (
        HttpRequest request,
        GoogleAuthService googleAuthService,
        JwtTokenService jwtService) =>
    {
        // Get the authorization code from the query string
        var code = request.Query["code"].ToString();

        if (string.IsNullOrEmpty(code))
        {
            return TypedResults.BadRequest("No authorization code provided");
        }

        try
        {
            // Exchange the code for user information
            var googleUser = await googleAuthService.HandleGoogleCallback(code);

            // Generate your application's JWT
            var token = jwtService.GenerateToken(googleUser.Name, googleUser.Email);

            // Return the user information and token
            return Results.Ok(new
            {
                token,
                user = new
                {
                    id = googleUser.Id,
                    email = googleUser.Email,
                    name = googleUser.Name,
                    picture = googleUser.Picture,
                    locale = googleUser.Locale,
                    familyName = googleUser.FamilyName,
                    givenName = googleUser.GivenName,
                }
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    })
    .WithName("GoogleCallback")
    .WithOpenApi();

app.Run();

// Add this to your appsettings.json
/*
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "RedirectUri": "https://your-frontend-url/auth/callback"
    }
  }
}
*/


// // Endpoints
// var apiGroup = app.MapGroup("/api");
// var jokeGroup = apiGroup.MapGroup("/jokes").WithTags("Jokes");
// var userGroup = apiGroup.MapGroup("/user").WithTags("User");
// // Get All Jokes
// jokeGroup.MapGet("/", async Task<IResult> (JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var jokes = await jokeService.GetJokes(ct);
//         return jokes is not null && jokes.Count != 0
//             ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Jokes", "all");
//     })
//     .WithName("GetJokes")
//     .WithDescription("Retrieves all jokes from the database with pagination")
//     .WithSummary("Get all jokes")
//     .WithOpenApi()
//     .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesProblem(StatusCodes.Status401Unauthorized)
//     .RequireAuthorization();
//
// // Get Joke by ID
// jokeGroup.MapGet("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var joke = await jokeService.GetJokeById(id, ct);
//         return joke is not null 
//             ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Joke", id.ToString());
//     })
//     .WithName("GetJokeById")
//     .WithDescription("Retrieves a specific joke by its unique identifier")
//     .WithSummary("Get a joke by ID")
//     .WithOpenApi()
//     .Produces<JokeResponse>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest);
//
// // Create New Joke
// jokeGroup.MapPost("/", async Task<IResult> (CreateJokeRequest request, JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var joke = Mapper.CreateRequestToJoke(request);
//         var created = await jokeService.CreateJoke(joke, ct);
//         var response = Mapper.JokeToJokeResponse(created);
//         return response is not null 
//             ? TypedResults.Created($"/api/jokes/{created.Id}", response)
//             : ProblemDetailsHelper.CreateUnprocessableEntityProblem(context, "Failed to create joke with the provided data");
//     })
//     .WithName("CreateJoke")
//     .WithDescription("Creates a new joke with the provided content and metadata")
//     .WithSummary("Create a new joke")
//     .WithOpenApi()
//     .Accepts<CreateJokeRequest>("application/json")
//     .Produces<JokeResponse>(StatusCodes.Status201Created)
//     .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest)
//     .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>();
//
// // Get Jokes by Type
// jokeGroup.MapGet("/type", async Task<IResult> ([AsParameters] FilterRequest request, JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var jokes = await jokeService.GetJokesByType(request, ct);
//         return jokes.Count != 0
//             ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Jokes", $"type {request.Type}");
//     })
//     .WithName("GetJokesByType")
//     .WithDescription("Retrieves jokes filtered by type with optional sorting and pagination")
//     .WithSummary("Get jokes by type")
//     .WithOpenApi()
//     .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest)
//     .AddEndpointFilter<ValidationFilter<FilterRequest>>();
//
// // Update Joke
// jokeGroup.MapPut("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, UpdateJokeRequest request, JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var joke = Mapper.UpdateRequestToJoke(id, request);
//         var updated = await jokeService.UpdateJoke(joke, ct);
//         return updated is not null
//             ? TypedResults.Ok(Mapper.JokeToJokeResponse(updated))
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Joke", id.ToString());
//     })
//     .WithName("UpdateJoke")
//     .WithDescription("Updates an existing joke's content and metadata")
//     .WithSummary("Update a joke")
//     .WithOpenApi()
//     .Accepts<UpdateJokeRequest>("application/json")
//     .Produces<JokeResponse>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest)
//     .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
//     .AddEndpointFilter<ValidationFilter<UpdateJokeRequest>>();
//
// // Delete Joke
// jokeGroup.MapDelete("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var result = await jokeService.DeleteJoke(id, ct);
//         return result
//             ? TypedResults.NoContent()
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Joke", id.ToString());
//     })
//     .WithName("DeleteJoke")
//     .WithDescription("Permanently removes a joke from the database")
//     .WithSummary("Delete a joke")
//     .WithOpenApi()
//     .Produces(StatusCodes.Status204NoContent)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest);
//
// // Get Random Joke
// jokeGroup.MapGet("/random", async Task<IResult> (JokeService jokeService, HttpContext context, CancellationToken ct) =>
//     {
//         var joke = await jokeService.GetRandomJoke(ct);
//         return joke is not null
//             ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Jokes", "random");
//     })
//     .WithName("GetRandomJoke")
//     .WithDescription("Retrieves a random joke from the available collection")
//     .WithSummary("Get a random joke")
//     .WithOpenApi()
//     .Produces<JokeResponse>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound);
//
// // Search Jokes
// jokeGroup.MapGet("/search", async Task<IResult> (
//     [FromQuery(Name = "q")] string query,
//     JokeService jokeService,
//     HttpContext context,
//     CancellationToken ct) =>
//     {
//         if (string.IsNullOrWhiteSpace(query))
//         {
//             return ProblemDetailsHelper.CreateBadRequestProblem(context, "Search query cannot be empty");
//         }
//
//         var jokes = await jokeService.SearchJokes(query, ct);
//         return jokes?.Count > 0
//             ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//             : ProblemDetailsHelper.CreateNotFoundProblem(context, "Jokes", $"matching query '{query}'");
//     })
//     .WithName("SearchJokes")
//     .WithDescription("Searches for jokes containing the specified query in their content or tags")
//     .WithSummary("Search for jokes")
//     .WithOpenApi()
//     .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest);
//
// // Get External Joke
// jokeGroup.MapGet("/whatever", async Task<IResult> (JokeApiService jokeApiService, HttpContext context, CancellationToken ct) =>
//     {
//         try
//         {
//             var joke = await jokeApiService.GetExternalJoke(ct);
//             return joke is not null
//                 ? TypedResults.Ok(joke)
//                 : ProblemDetailsHelper.CreateNotFoundProblem(context, "External Joke", "random");
//         }
//         catch (HttpRequestException ex)
//         {
//             return ProblemDetailsHelper.CreateExternalServiceProblem(context, "Joke API", "Failed to fetch joke from external service", ex);
//         }
//     })
//     .WithName("GetWhateverJoke")
//     .WithDescription("Retrieves a random joke from a third-party API")
//     .WithSummary("Get a joke from a third-party API")
//     .WithOpenApi()
//     .Produces<Joke>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status404NotFound)
//     .ProducesProblem(StatusCodes.Status502BadGateway);
//
// // User Login
// userGroup.MapPost("/login", async Task<IResult> ([FromBody] UserLoginRequest request, JwtTokenService jwtTokenService, HttpContext context) =>
//     {
//         var jwtToken = jwtTokenService.GenerateToken(request.Username, request.Email);
//         return !string.IsNullOrEmpty(jwtToken)
//             ? TypedResults.Ok(new { request.Username, Token = jwtToken })
//             : ProblemDetailsHelper.CreateUnauthorizedProblem(context, "Invalid credentials provided");
//     })
//     .WithName("UserLogin")
//     .WithDescription("Authenticates a user and returns a JWT token for subsequent requests")
//     .WithSummary("Login user")
//     .WithOpenApi()
//     .Accepts<UserLoginRequest>("application/json")
//     .Produces<object>(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status401Unauthorized)
//     .ProducesValidationProblem(StatusCodes.Status400BadRequest)
//     .AddEndpointFilter<ValidationFilter<UserLoginRequest>>();
//
// // User Logout
// userGroup.MapPost("/logout", async Task<IResult> ([FromServices] JwtTokenService jwtTokenService, HttpContext context) =>
//     {
//         var token = jwtTokenService.GetToken();
//         if (string.IsNullOrEmpty(token))
//         {
//             return ProblemDetailsHelper.CreateUnauthorizedProblem(context, "No valid authentication token found");
//         }
//
//         jwtTokenService.InvalidateToken(token);
//         return TypedResults.Ok();
//     })
//     .WithName("UserLogout")
//     .WithDescription("Invalidates the current user's JWT token")
//     .WithSummary("Logout user")
//     .WithOpenApi()
//     .Produces(StatusCodes.Status200OK)
//     .ProducesProblem(StatusCodes.Status401Unauthorized);
//
// app.Run();