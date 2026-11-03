using Azure.Storage.Blobs;
using SkyDrop.Api.ServiceInterfaces;
using SkyDrop.Api.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

var blobConnectionString = builder.Configuration.GetConnectionString("FileStorage") ?? throw new InvalidOperationException("FileStorage connection string is not configured.");
var blobContainerName = builder.Configuration["FileStorage:ContainerName"] ?? throw new InvalidOperationException("FileStorage:ContainerName is not configured.");

var cosmosConnectionString = builder.Configuration.GetConnectionString("Cosmos") ?? throw new InvalidOperationException("Cosmos connection string is not configured.");
var cosmosDatabaseName = builder.Configuration["Cosmos:DatabaseName"] ?? throw new InvalidOperationException("Cosmos:DatabaseName is not configured.");

// Add services to the container.
builder.Services.AddSingleton(new BlobContainerClient(blobConnectionString, blobContainerName));
builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString, new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
}).GetDatabase(cosmosDatabaseName));

builder.Services.AddSingleton<IFileUploader, FileUploader>();
builder.Services.AddSingleton<IFileDownloader, FileDownloader>();
builder.Services.AddSingleton<IFileDb, BlobFileDb>();
builder.Services.AddSingleton<IKeyGenerator, SimpleKeyGenerator>();

builder.Services.Configure<MonthlyUsageLimits>(o => {
    o.DownloadedBytes = builder.Configuration.GetValue<long?>("MonthlyUsageLimits:DownloadedBytes") ?? throw new InvalidOperationException("MonthlyUsageLimits:DownloadedBytes is not configured.");
    o.UploadedBytes = builder.Configuration.GetValue<long?>("MonthlyUsageLimits:UploadedBytes") ?? throw new InvalidOperationException("MonthlyUsageLimits:UploadedBytes is not configured.");
});
builder.Services.AddSingleton<IUsageDb, CosmosUsageDb>();
builder.Services.AddSingleton<IUsageLimiter, MonthlyUsageLimiter>();

builder.Services.AddSingleton<IFileMetadataDb, CosmosFileMetadataDb>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.WithOrigins("https://skydrop.skybounce.io").WithHeaders("Content-Type", "X-File-Name").WithMethods("GET", "POST")));

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
