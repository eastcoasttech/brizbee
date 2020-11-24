//
//  OrganizationRepository.cs
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

using Brizbee.Common.Models;
using Microsoft.AspNet.OData;
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Repositories
{
    public class OrganizationRepository : IDisposable
    {
        private SqlContext db = new SqlContext();

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }
        
        /// <summary>
        /// Returns the organization with the given id.
        /// </summary>
        /// <param name="id">The id of the organization</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The organization with the given id</returns>
        public IQueryable<Organization> Get(int id, User currentUser)
        {
            return db.Organizations
                .Where(o => o.Id == currentUser.OrganizationId)
                .Where(o => o.Id == id);
        }

        /// <summary>
        /// Updates the given organization with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the organization</param>
        /// <param name="patch">The changes that should be made to the organization</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated organization</returns>
        public Organization Update(int id, Delta<Organization> patch, User currentUser)
        {
            var organization = db.Organizations.Find(id);

            // Ensure that object was found
            if (organization == null) { throw new Exception("No object was found with that ID in the database"); }

            // Ensure that user is authorized
            //if (!TaskPolicy.CanUpdate(task, currentUser))
            //{
            //    throw new Exception("Not authorized to modify the object");
            //}

            // Peform the update
            patch.Patch(organization);
            
            // Update the Stripe payment source if it is provided
            if (patch.GetChangedPropertyNames().Contains("StripeSourceId"))
            {
                var customerService = new CustomerService();
                var sourceService = new SourceService();
                
                // Attach the card source id to the customer
                var attachOptions = new SourceAttachOptions()
                {
                    Source = organization.StripeSourceId
                };
                sourceService.Attach(organization.StripeCustomerId, attachOptions);

                // Update the customer's default source
                var customerOptions = new CustomerUpdateOptions()
                {
                    DefaultSource = organization.StripeSourceId
                };
                Stripe.Customer customer = customerService.Update(organization.StripeCustomerId, customerOptions);

                var source = sourceService.Get(organization.StripeSourceId);

                // Record the card details
                organization.StripeSourceCardLast4 = source.Card.Last4;
                organization.StripeSourceCardBrand = source.Card.Brand;
                organization.StripeSourceCardExpirationMonth = source.Card.ExpMonth.ToString();
                organization.StripeSourceCardExpirationYear = source.Card.ExpYear.ToString();
            }

            db.SaveChanges();

            return organization;
        }
    }
}