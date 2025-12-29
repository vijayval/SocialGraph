using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;

namespace Stunsy.SocialGraph.Api.Auth;

/// <summary>
/// Custom JWT handler that sanitizes tokens before validation to prevent injection attacks
/// </summary>
public class SanitizingJsonWebTokenHandler : JsonWebTokenHandler
{
    private static readonly Regex AllowedTokenPattern = new(@"^[A-Za-z0-9\-_\.]+$", RegexOptions.Compiled);

    public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        // Sanitize the token first
        var sanitizedToken = SanitizeToken(token);
        
        if (string.IsNullOrEmpty(sanitizedToken))
        {
            return new TokenValidationResult
            {
                IsValid = false,
                Exception = new SecurityTokenException("Token contains invalid characters")
            };
        }

        // Call base validation with sanitized token
        return await base.ValidateTokenAsync(sanitizedToken, validationParameters);
    }

    private static string SanitizeToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s.Substring(1, s.Length - 2);
        // Remove control/zero-width characters and newlines
        s = new string(s.Where(c => !char.IsControl(c)).ToArray());
        return s;
    }
}
