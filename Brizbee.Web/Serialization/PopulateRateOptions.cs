using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Serialization
{
    public class PopulateRateOptions
    {
        public PopulateRateOption[] Options { get; set; }

        public DateTime InAt { get; set; }

        public DateTime OutAt { get; set; }
    }
}