

using Brizbee.Common.Exceptions;
using Microsoft.OData;
using System.Web.Http.Filters;
using System.Web.OData.Extensions;

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
        else
        {

            base.OnException(context);
        }
    }
}
}