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
            Trace.TraceInformation(BrizbeeAuth);
            Trace.TraceInformation(Digits);

            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather numDigits='4' action='/api/Twilio/Pin?BrizbeeAuth={0}' method='GET'>", BrizbeeAuth);
            template += "<Say voice='woman'>";
            template += "Thank you for calling brizbee! Please enter your organization code now.";
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
            Trace.TraceInformation(BrizbeeAuth);
            Trace.TraceInformation(Digits);

            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            string template = "<?xml version = '1.0'?>";
            template += "<Response>";
            template += string.Format("<Gather numDigits='4' action='/api/Twilio/Select?BrizbeeAuth={0}&amp;OrganizationCode={1}' method='GET'>", BrizbeeAuth, Digits);
            template += "<Say voice='woman'>";
            template += "Okay, please enter your pin number now.";
            template += "</Say>";
            template += "</Gather>";
            template += "</Response>";
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            return response;
        }

        // GET: api/Twilio/Select
        [HttpGet]
        [AllowAnonymous]
        [Route("api/Twilio/Select")]
        public HttpResponseMessage GetSelect(string BrizbeeAuth = "", string OrganizationCode = "", string Digits = "")
        {
            Trace.TraceInformation(BrizbeeAuth);
            Trace.TraceInformation(OrganizationCode);
            Trace.TraceInformation(Digits);
            if (BrizbeeAuth != brizbeeAuth) { new Exception("Not Authorized"); }

            var user = db.Users
                .Include("Organization")
                .Where(u => u.Pin == Digits)
                .Where(u => u.Organization.Code == OrganizationCode)
                .FirstOrDefault();

            if (user != null)
            {
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='woman'>";
                template += "We have a user id.";
                template += "</Say>";
                template += "</Response>";
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }
            else
            {
                string template = "<?xml version = '1.0'?>";
                template += "<Response>";
                template += "<Say voice='woman'>";
                template += "We do not have a user.";
                template += "</Say>";
                template += "</Response>";
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }



            //if (user != null)
            //{
            //    var punch = db.Punches.Where(p => p.UserId == user.Id)
            //        .Where(p => p.OutAt == null)
            //        .OrderBy(p => p.InAt).Reverse()
            //        .FirstOrDefault();

            //    var message = "";

            //    if (punch != null)
            //    {
            //        message = "Hello, " + user.Name + ", you are currently punched in on task, " + punch.Task.Number + " " + punch.Task.Name + ", for job, " + punch.Task.Job.Name + " " + punch.Task.Job.Name + ", for customer, " + punch.Task.Job.Customer.Number + " " + punch.Task.Job.Customer.Name + ". Please press 1 to punch in on another task or job. Or, press 2 to punch out.";
            //    }
            //    else
            //    {
            //        message = string.Format("Hello {0}, please press 1 to punch in.", user.Name);
            //    }

            //    string template = "<?xml version = '1.0'?>";
            //    template += "<Response>";
            //    template += string.Format("<Gather numDigits='1' action='/api/Twilio/Select?OrganizationCode={0}&amp;UserPin={1}&amp;UserId={2}' method='GET'>", OrganizationCode, Digits, user.Id);
            //    template += "<Say voice='woman'>";
            //    template += message;
            //    template += "</Say>";
            //    template += "</Gather>";
            //    template += "</Response>";
            //    var response = this.Request.CreateResponse(HttpStatusCode.OK);
            //    response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            //    return response;
            //}
            //else
            //{
            //    string template = "<?xml version = '1.0'?>";
            //    template += "<Response>";
            //    template += "<Say voice='woman'>";
            //    template += "Sorry, there was an error. Please try again in a few minutes.";
            //    template += "</Say>";
            //    template += "</Response>";
            //    var response = this.Request.CreateResponse(HttpStatusCode.OK);
            //    response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/xml");
            //    return response;
            //}
        }
    }
}