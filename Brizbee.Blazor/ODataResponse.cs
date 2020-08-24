using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Blazor
{
    public class ODataResponse<T> where T : class
    {
        private readonly long? _count;
        private IEnumerable<T> _value;

        public ODataResponse(IEnumerable<T> value, long? count)
        {
            _count = count;
            _value = value;
        }

        public IEnumerable<T> Value
        {
            get { return _value; }
        }

        public long? Count
        {
            get { return _count; }
        }
    }
}
