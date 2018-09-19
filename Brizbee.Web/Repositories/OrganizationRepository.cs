using Brizbee.Common.Models;
using Microsoft.AspNet.OData;
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class OrganizationRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
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
                var customerService = new StripeCustomerService();
                var sourceService = new StripeSourceService();
                
                // Attach the card source id to the customer
                var attachOptions = new StripeSourceAttachOptions()
                {
                    Source = organization.StripeSourceId
                };
                sourceService.Attach(organization.StripeCustomerId, attachOptions);

                // Update the customer's default source
                var customerOptions = new StripeCustomerUpdateOptions()
                {
                    DefaultSource = organization.StripeSourceId
                };
                StripeCustomer customer = customerService.Update(organization.StripeCustomerId, customerOptions);

                var source = sourceService.Get(organization.StripeSourceId);

                // Record the card details
                organization.StripeSourceCardLast4 = source.Card.Last4;
                organization.StripeSourceCardBrand = source.Card.Brand;
                organization.StripeSourceCardExpirationMonth = source.Card.ExpirationMonth.ToString();
                organization.StripeSourceCardExpirationYear = source.Card.ExpirationYear.ToString();
            }

            db.SaveChanges();

            return organization;
        }
    }
}