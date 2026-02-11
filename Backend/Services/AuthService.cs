using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ResourcePlanPro.API.Models;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public interface IAuthService
    {
        string GenerateToken(User user);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey not configured");
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "480");

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string HashPassword(string password)
        {
            // Use PBKDF2 with a random salt for secure password hashing
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            // Combine salt + hash for storage
            byte[] hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashBytes;
            try
            {
                hashBytes = Convert.FromBase64String(storedHash);
            }
            catch (FormatException)
            {
                // Fall back to legacy SHA256 comparison for existing accounts
                using var sha256 = SHA256.Create();
                var legacyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var legacyHashString = BitConverter.ToString(legacyHash).Replace("-", "");
                return string.Equals(legacyHashString, storedHash, StringComparison.OrdinalIgnoreCase);
            }

            if (hashBytes.Length != 48)
                return false;

            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] computedHash = pbkdf2.GetBytes(32);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                hashBytes.AsSpan(16, 32),
                computedHash.AsSpan());
        }
    }
}
