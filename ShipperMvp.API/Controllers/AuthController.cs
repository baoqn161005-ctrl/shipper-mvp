using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShipperMvp.Application.Common.Interfaces;
using ShipperMvp.Domain.Entities;
using ShipperMvp.Domain.Enums;

namespace ShipperMvp.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IApplicationDbContext db, IConfiguration configuration) : ControllerBase
{
    private const string DefaultVehicleType = "Xe máy";
    private const string PlaceholderLicensePlate = "CHUA_CAP_NHAT";

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var fullName = request.FullName.Trim();
        var phoneNumber = request.PhoneNumber.Trim();
        var role = NormalizeRole(request.Role);
        if (role is null)
            return BadRequest(new { message = "Role phải là Customer hoặc Shipper." });

        if (phoneNumber.Length is < 9 or > 15)
            return BadRequest(new { message = "Số điện thoại phải có từ 9 đến 15 ký tự." });

        if (await db.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, ct))
            return Conflict(new { message = "Số điện thoại đã được đăng ký." });

        var user = new User
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (role == UserRole.Shipper)
        {
            var vehicleType = NormalizeVehicleType(request.VehicleType);
            if (vehicleType is null)
                return BadRequest(new { message = "VehicleType không hợp lệ." });

            user.Driver = new Driver
            {
                User = user,
                LicensePlate = string.IsNullOrWhiteSpace(request.LicensePlate)
                    ? PlaceholderLicensePlate
                    : request.LicensePlate.Trim(),
                VehicleType = vehicleType,
                IsOnline = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique phone index also protects concurrent registration attempts.
            return Conflict(new { message = "Số điện thoại đã được đăng ký." });
        }

        return StatusCode(StatusCodes.Status201Created, CreateAuthResponse(user));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var phoneNumber = request.PhoneNumber.Trim();
        var user = await db.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Số điện thoại hoặc mật khẩu không đúng." });

        return Ok(CreateAuthResponse(user));
    }

    private AuthResponse CreateAuthResponse(User user) => new(
        Token: CreateToken(user),
        UserId: user.Id,
        FullName: user.FullName,
        Role: user.Role,
        PhoneNumber: user.PhoneNumber);

    private string CreateToken(User user)
    {
        var jwt = configuration.GetSection("Jwt");
        var secret = jwt["Secret"];
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32 ||
            string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Cấu hình Jwt:Secret (ít nhất 32 byte), Jwt:Issuer và Jwt:Audience trước khi phát hành token.");
        }

        var expiryMinutes = int.TryParse(jwt["ExpirationMinutes"], out var configuredMinutes) && configuredMinutes > 0
            ? configuredMinutes
            : 60;
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("userId", user.Id.ToString()),
            new Claim("role", user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string? NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "customer" => UserRole.Customer,
        "shipper" => UserRole.Shipper,
        _ => null
    };

    private static string? NormalizeVehicleType(string? vehicleType)
    {
        if (string.IsNullOrWhiteSpace(vehicleType))
            return DefaultVehicleType;

        var value = vehicleType.Trim();
        return value is "Xe máy" or "Xe ba gác" or "Xe tải nhỏ" ? value : null;
    }
}

public sealed record RegisterRequest(
    string FullName,
    string PhoneNumber,
    string Password,
    string Role,
    string? LicensePlate = null,
    string? VehicleType = null);

public sealed record LoginRequest(string PhoneNumber, string Password);

public sealed record AuthResponse(string Token, int UserId, string FullName, string Role, string PhoneNumber);
