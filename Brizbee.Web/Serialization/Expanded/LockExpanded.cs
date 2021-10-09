//
//  LockExpanded.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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

namespace Brizbee.Web.Serialization.Expanded
{
    public class LockExpanded
    {
        // Lock Details

        public int Lock_Id { get; set; }

        public DateTime Lock_CreatedAt { get; set; }

        public int Lock_OrganizationId { get; set; }

        public DateTime Lock_InAt { get; set; }

        public DateTime Lock_OutAt { get; set; }

        public DateTime? Lock_QuickBooksExportedAt { get; set; }

        public int Lock_PunchCount { get; set; }

        public int Lock_UserId { get; set; }

        public Guid Lock_Guid { get; set; }


        // User Details

        public int User_Id { get; set; }

        public string User_Name { get; set; }
    }
}