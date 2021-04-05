using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Brizbee.Dashboard
{
    public class ODataSingleResponse<T> where T : class
    {
        public T Value { get; set; }
    }
}
