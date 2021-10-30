//
//  User.cs
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

namespace Brizbee.Functions.Alerts.Serialization
{
    public class User
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string EmailAddress { get; set; }
    }
}
