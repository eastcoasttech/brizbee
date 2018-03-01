using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Brizbee.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.NotFound;
        public string StatusCodeString { get; set; }

        public NotFoundException(string message) : base(message)
        {
        }
    }
}
