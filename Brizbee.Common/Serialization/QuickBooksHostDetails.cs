using System;
using System.Collections.Generic;
using System.Text;

namespace Brizbee.Common.Serialization
{
    public class QuickBooksHostDetails
    {
        public string QBProductName { get; set; }

        public string QBMajorVersion { get; set; }

        public string QBMinorVersion { get; set; }

        public string QBCountry { get; set; }

        public string QBSupportedQBXMLVersions { get; set; }
    }
}
