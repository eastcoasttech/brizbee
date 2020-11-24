//
//  OrganizationDTO.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
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
