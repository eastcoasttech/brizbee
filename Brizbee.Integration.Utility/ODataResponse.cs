﻿//
//  ODataResponse.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Integration Utility.
//
//  This program is free software: you can redistribute
//  it and/or modify it under the terms of the GNU General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will
//  be useful, but WITHOUT ANY WARRANTY; without even the implied
//  warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//  See the GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.
//  If not, see <https://www.gnu.org/licenses/>.
//

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Brizbee.Integration.Utility
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
