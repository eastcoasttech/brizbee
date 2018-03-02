using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Policies;
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.OData;

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

        /// <summary>
        /// Updates the given user with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="patch">The changes that should be made to the user</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated user</returns>
        public User Update(int id, Delta<User> patch, User currentUser)
        {
            var user = db.Users.Find(id);
            
            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("EmailAddress"))
            {
                throw new Exception("Cannot modify OrganizationId or EmailAddress");
            }

            // Peform the update
            patch.Patch(user);

            if (user.Password != null)
            {
                // Generates a password hash and salt
                var service = new SecurityService();
                user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
                user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
                user.Password = null;
            }
            else
            {
                user.Password = null;
            }

            db.SaveChanges();

            return user;
        }

        public User Register(User user, Organization organization)
        {
            // Ensure Email address is unique
            var duplicate = db.Users.Where(u => u.EmailAddress.ToLower().Equals(user.EmailAddress));
            if (duplicate.Any())
            {
                throw new DuplicateException("Email Address is already taken");
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

            try
            {
                // Create a Stripe customer object
                var cOptions = new StripeCustomerCreateOptions
                {
                    Email = user.EmailAddress,
                };
                var cService = new StripeCustomerService();
                StripeCustomer customer = cService.Create(cOptions);
                organization.StripeCustomerId = customer.Id;

                // Subscribe the customer to the default plan
                var items = new List<StripeSubscriptionItemOption> {
                    new StripeSubscriptionItemOption {
                        PlanId = "plan_CQ78r61TXy57j8",
                        Quantity = 1,
                    }
                };
                var sOptions = new StripeSubscriptionCreateOptions
                {
                    Items = items,
                    TrialPeriodDays = 30
                };
                var sService = new StripeSubscriptionService();
                StripeSubscription subscription = sService.Create(customer.Id, sOptions);
                organization.StripeSubscriptionId = subscription.Id;
            }
            catch (Exception ex)
            {
                throw new Common.Exceptions.StripeException(ex.Message);
            }
            
            // Save the organization and user
            db.Organizations.Add(organization);
            user.OrganizationId = organization.Id;

            db.Users.Add(user);

            db.SaveChanges();

            return user;
        }
    }
}