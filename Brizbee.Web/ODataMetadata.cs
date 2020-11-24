//
//  ODataMetadata.cs
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

using System.Collections.Generic;

namespace Brizbee.Web
{
    public class ODataMetadata<T> where T : class
    {
        private readonly long? _count;
        private IEnumerable<T> _result;

        public ODataMetadata(IEnumerable<T> result, long? count)
        {
            _count = count;
            _result = result;
        }

        public IEnumerable<T> Results
        {
            get { return _result; }
        }

        public long? Count
        {
            get { return _count; }
        }
    }
}