using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SendgridFaker.PublicModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var authority = "https://localhost:7233/";
var audience = "https://localhost:7233/";
var issuer = "https://localhost:7233/";
var signingKey = "K2A7JfE5h4dSD2ELyOS2EFtmPqvyw9hp7rlSBfi2dAclJj60ohG7HOWLDJvIJYSAw5qvvcCZrQHn4Zhc";

var builder = WebApplication.CreateBuilder(args);

IdentityModelEventSource.ShowPII = true;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.Configuration = new OpenIdConnectConfiguration();

        o.RequireHttpsMetadata = false;
        o.Authority = authority;
        o.Audience = audience;
        o.IncludeErrorDetails = true;
        o.SaveToken = false;

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),

            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            ValidateActor = false,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "You api title", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Name = "Bearer",
                Scheme = "oauth2",
                In = ParameterLocation.Header,
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
            },
            new List<string>()
        }
    });

    //c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapPost("/v3/mail/send", [Authorize] async (SendGridMessage sendGridMessage) =>
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

app.MapGet("/token", [AllowAnonymous] async () =>
{
    List<Claim> claims = new List<Claim>() {
            new Claim (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim (JwtRegisteredClaimNames.Email, "a@a.com"),
            new Claim (JwtRegisteredClaimNames.Sub, "1")
        };

    JwtSecurityToken token = new TokenBuilder()
        .AddAudience(audience)
        .AddIssuer(issuer)
        .AddExpiry(60)
        .AddKey(signingKey)
        .AddClaims(claims)
        .Build();

    return "Bearer " + new JwtSecurityTokenHandler().WriteToken(token);
});

app.Run();