﻿//
//  CustomerRepository.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Web.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Repositories
{
    public class CustomerRepository : IDisposable
    {
        private SqlContext db = new SqlContext();

        /// <summary>
        /// Creates the given customer in the database.
        /// </summary>
        /// <param name="customer">The customer to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created customer</returns>
        public Customer Create(Customer customer, User currentUser)
        {
            // Ensure that user is authorized
            if (!CustomerPolicy.CanCreate(customer, currentUser))
            {
                throw new NotAuthorizedException("Not authorized to create the object");
            }

            // Auto-generated
            customer.CreatedAt = DateTime.UtcNow;
            customer.OrganizationId = currentUser.OrganizationId;

            db.Customers.Add(customer);

            db.SaveChanges();

            return customer;
        }

        /// <summary>
        /// Deletes the customer with the given id.
        /// </summary>
        /// <param name="id">The id of the customer</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var customer = db.Customers.Find(id);

            // Ensure that user is authorized
            if (!CustomerPolicy.CanDelete(customer, currentUser))
            {
                throw new NotAuthorizedException("Not authorized to delete the object");
            }

            // Delete the object itself
            db.Customers.Remove(customer);

            db.SaveChanges();
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
        public IQueryable<Customer> Get(int id, User currentUser)
        {
            return db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of customers.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of customers</returns>
        public IQueryable<Customer> GetAll(User currentUser)
        {
            return db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId);
        }

        /// <summary>
        /// Updates the given customer with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the customer</param>
        /// <param name="patch">The changes that should be made to the customer</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated customer</returns>
        public Customer Update(int id, Delta<Customer> patch, User currentUser)
        {
            var customer = db.Customers.Find(id);

            // Ensure that object was found
            if (customer == null) { throw new NotFoundException("No object was found with that ID in the database"); }
            
            // Ensure that user is authorized
            if (!CustomerPolicy.CanUpdate(customer, currentUser))
            {
                throw new NotAuthorizedException("Not authorized to modify the object");
            }

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId"))
            {
                throw new NotAuthorizedException("Not authorized to modify the OrganizationId");
            }

            // Peform the update
            patch.Patch(customer);

            db.SaveChanges();

            return customer;
        }
    }
}