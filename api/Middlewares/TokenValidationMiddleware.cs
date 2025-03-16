using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly string _allowedRole;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    public TokenValidationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _allowedRole = _configuration["Keycloak:AllowedRole"];
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Извлечение токена из заголовка Authorization
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogDebug("Missing Authorization header.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing Authorization header.");
            return;
        }

        var token = authHeader.ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("Invalid Authorization header format.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid Authorization header format.");
            return;
        }

        // Валидация токена
        var tokenValidator = new TokenValidator(_configuration);
        var principal = await tokenValidator.ValidateTokenAsync(token);
        _logger.LogDebug($"token-{token}");
        _logger.LogDebug($"token-{_configuration["Keycloak:Authority"]}");
        if (principal == null)
        {
            _logger.LogDebug("Invalid or expired token.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or expired token.");
            return;
        }

        // Проверка роли
        var roles = GetRolesFromClaims(principal);

        if (!roles.Contains(_allowedRole))
        {
            _logger.LogDebug("User does not have the required role.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("User does not have the required role.");
            return;
        }

        // Если токен валиден и роль подтверждена, передаем управление следующему middleware
        await _next(context);
    }

    private static List<string> GetRolesFromClaims(ClaimsPrincipal principal)
    {
        var roles = new List<string>();

        // Извлечение глобальных ролей (realm_access.roles)
        var realmAccessClaim = principal.Claims.FirstOrDefault(c => c.Type == "realm_access");
        if (realmAccessClaim != null)
        {
            var realmAccess = JsonSerializer.Deserialize<RealmAccess>(realmAccessClaim.Value);
            roles.AddRange(realmAccess.Roles);
        }

        return roles;
    }
}

// Вспомогательные классы для десериализации
public class RealmAccess
{
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; }
}
