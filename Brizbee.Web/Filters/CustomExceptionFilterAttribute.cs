//
//  CustomExceptionFilterAttribute.cs
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
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData;
using System.Data.Entity.Validation;
using System.Web.Http.Filters;

namespace Brizbee.Web.Filters
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