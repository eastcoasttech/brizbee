//
//  AuditRecord.cs
//  BRIZBEE Common Library
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Common Library.
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

namespace Brizbee.Core.Serialization.Records
{
    public class AuditRecord
    {
        public DateTime Audit_CreatedAt { get; set; }

        public int Audit_Id { get; set; }

        public int Audit_OrganizationId { get; set; }

        public int Audit_UserId { get; set; }

        public int Audit_ObjectId { get; set; }

        public string Audit_Action { get; set; }

        public string Audit_Before { get; set; }

        public string Audit_After { get; set; }

        public string Audit_ObjectType { get; set; }

        public int User_Id { get; set; }

        public string User_Name { get; set; }
    }
}
