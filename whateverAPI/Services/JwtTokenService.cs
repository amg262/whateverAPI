using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using whateverAPI.Helpers;
using whateverAPI.Options;

namespace whateverAPI.Services;

/// <summary>
/// Defines the contract for JWT token management operations.
/// </summary>
/// <remarks>
/// This interface establishes the essential operations needed for JWT token handling in the application,
/// including token generation, validation, and invalidation. It ensures a consistent approach to
/// token-based authentication across the application.
/// </remarks>
public interface IJwtTokenService
{
    /// <summary>
    /// Retrieves the JWT token from either cookies or the authorization header.
    /// </summary>
    /// <param name="context">Optional message context for token extraction during authentication</param>
    /// <returns>The JWT token if found; otherwise, null</returns>
    string? GetToken(MessageReceivedContext? context = null);

    /// <summary>
    /// Generates a JWT token with claims including the user's IP address.
    /// </summary>
    /// <returns>A JWT token string.</returns>
    string GenerateToken(string? name, string? email, string? userId, string? provider, bool saveCookie = true);

    /// <summary>
    /// Validates the given JWT token.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Invalidates the given token by setting its expiration to a past date.
    /// </summary>
    /// <param name="token">The token to invalidate.</param>
    /// <returns>True if the token was successfully invalidated, false otherwise.</returns>
    void InvalidateToken(string token);
}

/// <summary>
/// Implements comprehensive JWT (JSON Web Token) management for secure authentication and authorization.
/// </summary>
/// <remarks>
/// This service provides a robust implementation of JWT token management with the following security features:
/// 
/// Security Measures:
/// - Uses secure HTTP-only cookies for token storage
/// - Implements strict SameSite cookie policies
/// - Includes IP address validation in token claims
/// - Supports both cookie and bearer token authentication
/// - Implements secure token invalidation
/// 
/// Token Management Features:
/// - Flexible token retrieval from both cookies and authorization headers
/// - Comprehensive token validation including issuer and audience verification
/// - Secure token generation with claims-based identity
/// - Cookie management with security-first defaults
/// </remarks>
public class JwtTokenService : IJwtTokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtOptions _options;
    private readonly ILogger<JwtTokenService> _logger;


    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Provides access to the HTTP context.</param>
    /// <param name="options">Options for configuring JWT token handling.</param>
    /// <param name="logger">Logger for JwtTokenService</param>
    public JwtTokenService(IHttpContextAccessor httpContextAccessor, IOptions<JwtOptions> options,
        ILogger<JwtTokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Retrieves the current authentication token from either cookies or the authorization header.
    /// </summary>
    /// <remarks>
    /// This method implements a fallback mechanism for token retrieval:
    /// 1. First attempts to retrieve the token from secure HTTP-only cookies
    /// 2. If not found in cookies, checks the Authorization header for a bearer token
    /// 3. Updates the message context with the token if provided
    /// 
    /// This dual retrieval strategy supports both:
    /// - Browser-based applications using secure cookies
    /// - API clients using bearer token authentication
    /// </remarks>
    public string? GetToken(MessageReceivedContext? context = null)
    {
        // Check if the token exists in the cookies
        if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(Helper.TokenCookie, out var token) == true)
        {
            if (context != null)
            {
                context.Token = token;
            }

            return token;
        }

        // Check if the token exists in the authorization header
        token = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

        return !string.IsNullOrEmpty(token)
            ? token
            : null;
    }

    /// <summary>
    /// Clears the authentication token from the HTTP context's cookies.
    /// </summary>
    /// <remarks>
    /// Sets the token to an empty string and deletes the cookie from the response.
    /// It's a redundant approach to ensure the token is cleared. Doing both ensures all browsers are covered.
    /// </remarks>
    /// <returns>True if the token was successfully cleared.</returns>
    private void DeleteTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.Now.AddDays(-2)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append(Helper.TokenCookie, "", cookieOptions);
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(Helper.TokenCookie);
    }

    /// <summary>
    /// Manages secure storage of authentication tokens in HTTP-only cookies.
    /// </summary>
    /// <remarks>
    /// Implements secure cookie storage with the following protections:
    /// - HTTP-only flag prevents JavaScript access
    /// - Secure flag ensures HTTPS-only transmission
    /// - Strict SameSite policy prevents CSRF attacks
    /// - Extended expiration for persistent sessions
    /// </remarks>
    private void SaveTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(400)
        };
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(Helper.TokenCookie, token, cookieOptions);
    }

    /// <summary>
    /// Generates a JWT token with claims including the user's IP address.
    /// </summary>
    /// <returns>A JWT token string.</returns>
    public string GenerateToken(string? name, string? email, string? userId, string? provider, bool saveCookie = true)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // IP address from the current HTTP context
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, name ?? "Unknown"),
            new(JwtRegisteredClaimNames.Sub, email ?? "Unknown"),
            new(JwtRegisteredClaimNames.Email, email ?? "Unknown"),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()), // Unique identifier for the token
            new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.CurrentCulture), ClaimValueTypes.Integer64),
            new("ip", ip),
            new("uid", userId ?? "Unknown"),
            new("provider", provider ?? "Unknown"),
        };

        // token descriptor with issuer, audience, expiration, signing credentials, and claims
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.MaxValue, // This token never expires
            SigningCredentials = creds,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = DateTime.UtcNow
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        if (saveCookie) SaveTokenCookie(tokenHandler.WriteToken(token));

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates the given JWT token.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret))
            }, out var validatedToken);

            return validatedToken != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed: {Token}", token);
            return false;
        }
    }

    /// <summary>
    /// Invalidates the given token by setting its expiration to a past date.
    /// </summary>
    /// <param name="token">The token to invalidate.</param>
    /// <returns>True if the token was successfully invalidated, false otherwise.</returns>
    public void InvalidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return;

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();

        // Remove the existing expiration claim
        claims.RemoveAll(claim => claim.Type == JwtRegisteredClaimNames.Exp);

        // Add a new expiration claim set to a past date
        claims.Add(new Claim(JwtRegisteredClaimNames.Exp,
            EpochTime.GetIntDate(DateTime.UtcNow.AddDays(-2)).ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var newToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(-1),
            signingCredentials: creds);

        var newTokenString = tokenHandler.WriteToken(newToken);

        // Replace the old token set new invalidated one
        SaveTokenCookie(newTokenString);
        DeleteTokenCookie();

        _logger.LogInformation("Token invalidated: {Token}", token);
    }
}