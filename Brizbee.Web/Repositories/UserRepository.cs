﻿//
//  UserRepository.cs
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
using Microsoft.AspNet.OData;
using SendGrid;
using SendGrid.Helpers.Mail;
using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace Brizbee.Web.Repositories
{
    public class UserRepository : IDisposable
    {
        private SqlContext db = new SqlContext();
        
        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        public User Register(User user, Organization organization)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Ensure Email address is unique
                    var duplicate = db.Users.Where(u => u.EmailAddress.ToLower().Equals(user.EmailAddress.ToLower()));
                    if (duplicate.Any())
                    {
                        throw new DuplicateException("Email Address is already taken");
                    }

                    // Generates a password hash and salt
                    var service = new SecurityService();
                    user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
                    user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
                    user.Password = null;

                    // Formatting
                    user.EmailAddress = user.EmailAddress.ToLower();

                    // Auto-generated
                    user.Role = "Administrator";
                    user.CreatedAt = DateTime.UtcNow;
                    user.AllowedPhoneNumbers = "*";
                    organization.CreatedAt = DateTime.UtcNow;
                    organization.MinutesFormat = "minutes";

#if DEBUG
                    organization.StripeCustomerId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString());
                    organization.StripeSubscriptionId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString());
#endif

                    // Determine the actual Stripe Plan Id based on the PlanId
                    var stripePlanId = ConfigurationManager.AppSettings["StripePlanId1"].ToString(); // Default plan is the contractor plan
                    switch (organization.PlanId) {
                        case 1:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId1"].ToString();
                            break;
                        case 2:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId2"].ToString();
                            break;
                        case 3:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId3"].ToString();
                            break;
                        case 4:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId4"].ToString();
                            break;
                    }

#if !DEBUG
                    try
                    {
                        // Create a Stripe customer object and save the customer id
                        var customerOptions = new CustomerCreateOptions
                        {
                            Email = user.EmailAddress
                        };
                        var customers = new CustomerService();
                        Stripe.Customer customer = customers.Create(customerOptions);
                        organization.StripeCustomerId = customer.Id;

                        // Subscribe the customer to the price and save the subscription id
                        var subscriptionOptions = new SubscriptionCreateOptions
                        {
                            Customer = customer.Id, // ex. cus_IDjvN9UsoFp2mk
                            Items = new List<SubscriptionItemOptions>
                            {
                                new SubscriptionItemOptions
                                {
                                    Price = stripePlanId // ex. price_1Hd5PvLI44a19MHX9w5EGD4r
                                }
                            },
                            TrialFromPlan = true
                        };
                        var subscriptions = new SubscriptionService();
                        Subscription subscription = subscriptions.Create(subscriptionOptions);

                        organization.StripeSubscriptionId = subscription.Id;

                        // Send Welcome Email
                        var apiKey = ConfigurationManager.AppSettings["SendGridApiKey"].ToString();
                        var client = new SendGridClient(apiKey);
                        var from = new EmailAddress("BRIZBEE <administrator@brizbee.com>");
                        var to = new EmailAddress(user.EmailAddress);
                        var msg = MailHelper.CreateSingleTemplateEmail(from, to, "d-8c48a9ad2ddd4d73b6e6c10307182f43", null);
                        var response = client.SendEmailAsync(msg);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning(ex.ToString());
                        throw;
                    }
#endif

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