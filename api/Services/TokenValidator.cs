using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

public class TokenValidator
{
    private readonly IConfiguration _configuration;

    public TokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
    {
        var authority = _configuration["Keycloak:Authority"];
        var audience = _configuration["Keycloak:Audience"];

        var httpDocumentRetriever = new HttpDocumentRetriever
        {
            RequireHttps = false // Отключаем требование HTTPS
        };

        // Загрузка конфигурации OpenID Connect
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            httpDocumentRetriever);

        var openIdConfig = await configurationManager.GetConfigurationAsync(default);

        // Настройка параметров валидации токена
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = authority,
            ValidAudience = audience,
            IssuerSigningKeys = openIdConfig.SigningKeys,
            ValidateLifetime = true,
            ValidateIssuer = true,
            ValidateAudience = false
        };

        // Проверка токена
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (SecurityTokenException)
        {
            // Токен недействителен
            return null;
        }
    }
}