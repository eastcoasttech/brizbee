//
//  SqlHelper.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2022 East Coast Technology Services, LLC
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

namespace Brizbee.Api.Sql;

public class SqlHelper
{
    public static string SqlFromFile(string category, string queryName)
    {
        Stream? stream = null;

        try
        {
            var assembly = typeof(SqlHelper).Assembly;

            var resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith($"{category.Replace(" ", "_")}.{queryName}.sql"));

            if (resourceName == null)
                return string.Empty;

            stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                return string.Empty;

            using var reader = new StreamReader(stream);

            stream.Position = 0;
            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            return string.Empty;
        }
        finally
        {
            if (stream != null)
                stream?.Dispose();
        }
    }
}
