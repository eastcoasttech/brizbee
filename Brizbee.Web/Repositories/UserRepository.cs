using Brizbee.Common.Models;
using Brizbee.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class UserRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        
        /// <summary>
        /// Changes the password to the given password.
        /// </summary>
        /// <param name="id">The id of the user for which to change the password</param>
        /// <param name="password">The new password</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void ChangePassword(int id, string password, User currentUser)
        {
            var user = db.Users.Find(id);

            // Ensure that user is authorized
            if (!UserPolicy.CanChangePassword(user, currentUser))
            {
                throw new Exception("Not authorized to change the password");
            }

            // Generates a password hash and salt
            var service = new SecurityService();
            user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
            user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", password, user.PasswordSalt));
            user.Password = null;
            
            db.SaveChanges();
        }

        /// <summary>
        /// Creates the given user in the database.
        /// </summary>
        /// <param name="user">The user to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created user</returns>
        public User Create(User user, User currentUser)
        {
            // Ensure that user is authorized
            if (!UserPolicy.CanCreate(user, currentUser))
            {
                throw new Exception("Not authorized to create the object");
            }

            // Auto-generated
            user.CreatedAt = DateTime.Now;
            user.OrganizationId = currentUser.OrganizationId;

            db.Users.Add(user);

            db.SaveChanges();

            return user;
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Returns the user with the given id.
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The user with the given id</returns>
        public User Get(int id, User currentUser)
        {
            return db.Users
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of users.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of users</returns>
        public IQueryable<User> GetAll(User currentUser)
        {
            return db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .AsQueryable<User>();
        }

        public User Register(User user, Organization organization)
        {
            // Ensure Email address is unique
            var duplicate = db.Users.Where(u => u.EmailAddress.ToLower().Equals(user.EmailAddress));
            if (duplicate.Any())
            {
                throw new Exception("Email Address is already taken");
            }

            // Generates a password hash and salt
            var service = new SecurityService();
            user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
            user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
            user.Password = null;

            // Auto-generated
            user.Role = "Administrator";
            user.CreatedAt = DateTime.Now;
            organization.CreatedAt = DateTime.Now;

            db.Organizations.Add(organization);
            user.OrganizationId = organization.Id;

            db.Users.Add(user);

            db.SaveChanges();

            return user;
        }
    }
}