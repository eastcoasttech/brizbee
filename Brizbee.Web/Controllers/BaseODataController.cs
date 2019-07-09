using Brizbee.Common.Models;
using Microsoft.AspNet.OData;
using System.Linq;

namespace Brizbee.Web.Controllers
{
    public class BaseODataController : ODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        public User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return db.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }
}