//
//  DownloadFailedException.cs
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

using System;
using System.Runtime.Serialization;

namespace Brizbee.Integration.Utility.Exceptions
{
    [Serializable]
    internal class DownloadFailedException : Exception
    {
        public DownloadFailedException()
        {
        }

        public DownloadFailedException(string message) : base(message)
        {
        }

        public DownloadFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DownloadFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
