using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Api.Serialization.DTO
{
    public class OrganizationDTO
    {
        public DateTime CreatedAt { get; set; }
        public int Id { get; set; }
        public string MinutesFormat { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int PlanId { get; set; } // 1, 2, 3, or 4
    }
}
