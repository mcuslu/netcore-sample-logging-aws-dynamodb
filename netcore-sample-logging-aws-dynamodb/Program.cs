using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using netcore_sample_logging_aws_dynamodb.Logger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// AWS Credentials
var awsCredentials = new BasicAWSCredentials("YOUR_IAM_ACCESS_KEY", "YOUR_IAM_SECRET_KEY"); // This authentication method for development sample,
// for production , use secure authentication methods, aws profile, IAM role etc.
var dynamoDbConfig = new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1 }; // Your AWS Region
builder.Services.AddSingleton<AmazonDynamoDBClient>(sp => new AmazonDynamoDBClient(awsCredentials, dynamoDbConfig));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.UseRequestLogging();

app.Run();
