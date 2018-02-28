﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brizbee.QuickBooksConnector
{
    public class ODataResponse<T>
    {
        [JsonProperty("value")]
        public List<T> Value { get; set; }

        [JsonProperty("@odata.count")]
        public int Count { get; set; }

        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        [JsonProperty("@odata.error")]
        public string Error { get; set; }
    }
}
