using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Brizbee.Core.Models;
using Brizbee.Dashboard.Server.Serialization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Brizbee.Dashboard.Server.Services
{
    public class AuthService(
        ApiService apiService,
        IDbContextFactory<PrimaryDbContext> dbContextFactory,
        IConfiguration configuration,
        ProtectedSessionStorage protectedSessionStorage)
    {
        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            apiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public void ResetHeaders()
        {
            apiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }

        public async Task<string?> AuthenticateWithPinAsync(PinSession session)
        {
            // Validate presence of both an organization code and user PIN.
            if (string.IsNullOrEmpty(session.UserPin) ||
                string.IsNullOrEmpty(session.OrganizationCode))
                return null;
            
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var found = await context.Users!
                .Include(u => u.Organization)
                .Where(u => u.IsDeleted == false)
                .Where(u => u.IsActive == true)
                .Where(u => u.Organization!.Code! == session.OrganizationCode)
                .Where(u => u.Pin! == session.UserPin)
                .FirstOrDefaultAsync();
            
            // Attempt to authenticate.
            if (found == null)
                return null;
            
            // Store the user ID to be retrieved later.
            await protectedSessionStorage.SetAsync("userId", found.Id);

            var token = GenerateJsonWebToken(found.Id, found.EmailAddress);
            
            // Store the JWT to be retrieved later.
            await protectedSessionStorage.SetAsync("token", token);

            return token;
        }

        public async Task<string?> AuthenticateWithEmailAsync(EmailSession session)
        {
            var service = new SecurityService();

            // Validate presence of both an Email and Password.
            if (string.IsNullOrEmpty(session.EmailAddress) ||
                string.IsNullOrEmpty(session.EmailPassword))
                return null;
            
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var found = await context.Users!
                .Where(u => u.IsDeleted == false)
                .Where(u => u.IsActive == true)
                .Where(u => u.EmailAddress == session.EmailAddress)
                .FirstOrDefaultAsync();

            // Attempt to authenticate.
            if ((found == null) ||
                !service.AuthenticateWithPassword(found.PasswordSalt!, found.PasswordHash!, session.EmailPassword))
                return null;
            
            // Store the user ID to be retrieved later.
            await protectedSessionStorage.SetAsync("userId", found.Id);

            var token = GenerateJsonWebToken(found.Id, found.EmailAddress);
            
            // Store the JWT to be retrieved later.
            await protectedSessionStorage.SetAsync("token", token);

            return token;
        }

        public async Task<User?> GetMeAsync()
        {
            var userId = await protectedSessionStorage.GetAsync<int>("userId");

            if (!userId.Success)
                return null;
            
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var user = await context.Users!
                .Include(u => u.Organization)
                .Where(u => u.Id == userId.Value)
                .FirstOrDefaultAsync();

            return user;
        }
        
        private string GenerateJsonWebToken(int userId, string? emailAddress)
        {
            var key = configuration.GetValue<string>("Jwt:Key");
            var issuer = configuration.GetValue<string>("Jwt:Issuer");

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer))
            {
                // TODO throw an exception
                return string.Empty;
            }
            
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, string.IsNullOrEmpty(emailAddress) ? "" : emailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(issuer,
                issuer,
                claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
