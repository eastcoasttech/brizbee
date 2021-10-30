//
//  MidnightPunch.cs
//  BRIZBEE Alerts Function
//
//  Copyright (C) 2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Alerts Function.
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

using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace Brizbee.Functions.Alerts.Serialization
{
    public class MidnightPunch
    {
        [JsonProperty("user_name")]
        [JsonPropertyName("user_name")]
        public string User_Name { get; set; }

        [JsonProperty("punch_outAt")]
        [JsonPropertyName("punch_outAt")]
        public string Punch_OutAt { get; set; }

        [JsonProperty("punch_inAt")]
        [JsonPropertyName("punch_inAt")]
        public string Punch_InAt { get; set; }

        [JsonProperty("task_number")]
        [JsonPropertyName("task_number")]
        public string Task_Number { get; set; }

        [JsonProperty("task_name")]
        [JsonPropertyName("task_name")]
        public string Task_Name { get; set; }

        [JsonProperty("project_number")]
        [JsonPropertyName("project_number")]
        public string Project_Number { get; set; }

        [JsonProperty("project_name")]
        [JsonPropertyName("project_name")]
        public string Project_Name { get; set; }

        [JsonProperty("customer_number")]
        [JsonPropertyName("customer_number")]
        public string Customer_Number { get; set; }

        [JsonProperty("customer_name")]
        [JsonPropertyName("customer_name")]
        public string Customer_Name { get; set; }
    }
}
