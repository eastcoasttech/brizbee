//
//  TwilioController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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

using Brizbee.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    public class TwilioController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;
        private readonly string _brizbeeAuth;

        public TwilioController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;

            _brizbeeAuth = _configuration["TwilioAuthKeyForBrizbeeApi"];
        }

        // GET: api/Twilio/Code
        [HttpGet("api/Twilio/Code")]
        [AllowAnonymous]
        public IActionResult GetCode(string BrizbeeAuth = "", string Digits = "")
        {
            if (BrizbeeAuth != _brizbeeAuth) { new Exception("Not Authorized"); }

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather timeout='10' action='/api/Twilio/Pin?BrizbeeAuth={0}' method='GET'>", BrizbeeAuth);
            template += "<Say voice='Polly.Matthew'>";
            template += "Thank you for calling brizbee! Please enter your organization code and press the pound key.";
            template += "</Say>";
            template += "</Gather>";
            template += "</Response>";

            return new ContentResult
            {
                Content = template,
                ContentType = "application/xml",
                StatusCode = 200
            };
        }

        // GET: api/Twilio/Pin
        [HttpGet("api/Twilio/Pin")]
        [AllowAnonymous]
        public IActionResult GetPin(string BrizbeeAuth = "", string Digits = "")
        {
            if (BrizbeeAuth != _brizbeeAuth) { new Exception("Not Authorized"); }

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather timeout='10' action='/api/Twilio/Status?BrizbeeAuth={0}&amp;OrganizationCode={1}' method='GET'>", BrizbeeAuth, Digits);
            template += "<Say voice='Polly.Matthew'>";
            template += "Okay, please enter your PIN number and press the pound key.";
            template += "</Say>";
            template += "</Gather>";
            template += "</Response>";

            return new ContentResult
            {
                Content = template,
                ContentType = "application/xml",
                StatusCode = 200
            };
        }

        // GET: api/Twilio/Status
        [HttpGet("api/Twilio/Status")]
        [AllowAnonymous]
        public IActionResult GetStatus(string BrizbeeAuth = "", string OrganizationCode = "", string Digits = "", string From = "")
        {
            if (BrizbeeAuth != _brizbeeAuth) { new Exception("Not Authorized"); }

            var user = _context.Users
                .Include("Organization")
                .Where(u => u.Pin == Digits)
                .Where(u => u.Organization.Code == OrganizationCode)
                .Where(u => u.IsDeleted == false)
                .FirstOrDefault();

            if (user != null)
            {
                // Ensure that the user is allowed to use touch-tone clock.
                var allowedPhoneNumbers = user.AllowedPhoneNumbers.Split(',');
                if (!allowedPhoneNumbers.Contains("*") && !allowedPhoneNumbers.Contains(From))
                {
                    // Inform the user that they are denied.
                    string deniedTemplate = "<?xml version = '1.0'?>";
                    deniedTemplate += "<Response>";
                    deniedTemplate += "<Say voice='Polly.Matthew'>";
                    deniedTemplate += "I'm sorry, but you do not have permission to use the touch-tone clock.";
                    deniedTemplate += "</Say>";
                    deniedTemplate += "<Hangup/>";
                    deniedTemplate += "</Response>";

                    return new ContentResult
                    {
                        Content = deniedTemplate,
                        ContentType = "application/xml",
                        StatusCode = 200
                    };
                }

                // Continue punching in.
                var punch = _context.Punches
                    .Include("Task")
                    .Where(p => p.UserId == user.Id)
                    .Where(p => !p.OutAt.HasValue)
                    .OrderByDescending(p => p.InAt)
                    .FirstOrDefault();

                var message = "";

                if (punch != null)
                {
                    message = string.Format("Hello {0}! You are currently punched <emphasis>in</emphasis> to task, {1} - {2}, for the job, {3} - {4}, and customer, {5} - {6}. Please press 1 to punch in on another task or job. Or, press 2 to punch out.",
                        StripBadCharacters(user.Name),
                        punch.Task.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                        StripBadCharacters(punch.Task.Name),
                        punch.Task.Job.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                        StripBadCharacters(punch.Task.Job.Name),
                        punch.Task.Job.Customer.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                        StripBadCharacters(punch.Task.Job.Customer.Name));
                }
                else
                {
                    message = string.Format("Hello {0}! You are not punched <emphasis>in</emphasis> right now. Please press 1 to punch in.", user.Name);
                }

                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += string.Format("<Gather numDigits='1' timeout='5' action='/api/Twilio/Select?BrizbeeAuth={0}&amp;UserId={1}' method='GET'>", BrizbeeAuth, user.Id);
                template += "<Say voice='Polly.Matthew'>";
                template += message;
                template += "</Say>";
                template += "</Gather>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            else
            {
                // Redirect to Status
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='Polly.Matthew'>";
                template += "We could not find a user with the organization code and PIN number you provided.";
                template += "</Say>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/Code?BrizbeeAuth={0}", BrizbeeAuth);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
        }

        // GET: api/Twilio/Select
        [HttpGet("api/Twilio/Select")]
        [AllowAnonymous]
        public IActionResult GetSelect(string BrizbeeAuth = "", string UserId = "", string Digits = "")
        {
            if (BrizbeeAuth != _brizbeeAuth) { new Exception("Not Authorized"); }
            
            if (Digits.Equals("1"))
            {
                // Redirect to TaskNumber
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/TaskNumber?BrizbeeAuth={0}&amp;UserId={1}", BrizbeeAuth, UserId);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            else if (Digits.Equals("2"))
            {
                // Redirect to PunchOut
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/PunchOut?BrizbeeAuth={0}&amp;UserId={1}", BrizbeeAuth, UserId);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            else
            {
                // Redirect to Status
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='Polly.Matthew'>";
                template += "We could not find a user with the organization code and PIN number you provided.";
                template += "</Say>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/Code?BrizbeeAuth={0}", BrizbeeAuth);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
        }

        // GET: api/Twilio/TaskNumber
        [HttpGet("api/Twilio/TaskNumber")]
        [AllowAnonymous]
        public IActionResult GetTaskNumber(string BrizbeeAuth = "", string UserId = "", string Digits = "")
        {
            // Redirect to ConfirmTaskNumber
            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather timeout='10' action='/api/Twilio/ConfirmTaskNumber?BrizbeeAuth={0}&amp;UserId={1}' method='GET'>", BrizbeeAuth, UserId);
            template += "<Say voice='Polly.Matthew'>";
            template += "Please enter the task number that you want to punch in to and press the pound key.";
            template += "</Say>";
            template += "</Gather>";
            template += "</Response>";

            return new ContentResult
            {
                Content = template,
                ContentType = "application/xml",
                StatusCode = 200
            };
        }

        // GET: api/Twilio/ConfirmTaskNumber
        [HttpGet("api/Twilio/ConfirmTaskNumber")]
        [AllowAnonymous]
        public IActionResult GetConfirmTaskNumber(string BrizbeeAuth = "", string UserId = "", string Digits = "")
        {
            if (BrizbeeAuth != _brizbeeAuth) { new Exception("Not Authorized"); }

            var user = _context.Users.Find(int.Parse(UserId));

            var customerIds = _context.Customers.Where(c => c.OrganizationId == user.OrganizationId).Select(c => c.Id);
            var jobIds = _context.Jobs.Where(j => customerIds.Contains(j.CustomerId)).Select(j => j.Id);
            var task = _context.Tasks
                .Where(t => jobIds.Contains(t.JobId))
                .Where(t => t.Number == Digits)
                .FirstOrDefault();

            if (task != null)
            {
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += string.Format("<Gather numDigits='1' timeout='5' action='/api/Twilio/PunchIn?BrizbeeAuth={0}&amp;UserId={1}&amp;TaskId={2}' method='GET'>", BrizbeeAuth, UserId, task.Id);
                template += "<Say voice='Polly.Matthew'>";
                template += string.Format("Are you sure you want to punch <emphasis>in</emphasis> to task, {0} - {1}, for the job, {2} - {3}, and customer, {4} - {5}? Press 1 if yes. Press 2 to punch in to another task.",
                    task.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                    StripBadCharacters(task.Name),
                    task.Job.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                    StripBadCharacters(task.Job.Name),
                    task.Job.Customer.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                    StripBadCharacters(task.Job.Customer.Name));
                template += "</Say>";
                template += "</Gather>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            else
            {
                // Redirect to TaskNumber
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='Polly.Matthew'>";
                template += "We could not find a task with the number you provided.";
                template += "</Say>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/TaskNumber?BrizbeeAuth={0}&amp;TaskId={1}", BrizbeeAuth, UserId);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
        }

        // GET: api/Twilio/PunchIn
        [HttpGet("api/Twilio/PunchIn")]
        [AllowAnonymous]
        public IActionResult GetPunchIn(string BrizbeeAuth = "", string UserId = "", string TaskId = "", string Digits = "", string From = "")
        {
            if (BrizbeeAuth != _brizbeeAuth) { new Exception("Not Authorized"); }

            var user = _context.Users.Find(int.Parse(UserId));
            var timezone = user.TimeZone;
            var task = _context.Tasks.Find(int.Parse(TaskId));

            if (Digits.Equals("1"))
            {
                // Punch in to the requested task.
                new PunchRepository(_context).PunchIn(task.Id, user, From, timezone, sourceHardware: "Phone", sourcePhoneNumber: From);

                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='Polly.Matthew'>";
                template += "Okay, you are now punched <emphasis>in</emphasis>. Goodbye.";
                template += "</Say>";
                template += "<Hangup/>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            else if (Digits.Equals("2"))
            {
                // Redirect to TaskNumber
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/TaskNumber?BrizbeeAuth={0}&amp;UserId={1}", BrizbeeAuth, UserId);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }
            else
            {
                // Redirect to TaskNumber
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/TaskNumber?BrizbeeAuth={0}&amp;UserId={1}", BrizbeeAuth, UserId);
                template += "</Redirect>";
                template += "</Response>";

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/xml",
                    StatusCode = 200
                };
            }

        }

        // GET: api/Twilio/PunchOut
        [HttpGet("api/Twilio/PunchOut")]
        [AllowAnonymous]
        public IActionResult GetPunchOut(string BrizbeeAuth = "", string UserId = "", string Digits = "", string From = "")
        {
            var user = _context.Users.Find(int.Parse(UserId));
            var timezone = user.TimeZone;

            // Punch out of the current task.
            new PunchRepository(_context).PunchOut(user, From, timezone, sourceHardware: "Phone", sourcePhoneNumber: From);
            
            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += "<Say voice='Polly.Matthew'>";
            template += "Okay, you are now punched <emphasis>out</emphasis>. Goodbye.";
            template += "</Say>";
            template += "<Hangup/>";
            template += "</Response>";

            return new ContentResult
            {
                Content = template,
                ContentType = "application/xml",
                StatusCode = 200
            };
        }

        private string StripBadCharacters(string dirty)
        {
            return dirty.Replace("&", " and ");
        }
    }
}