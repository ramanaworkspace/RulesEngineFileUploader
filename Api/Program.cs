using Application.Interfaces;
using Infrastructure;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind Azure Blob settings
builder.Services.Configure<AzureStorageSettings>(builder.Configuration.GetSection("AzureBlobStorage"));
// Add DI for application interface -> infrastructure implementation
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
