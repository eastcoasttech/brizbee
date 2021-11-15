//
//  Program.cs
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

using Brizbee.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = (context) => {
                            StringValues values;

                            if (!context.Request.Query.TryGetValue("access_token", out values))
                            {
                                return Task.CompletedTask;
                            }

                            if (values.Count > 1)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Fail(
                                    "Only one 'access_token' query string parameter can be defined. " +
                                    $"However, {values.Count:N0} were included in the request."
                                );

                                return Task.CompletedTask;
                            }

                            var token = values.Single();

                            if (string.IsNullOrWhiteSpace(token))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Fail(
                                    "The 'access_token' query string parameter was defined, " +
                                    "but a value to represent the token was not included."
                                );

                                return Task.CompletedTask;
                            }

                            context.Token = token;

                            return Task.CompletedTask;
                        }
                    };
                });

builder.Services.AddDbContext<SqlContext>(options =>
                options.UseSqlServer(builder.Configuration["ConnectionStrings:SqlContext"]));

builder.Services.AddControllers()
                .AddOData(options =>
                {
                    options
                        .Select()
                        .Expand()
                        .OrderBy()
                        .Filter()
                        .Count()
                        .SetMaxTop(null)
                        .AddRouteComponents("odata", BrizbeeEntityDataModel.GetEntityDataModel());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

builder.Services.AddODataQueryFilter();

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AuthorizationHeaderOperation>();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Brizbee.Api", Version = "v1" });
});

var app = builder.Build();

app.UseRouting();

app.UseCors(builder =>
{
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("*");
});

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
