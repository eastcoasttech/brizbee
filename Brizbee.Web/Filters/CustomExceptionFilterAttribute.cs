using Brizbee.Common.Exceptions;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData;
using System.Data.Entity.Validation;
using System.Web.Http.Filters;

namespace Brizbee.Filters
{
    public class CustomExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is NotAuthorizedException)
            {
                var e = (NotAuthorizedException)context.Exception;

                var response = context.Request.CreateErrorResponse(e.StatusCode, new ODataError
                {
                    ErrorCode = e.StatusCodeString,
                    Message = e.Message
                });
                context.Response = response;
            }
            else if (context.Exception is NotFoundException)
            {
                var e = (NotFoundException)context.Exception;

                var response = context.Request.CreateErrorResponse(e.StatusCode, new ODataError
                {
                    ErrorCode = e.StatusCodeString,
                    Message = e.Message
                });
                context.Response = response;
            }
            else if (context.Exception is StripeException)
            {
                var e = (StripeException)context.Exception;

                var response = context.Request.CreateErrorResponse(e.StatusCode, new ODataError
                {
                    ErrorCode = e.StatusCodeString,
                    Message = e.Message
                });
                context.Response = response;
            }
            else if (context.Exception is DbEntityValidationException)
            {
                var e = (DbEntityValidationException)context.Exception;
                var message = "";

                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0}: {1}", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                var response = context.Request.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, new ODataError
                {
                    ErrorCode = System.Net.HttpStatusCode.BadRequest.ToString(),
                    Message = message
                });
                context.Response = response;
            }
            else
            {
                //base.OnException(context);
                
                var e = context.Exception;

                var response = context.Request.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, new ODataError
                {
                    ErrorCode = "Bad Request",
                    Message = e.Message
                });
                context.Response = response;
            }
        }
    }
}