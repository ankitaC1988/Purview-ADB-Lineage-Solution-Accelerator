// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Function.Domain.Helpers;
using Function.Domain.Services;


namespace AdbToPurview.Function
{
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly IHttpHelper _httpHelper;
        private IConfiguration _configuration;

        public HealthCheck(
                ILogger<HealthCheck> logger,
                IHttpHelper httpHelper,
                IConfiguration configuration){
            _logger = logger;
            _httpHelper = httpHelper;
            _configuration = configuration;
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "get", 
                Route = "healthcheck"
            )] HttpRequestData req)
        {
            try {
                // Send appropriate success response
                string responseString = "{\"version\":\"2.2.0\"}";
                var response = await _httpHelper.CreateSuccessfulHttpResponse(req, responseString);
                return response;
            }
            catch(Exception ex){
                _logger.LogError(ex, $"Error in Healthcheck function: {ex.Message}");
                return _httpHelper.CreateServerErrorHttpResponse(req);
            }
        }
    }
}