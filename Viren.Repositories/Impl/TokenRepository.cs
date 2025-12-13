using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Interfaces;

namespace Viren.Repositories.Impl
{
    public class TokenRepository : ITokenRepository
    {
        private readonly IConfiguration _configuration;

        public TokenRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string, int) GenerateJwtToken(ApplicationUser user, string role)
        {
            var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.UserName!),
            new Claim(ClaimTypes.Role, role)
        };
            var expiration = double.Parse(_configuration["JwtSettings:AccessTokenExpirationSeconds"]
                ?? throw new ArgumentException("Expiration time cannot be null"));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]
                ?? throw new ArgumentException("Key cannot be null")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"],
                claims,
                expires: DateTime.UtcNow.AddSeconds(expiration),
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), (int)expiration);
        }
    }
}
