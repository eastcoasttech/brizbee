//
//  MockHttpClientBunitHelpers.cs
//  BRIZBEE Dashboard
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Dashboard.
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

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Brizbee.Dashboard.Tests
{
    public static class MockHttpClientBunitHelpers
    {
        public static MockHttpMessageHandler AddMockHttpClient(this TestServiceProvider services)
        {
            var mockHttpHandler = new MockHttpMessageHandler();
            var httpClient = mockHttpHandler.ToHttpClient();
            httpClient.BaseAddress = new Uri("http://localhost");
            services.AddSingleton(httpClient);
            return mockHttpHandler;
        }

        public static MockedRequest RespondJson<T>(this MockedRequest request, T content)
        {
            request.Respond(req =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(JsonSerializer.Serialize(content));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return response;
            });
            return request;
        }

        public static MockedRequest RespondJson<T>(this MockedRequest request, Func<T> contentProvider)
        {
            request.Respond(req =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(JsonSerializer.Serialize(contentProvider()));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return response;
            });
            return request;
        }
    }
}
