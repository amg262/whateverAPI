using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure())
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging());
// options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
//         o => o.EnableRetryOnFailure())
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


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

builder.Services.AddAuthenticationJwtBearer(s=>s.SigningKey = "supersecretsupersecretsupersecretsupersecretsupersecretsupersecret");
builder.Services.AddAuthorization();

builder.Services.AddScoped<IJokeService, JokeService>();
builder.Services.AddScoped<ITagService, TagService>();
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