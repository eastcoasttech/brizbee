using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class CustomerRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given customer in the repository.
        /// </summary>
        /// <param name="customer">The customer to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created customer</returns>
        public Customer Create(Customer customer, User currentUser)
        {
            customer.CreatedAt = DateTime.Now;
            customer.OrganizationId = currentUser.OrganizationId;
            db.Customers.Add(customer);

            db.SaveChanges();

            return customer;
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Returns the customer with the given id.
        /// </summary>
        /// <param name="id">The id of the customer</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The customer with the given id</returns>
        public Customer Get(int id, User currentUser)
        {
            return db.Customers.Find(id);
        }

        /// <summary>
        /// Returns a queryable collection of customers.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of customers</returns>
        public IQueryable<Customer> GetAll(User currentUser)
        {
            return db.Customers.AsQueryable<Customer>();
        }
    }
}