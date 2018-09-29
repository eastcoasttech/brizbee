using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Brizbee.Controllers
{
    public class TwilioController : ApiController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private string brizbeeAuth = "78BcpK23z";

        // GET: api/Twilio/Code
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/Code")]
        public HttpResponseMessage GetCode(string BrizbeeAuth = "", string Digits = "")
        {
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather timeout='10' action='/api/Twilio/Pin?BrizbeeAuth={0}' method='GET'>", BrizbeeAuth);
            template += "<Say voice='Polly.Matthew'>";
            template += "Thank you for calling brizbee! Please enter your organization code and press the pound key.";
            template += "</Say>";
            template += "</Gather>";
            template += "</Response>";
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            return response;
        }

        // GET: api/Twilio/Pin
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/Pin")]
        public HttpResponseMessage GetPin(string BrizbeeAuth = "", string Digits = "")
        {
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather timeout='10' action='/api/Twilio/Status?BrizbeeAuth={0}&amp;OrganizationCode={1}' method='GET'>", BrizbeeAuth, Digits);
            template += "<Say voice='Polly.Matthew'>";
            template += "Okay, please enter your PIN number and press the pound key.";
            template += "</Say>";
            template += "</Gather>";
            template += "</Response>";
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            return response;
        }

        // GET: api/Twilio/Status
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/Status")]
        public HttpResponseMessage GetStatus(string BrizbeeAuth = "", string OrganizationCode = "", string Digits = "")
        {
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            var user = db.Users
                .Include("Organization")
                .Where(u => u.Pin == Digits)
                .Where(u => u.Organization.Code == OrganizationCode)
                .FirstOrDefault();

            if (user != null)
            {
                var punch = db.Punches
                    .Include("Task")
                    .Where(p => p.UserId == user.Id)
                    .Where(p => !p.OutAt.HasValue)
                    .OrderByDescending(p => p.InAt)
                    .FirstOrDefault();

                var message = "";

                if (punch != null)
                {
                    message = string.Format("Hello {0}! You are currently punched <emphasis>in</emphasis> to task, {1} - {2}, for the job, {3} - {4}, and customer, {5} - {6}. Please press 1 to punch in on another task or job. Or, press 2 to punch out.",
                        user.Name,
                        punch.Task.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                        punch.Task.Name,
                        punch.Task.Job.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                        punch.Task.Job.Name,
                        punch.Task.Job.Customer.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                        punch.Task.Job.Customer.Name);

                    //message = "Hello, " + user.Name + "! You are currently punched <emphasis>in</emphasis> to task, " + punch.Task.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' ') + " - " + punch.Task.Name + " for the job " + punch.Task.Job.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' ') + " - " + punch.Task.Job.Name + " and customer " + punch.Task.Job.Customer.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' ') + " - " + punch.Task.Job.Customer.Name + ". Please press 1 to punch in on another task or job. Or, press 2 to punch out.";
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }
        }

        // GET: api/Twilio/Select
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/Select")]
        public HttpResponseMessage GetSelect(string BrizbeeAuth = "", string UserId = "", string Digits = "")
        {
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }
            
            if (Digits.Equals("1"))
            {
                // Redirect to TaskNumber
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Redirect method='GET'>";
                template += string.Format("/api/Twilio/TaskNumber?BrizbeeAuth={0}&amp;UserId={1}", BrizbeeAuth, UserId);
                template += "</Redirect>";
                template += "</Response>";
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }
        }

        // GET: api/Twilio/TaskNumber
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/TaskNumber")]
        public HttpResponseMessage GetTaskNumber(string BrizbeeAuth = "", string UserId = "", string Digits = "")
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
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            return response;
        }

        // GET: api/Twilio/ConfirmTaskNumber
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/ConfirmTaskNumber")]
        public HttpResponseMessage GetConfirmTaskNumber(string BrizbeeAuth = "", string UserId = "", string Digits = "")
        {
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }
            
            var task = db.Tasks
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
                    task.Name,
                    task.Job.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                    task.Job.Name,
                    task.Job.Customer.Number.ToString().Aggregate(string.Empty, (c, i) => c + i + ' '),
                    task.Job.Customer.Name);
                template += "</Say>";
                template += "</Gather>";
                template += "</Response>";
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }
        }

        // GET: api/Twilio/PunchIn
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/PunchIn")]
        public HttpResponseMessage GetPunchIn(string BrizbeeAuth = "", string UserId = "", string TaskId = "", string Digits = "", string From = "")
        {
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            var user = db.Users.Find(int.Parse(UserId));

            if (Digits.Equals("1"))
            {
                var existing = db.Punches
                    .Where(p => p.UserId == user.Id)
                    .Where(p => !p.OutAt.HasValue)
                    .OrderByDescending(p => p.InAt)
                    .FirstOrDefault();

                // Punch out the user on any existing tasks
                if (existing != null)
                {
                    existing.OutAt = DateTime.Now;
                    existing.SourceForOutAt = From;
                }

                // Save the punch and hang up
                var task = db.Tasks.Find(int.Parse(TaskId));
                var punch = new Punch()
                {
                    CreatedAt = DateTime.Now,
                    Guid = Guid.NewGuid(),
                    InAt = DateTime.Now,
                    SourceForInAt = From,
                    TaskId = task.Id,
                    UserId = user.Id
                };
                db.Punches.Add(punch);
                db.SaveChanges();
                
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='Polly.Matthew'>";
                template += "Okay, you are now punched <emphasis>in</emphasis>. Goodbye.";
                template += "</Say>";
                template += "<Hangup/>";
                template += "</Response>";
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
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
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }

        }

        // GET: api/Twilio/PunchOut
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/PunchOut")]
        public HttpResponseMessage GetPunchOut(string BrizbeeAuth = "", string UserId = "", string Digits = "", string From = "")
        {
            var user = db.Users.Find(int.Parse(UserId));
            var punch = db.Punches.Where(p => p.UserId == user.Id)
                .Where(p => !p.OutAt.HasValue)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();
            punch.OutAt = DateTime.Now;
            punch.SourceForOutAt = From;
            db.SaveChanges();

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += "<Say voice='Polly.Matthew'>";
            template += "Okay, you are now punched <emphasis>out</emphasis>. Goodbye.";
            template += "</Say>";
            template += "<Hangup/>";
            template += "</Response>";
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            return response;
        }
    }
}