using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Brizbee.Common.Exceptions
{
    public class DuplicateException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;
        public string StatusCodeString { get; set; }

        public DuplicateException(string message) : base(message)
        {

        }
    }
}
