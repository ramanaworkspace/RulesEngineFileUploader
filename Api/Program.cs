using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Infrastructure;
using Infrastructure.Services;
using Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureStorageSettings>(builder.Configuration.GetSection("AzureStorage"));

// Add DI for application interface -> infrastructure implementation
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Authentication / Authorization (Azure AD)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

// Swagger with OAuth2 (as earlier)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { /* same as earlier */ });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rules Engine Service v1");
    c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
    c.OAuthUsePkce();
});

app.MapControllers();
app.Run();
