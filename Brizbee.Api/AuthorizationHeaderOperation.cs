﻿//
//  AuthorizationHeaderOperation.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2024 East Coast Technology Services, LLC
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

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Brizbee.Api;

public abstract class AuthorizationHeaderOperation : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Validate the operation.
        ArgumentNullException.ThrowIfNull(operation);

        operation.Parameters ??= new List<OpenApiParameter>();

        // Configure the Authorization header.
        operation.Parameters.Add(new OpenApiParameter()
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Description = "JWT",
            Required = false
        });
    }
}
