using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using static System.Net.WebRequestMethods;
using System.Runtime.ConstrainedExecution;
using System;
using System.Xml.Linq;

namespace netcore_sample_logging_aws_dynamodb.Logger
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly AmazonDynamoDBClient _dynamoDbClient;
        private readonly Table _logTable;

        //  RequestLogs table description , create it in AWS DynamoDB
        //  Id ,String (PK)
        //  Path ,String  
        //  RequestBody ,String  
        //  StatusCode ,Number  
        //  ElapsedMilliseconds ,Number
        //  
        //  create table command (aws cli)
        //  aws dynamodb create-table --table-name RequestLogs --attribute-definitions AttributeName = Id, AttributeType = S--key-schema AttributeName = Id, KeyType = HASH--billing-mode PAY_PER_REQUEST

        public RequestLoggingMiddleware(RequestDelegate requestDelegate, ILogger<RequestLoggingMiddleware> logger, AmazonDynamoDBClient dynamoDbClient)
        {
            _requestDelegate = requestDelegate;
            _logger = logger;
            _dynamoDbClient = dynamoDbClient;
            _logTable = Table.LoadTable(_dynamoDbClient, "RequestLogs"); // DynamoDB table name
        }

       

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var stopWatch = Stopwatch.StartNew();

            // Request logging
            httpContext.Request.EnableBuffering();

            var requestBody = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            httpContext.Request.Body.Position = 0;

            _logger.LogInformation("Incoming request: {Method} {Path} - Body: {Body}",
                httpContext.Request.Method, httpContext.Request.Path, requestBody
                );

            await _requestDelegate(httpContext);
            stopWatch.Stop();

            // Save log to DynamoDB
            var logEntry = new Document
            {
                ["Id"] = Guid.NewGuid().ToString(),
                ["Timestamp"] = DateTime.UtcNow.ToString("o"),
                ["Method"] = httpContext.Request.Method,
                ["Path"] =  httpContext.Request.Path.ToString(),
                ["RequestBody"] = requestBody,
                ["StatusCode"] = httpContext.Response.StatusCode,
                ["ElapsedMilliseconds"] = stopWatch.ElapsedMilliseconds
            };

            await _logTable.PutItemAsync(logEntry);

            // Response logging
            _logger.LogInformation("Outgoing response: {StatusCode} - Elapsed time: {ElapsedMilliseconds} ms",
                httpContext.Response.StatusCode, stopWatch.ElapsedMilliseconds
                );



        }
    }



}
