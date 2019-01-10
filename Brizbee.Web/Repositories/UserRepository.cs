using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Policies;
using Microsoft.AspNet.OData;
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            user.CreatedAt = DateTime.UtcNow;
            user.OrganizationId = currentUser.OrganizationId;

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
            using (var transaction = db.Database.BeginTransaction())
            {
                try
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
                    user.CreatedAt = DateTime.UtcNow;
                    organization.CreatedAt = DateTime.UtcNow;
                    organization.Code = GenerateOrganizationCode();

                    try
                    {
                        // Create a Stripe customer object
                        var customerOptions = new StripeCustomerCreateOptions
                        {
                            Email = user.EmailAddress
                        };
                        var customers = new StripeCustomerService();
                        StripeCustomer customer = customers.Create(customerOptions);
                        organization.StripeCustomerId = customer.Id;

                        // Subscribe the customer to the default plan
                        var items = new List<StripeSubscriptionItemOption> {
                            new StripeSubscriptionItemOption {
                                PlanId = "plan_CREJJE9VGQJjnN",
                                Quantity = 1,
                            }
                        };
                        var subscriptionOptions = new StripeSubscriptionCreateOptions
                        {
                            Items = items,
                            TrialPeriodDays = 30,
                            CustomerId = customer.Id
                        };
                        var subscriptions = new StripeSubscriptionService();
                        StripeSubscription subscription = subscriptions.Create(subscriptionOptions);
                        organization.StripeSubscriptionId = subscription.Id;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning(ex.ToString());
                        #if DEBUG
                        organization.StripeCustomerId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString());
                        organization.StripeSubscriptionId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString());
                        #endif
                        #if !DEBUG
                        throw;
                        #endif
                    }

                    // Save the organization and user
                    db.Organizations.Add(organization);
                    user.OrganizationId = organization.Id;

                    db.Users.Add(user);

                    db.SaveChanges();

                    transaction.Commit();

                    return user;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(ex.ToString());
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private string GenerateOrganizationCode()
        {
            var code = new Random().Next(1000, 9999).ToString();
            if (db.Organizations.Where(o => o.Code == code).Any())
            {
                return GenerateOrganizationCode();
            }
            else
            {
                return code;
            }
        }
    }
}