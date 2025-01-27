﻿namespace whateverAPI.Options;

/// <summary>
/// Represents the configuration options for JSON Web Tokens (JWT) used in the application.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Gets or initializes the secret key used for signing the JWT.
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the issuer of the JWT.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the audience for the JWT.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the expiration time in days for the JWT.
    /// </summary>
    public double ExpirationInDays { get; init; } = 90;
}