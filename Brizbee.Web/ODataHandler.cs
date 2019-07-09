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