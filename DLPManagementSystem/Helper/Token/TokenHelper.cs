using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DLPManagementSystem.Helper.Token
{
    public static class TokenHelper
    {
        public static string GenerateJWTToken(int userId, string firstName, string role, IConfiguration config)
        {
            var jwtToken = new JwtSecurityTokenHandler();

            var secret = config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT key is missing from configuration.");

            var tokenByteKey = Encoding.UTF8.GetBytes(secret);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim("UserId", userId.ToString()),
                new Claim("FirstName", firstName),
                new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(tokenByteKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtToken.CreateToken(descriptor);
            return jwtToken.WriteToken(token);
        }

        public static string IsValidToken(string token, IConfiguration config)
        {
            try
            {
                var jwtToken = new JwtSecurityTokenHandler();

                var secret = config["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(secret))
                    return "Invalid JWT configuration.";

                var tokenByteKey = Encoding.UTF8.GetBytes(secret);

                var tokenValidatorParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(tokenByteKey),

                    ValidateLifetime = true,

                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],

                    ValidateAudience = true,
                    ValidAudience = config["Jwt:Audience"],

                    ClockSkew = TimeSpan.Zero
                };

                jwtToken.ValidateToken(token, tokenValidatorParams, out SecurityToken validToken);

                return "valid";
            }
            catch (Exception ex)
            {
                return $"Invalid {ex.Message}";
            }
        }
    }
}
