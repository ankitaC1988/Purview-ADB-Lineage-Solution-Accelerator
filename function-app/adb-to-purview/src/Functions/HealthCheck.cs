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
using Azure.Data.Tables;
using Newtonsoft.Json.Linq;

namespace AdbToPurview.Function
{
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly IHttpHelper _httpHelper;
        private IConfiguration _configuration;
        private const string SOLUTION_VERSION = "2.2.0";

        public HealthCheck(
                ILogger<HealthCheck> logger,
                IHttpHelper httpHelper,
                IConfiguration configuration)
        {
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
            string CHECK_TABLE_STORAGE_CONN_IS_SET = "FunctionStorage is not set or is not a valid storage account connection string.";
            string CHECK_TABLE_STORAGE_TABLE_EXISTS = "The Table Storage table does not exist, it may not have been instantiated yet.";
            string CHECK_PURVIEW_NAME_IS_SET = "The PurviewAccountName is not set.";

            try
            {
                string _storageConStr = _configuration["FunctionStorage"];
                var options = new TableClientOptions();
                options.Retry.MaxRetries = 3;
                TableServiceClient _serviceClient = new TableServiceClient(_storageConStr, options);
                // TODO This should not hard code OlEventConsolidation
                await foreach (var tbl in _serviceClient.QueryAsync(t => t.Name == "OlEventConsolodation"))
                {
                    CHECK_TABLE_STORAGE_TABLE_EXISTS = "true";
                }
                CHECK_TABLE_STORAGE_CONN_IS_SET = "true";
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to check all Table Storage related checks.");
            }

            // Check Purview
            try{
                CHECK_PURVIEW_NAME_IS_SET = _configuration["PurviewAccountName"] ?? CHECK_PURVIEW_NAME_IS_SET;

            }catch (Exception e)
            {
                _logger.LogWarning("Failed to check all Microsoft Purview related checks.");
            }

            try
            {
                // Send appropriate success response
                JObject healthStats = new JObject(
                    new JProperty("version", SOLUTION_VERSION),
                    new JProperty("tableStorageConnectionEstablished", CHECK_TABLE_STORAGE_CONN_IS_SET),
                    new JProperty("tableStorageTableExists", CHECK_TABLE_STORAGE_TABLE_EXISTS),
                    new JProperty("microsoftPurviewAccountName", CHECK_PURVIEW_NAME_IS_SET)

                );
                string responseString = healthStats.ToString();
                var response = await _httpHelper.CreateSuccessfulHttpResponse(req, responseString);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in HealthCheck function: {ex.Message}");
                return _httpHelper.CreateServerErrorHttpResponse(req);
            }
        }
    }
}
