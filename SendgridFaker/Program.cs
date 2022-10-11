using Azure.Storage.Blobs;
using Microsoft.OpenApi.Models;
using SendgridFaker.PublicModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("/v3/mail/send", async (SendGridMessage sendGridMessage) =>
{
    string blobConnString = app.Configuration["BlobConnString"];
    string blobContainerName = app.Configuration["BlobContainerName"];
    string blobName = $"{DateTime.UtcNow.ToString("s")}_{Guid.NewGuid()}";

    var container = new BlobContainerClient(blobConnString, blobContainerName);
    var blobClient = container.GetBlobClient(blobName);

    using var ms = new MemoryStream();
    StreamWriter writer = new(ms);
    writer.Write(sendGridMessage.ToString());
    writer.Flush();
    ms.Position = 0;
    await blobClient.UploadAsync(ms);
})
.WithName("SendEmailMessageV3");

app.Run();