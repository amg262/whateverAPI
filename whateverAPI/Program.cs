using System.Net.Http.Headers;
using System.Text;
using FastEndpoints;
using FastEndpoints.Swagger;
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
using Microsoft.AspNetCore.Mvc;
using whateverAPI.Features.User;


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

// options.UseSqlServer(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         sqlOptions => sqlOptions.EnableRetryOnFailure())
//     .EnableDetailedErrors()
//     .EnableSensitiveDataLogging());


// builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = "supersecret");
// builder.Services.AddAuthorization();

// Add this after CreateBuilder
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Bind to localhost instead of any IP
        // options.ListenLocalhost(8080); // HTTP
        options.ListenLocalhost(8081, configure => configure.UseHttps()); // HTTPS
    });

    // builder.WebHost.ConfigureKestrel(options =>
    // {
    //     options.ListenAnyIP(8080); // HTTP
    //     options.ListenAnyIP(8081, configure => configure.UseHttps()); // HTTPS
    // });
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<JokeApiService>();
builder.Services.AddScoped<IJokeService, JokeService>();


builder.Services.AddHttpClient<JokeApiService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.BaseAddress = new Uri(builder.Configuration["JokeApiOptions:BaseUrl"] ?? string.Empty);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddStandardResilienceHandler();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

        // This fires before the request is processed to add the Authorization header to the request
        // or to get the token from the request and add it to the Authorization header if it's set by Swagger UI
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var jwtTokenService = context.HttpContext.RequestServices.GetRequiredService<JwtTokenService>();
                jwtTokenService.GetToken(context);
                return Task.CompletedTask;
            }
        };
    });

// Validation Filter


builder.Services.AddAuthorization();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped(typeof(ValidationFilter<>));

// builder.Services.AddScoped<IBaseRepository<Joke, Guid>, JokeRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseDefaultExceptionHandler();
// app.UseFastEndpoints(c => { c.Endpoints.RoutePrefix = "api"; });
// app.UseSwaggerGen();


app.UseAuthentication();
app.UseAuthorization();

await DbInitializer.InitDb(app);

app.MapControllers();

// Endpoints
var api = app.MapGroup("/api");
var jokes = api.MapGroup("/jokes").WithTags("Jokes");
var user = api.MapGroup("/user").WithTags("User");

api.MapDelete("/delete/{Id}", async Task<IResult> (Guid id, IJokeService jokeService) =>
    {
        await jokeService.DeleteJoke(id);
        return TypedResults.Ok();
    }).WithName("DeleteToken")
    .WithDescription("Delete the token")
    .WithOpenApi();

jokes.MapGet("/", async Task<IResult> (IJokeService jokeService) =>
    {
        var jokes = await jokeService.GetJokes();

        if (jokes is null) return TypedResults.NotFound();
        return TypedResults.Ok(EntityMapper.JokesToJokeReponses(jokes));
    })
    .WithName("GetJokes")
    .WithDescription("Get all jokes")
    .WithOpenApi();

jokes.MapGet("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, IJokeService jokeService) =>
    {
        var joke = await jokeService.GetJokeById(id);

        return joke is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(EntityMapper.JokeToJokeResponse(joke));
    })
    .WithName("GetJokeById")
    .WithDescription("Get a joke by ID")
    .WithOpenApi();

jokes.MapPost("/", async Task<IResult> (CreateJokeRequest request, IJokeService jokeService) =>
    {
        var joke = EntityMapper.CreateRequestToJoke(request);
        var created = await jokeService.CreateJoke(joke);
        var response = EntityMapper.JokeToJokeResponse(created);

        return response is null
            ? TypedResults.BadRequest()
            : TypedResults.Created($"/api/jokes/{created.Id}", response);
    })
    .WithName("CreateJoke")
    .WithDescription("Create a new joke")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>();

jokes.MapPut("/{id:guid}", async Task<IResult> ([FromRoute] Guid id, UpdateJokeRequest request, IJokeService jokeService) =>
    {
        var joke = EntityMapper.UpdateRequestToJoke(id, request);
        // joke.Id = id;
        var updated = await jokeService.UpdateJoke(joke);

        return updated is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(EntityMapper.JokeToJokeResponse(updated));
    }).WithName("UpdateJoke")
    .WithDescription("Update a joke by ID")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<UpdateJokeRequest>>();


jokes.MapDelete("/{id}", async Task<IResult> (Guid id, IJokeService jokeService) =>
    {
        var result = await jokeService.DeleteJoke(id);
        return result
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }).WithName("DeleteJoke")
    .WithDescription("Delete a joke by ID")
    .AddEndpointFilter<ValidationFilter<DeleteJokeRequest>>()
    .WithOpenApi();

user.MapPost("/login", async Task<IResult> (UserLogin.Request request, JwtTokenService jwtTokenService) =>
    {
        var jwtToken = jwtTokenService.GenerateToken(request.Name, request.Username);
        return TypedResults.Ok(new { request.Username, Token = jwtToken });
    }).WithName("UserLogin")
    .WithDescription("Login")
    .WithOpenApi()
    .AddEndpointFilter<ValidationFilter<UserLogin.Request>>();

user.MapPost("/logout", async Task<IResult> (JwtTokenService jwtTokenService) =>
    {
        var token = jwtTokenService.GetToken();
        jwtTokenService.InvalidateToken(token);
        return TypedResults.Ok();
    }).WithName("UserLogout")
    .WithDescription("Logout")
    .WithOpenApi();

app.Run();