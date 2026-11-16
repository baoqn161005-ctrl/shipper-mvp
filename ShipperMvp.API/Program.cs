using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using ShipperMvp.Infrastructure;
using ShipperMvp.Application.Orders;
using ShipperMvp.Application.Reviews;
using ShipperMvp.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The API is reachable only on the private Docker network behind Caddy.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var jwt = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwt["Secret"];
var jwtIssuer = jwt["Issuer"];
var jwtAudience = jwt["Audience"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.StartsWith("CHANGE_ME", StringComparison.Ordinal) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("Configure Jwt:Secret with a private secret of at least 32 UTF-8 bytes (use user-secrets or Jwt__Secret environment variable).");
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Configure Jwt:Issuer and Jwt:Audience in appsettings.json or environment variables.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// Infrastructure Layer (DbContext, Repositories, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();
app.UseForwardedHeaders();
app.UseMiddleware<ApiExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

