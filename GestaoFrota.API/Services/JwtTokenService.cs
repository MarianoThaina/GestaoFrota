using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestaoFrota.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace GestaoFrota.API.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var chave = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurado.");
        var emissor = jwtSection["Issuer"] ?? "GestaoFrota.API";
        var audiencia = jwtSection["Audience"] ?? "GestaoFrota.App";
        var minutosExpiracao = int.TryParse(jwtSection["ExpiracaoMinutos"], out var m) ? m : 480;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            // Claim de role usada pelo RBAC ([Authorize(Roles = "...")])
            new(ClaimTypes.Role, usuario.Perfil.ToString())
        };

        var expiraEm = DateTime.UtcNow.AddMinutes(minutosExpiracao);

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: audiencia,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
