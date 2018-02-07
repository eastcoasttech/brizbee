using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Brizbee
{
    public class SecurityService
    {
        public string GenerateHash(string value)
        {
            if ((value == null) || (value.Length == 0)) { return ""; }

            var sha512 = System.Security.Cryptography.SHA512.Create();
            var inputBytes = Encoding.ASCII.GetBytes(value);
            var hash = sha512.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public string GeneratePasswordString()
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 1; i < 8 + 1; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26
                    * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Generates a random string that is 150 characters long
        /// </summary>
        /// <returns>A string of random characters</returns>
        public string GenerateRandomString()
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 1; i < 150 + 1; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26
                    * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public bool AuthenticateWithPassword(User user, string password)
        {
            var contents = string.Format("{0} {1}", password,
                user.PasswordSalt);
            var calculatedHash = GenerateHash(contents);
            var storedHash = user.PasswordHash;
            return calculatedHash == storedHash;
        }
    }
}