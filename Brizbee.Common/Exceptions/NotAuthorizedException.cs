using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Brizbee.Common.Exceptions
{
    public class NotAuthorizedException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Unauthorized;
        public string StatusCodeString { get; set; }

        public NotAuthorizedException(string message) : base(message)
        {

        }
    }
}
