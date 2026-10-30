using Azure.Storage.Blobs;
using SkyDrop.Api.ServiceInterfaces;
using SkyDrop.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FileStorage") ?? throw new InvalidOperationException("FileStorage connection string is not configured.");
var containerName = builder.Configuration["FileStorage:ContainerName"] ?? throw new InvalidOperationException("FileStorage:ContainerName is not configured.");

// Add services to the container.
builder.Services.AddSingleton(new BlobContainerClient(connectionString, containerName));
builder.Services.AddSingleton<IStreamService, BlobStreamService>();
builder.Services.AddSingleton<IKeyGenerator, SimpleKeyGenerator>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
