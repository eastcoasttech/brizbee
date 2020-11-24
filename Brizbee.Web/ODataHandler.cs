//
//  ODataHandler.cs
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

using Microsoft.AspNet.OData;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Brizbee.Web
{
    public class ODataHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken).ContinueWith(
              task =>
              {
                  var response = task.Result;

                  if (ResponseIsValid(response))
                  {
                      object responseObject;
                      response.TryGetContentValue(out responseObject);

                      if (responseObject is PageResult)
                      {
                          var renum = responseObject as IEnumerable<object>;
                          var robj = responseObject as PageResult;

                          if (robj.Count != null)
                          {
                              response = request.CreateResponse(HttpStatusCode.OK, new ODataMetadata<object>(renum, robj.Count));
                          }
                      }
                  }

                  return response;
              });
        }

        private bool ResponseIsValid(HttpResponseMessage response)
        {
            if (response == null || response.StatusCode != HttpStatusCode.OK || !(response.Content is ObjectContent)) return false;
            return true;
        }
    }
}