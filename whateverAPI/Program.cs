using System.Net.Http.Headers;
using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;
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
        options.ListenAnyIP(8080); // HTTP
        options.ListenAnyIP(8081, configure => configure.UseHttps()); // HTTPS
    });
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<JokeApiService>();
builder.Services.AddScoped<IJokeService, JokeService>();


builder.Services.AddHttpClient<JokeApiService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.BaseAddress = new Uri(builder.Configuration["JokeApiOptions:BaseUrl"] ?? string.Empty);
    // client.DefaultRequestHeaders.Add("username", builder.Configuration["DelivraOptions:Username"]);
    // client.DefaultRequestHeaders.Add("password", builder.Configuration["DelivraOptions:Password"]);
    // client.DefaultRequestHeaders.Add("listname", builder.Configuration["DelivraOptions:Listname"]);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddStandardResilienceHandler();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.DocumentName = "v1";
        s.Title = "whateverAPI";
        s.Version = "v1";
    };
});
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
                var token = jwtTokenService.GetToken(context);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();


// builder.Services.AddScoped<IBaseRepository<Joke, Guid>, JokeRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultExceptionHandler();
app.UseFastEndpoints(c => { c.Endpoints.RoutePrefix = "api"; });
app.UseSwaggerGen();

app.UseAuthentication();
app.UseAuthorization();

await DbInitializer.InitDb(app);


app.Run();