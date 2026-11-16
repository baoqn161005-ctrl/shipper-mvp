using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var baseUrl = Environment.GetEnvironmentVariable("SHIPPER_API_BASE_URL") ?? "https://localhost:7001";
var orderIdText = Environment.GetEnvironmentVariable("SHIPPER_TEST_ORDER_ID");
var token1 = Environment.GetEnvironmentVariable("SHIPPER_TOKEN_1");
var token2 = Environment.GetEnvironmentVariable("SHIPPER_TOKEN_2");

if (!int.TryParse(orderIdText, out var orderId) || orderId <= 0)
    return Fail("Set SHIPPER_TEST_ORDER_ID to an existing Pending order ID.");
if (string.IsNullOrWhiteSpace(token1) || string.IsNullOrWhiteSpace(token2))
{
    if (!string.Equals(Environment.GetEnvironmentVariable("SHIPPER_GENERATE_TEST_TOKENS"), "true", StringComparison.OrdinalIgnoreCase))
        return Fail("Set both token variables, or set SHIPPER_GENERATE_TEST_TOKENS=true and the two Shipper user IDs.");

    var secret = Environment.GetEnvironmentVariable("Jwt__Secret");
    var issuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "ShipperMvp";
    var audience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? "ShipperMvp.Mobile";
    if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        return Fail("Generated test tokens need Jwt__Secret matching the API secret (at least 32 UTF-8 bytes).");
    if (!int.TryParse(Environment.GetEnvironmentVariable("SHIPPER_USER_ID_1"), out var userId1) || userId1 <= 0 ||
        !int.TryParse(Environment.GetEnvironmentVariable("SHIPPER_USER_ID_2"), out var userId2) || userId2 <= 0 || userId1 == userId2)
        return Fail("Set SHIPPER_USER_ID_1 and SHIPPER_USER_ID_2 to different active Shipper user IDs that have Driver rows.");

    token1 = CreateTestToken(userId1, secret, issuer, audience);
    token2 = CreateTestToken(userId2, secret, issuer, audience);
}
if (string.Equals(token1, token2, StringComparison.Ordinal))
    return Fail("The two Shipper tokens must belong to different Shippers.");
if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiBase))
    return Fail("SHIPPER_API_BASE_URL must be an absolute URL.");

using var handler = new HttpClientHandler();
// For local HTTPS development certificates only. Set SHIPPER_ALLOW_UNTRUSTED_CERT=true to opt in.
if (string.Equals(Environment.GetEnvironmentVariable("SHIPPER_ALLOW_UNTRUSTED_CERT"), "true", StringComparison.OrdinalIgnoreCase))
    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
using var http = new HttpClient(handler) { BaseAddress = apiBase };

var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var attempts = new[] { (Shipper: "Shipper 1", Token: token1), (Shipper: "Shipper 2", Token: token2) }
    .Select(async attempt =>
    {
        await startGate.Task;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/orders/{orderId}/accept");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", attempt.Token);
        using var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return (attempt.Shipper, response.StatusCode, Body: body);
    })
    .ToArray();

startGate.SetResult();
var results = await Task.WhenAll(attempts);

foreach (var result in results)
    Console.WriteLine($"{result.Shipper}: {(int)result.StatusCode} {result.StatusCode}{(string.IsNullOrWhiteSpace(result.Body) ? "" : $" — {result.Body}")}");

var acceptedCount = results.Count(r => r.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
var conflictCount = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);
if (acceptedCount == 1 && conflictCount == 1)
{
    Console.WriteLine("PASS: exactly one Shipper accepted the order and the other received HTTP 409 Conflict.");
    return 0;
}

Console.Error.WriteLine($"FAIL: expected one HTTP 200/204 and one HTTP 409; observed {acceptedCount} success(es), {conflictCount} conflict(s).");
return 1;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 2;
}

static string CreateTestToken(int userId, string secret, string issuer, string audience)
{
    var now = DateTimeOffset.UtcNow;
    var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
    var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
    {
        iss = issuer,
        aud = audience,
        sub = userId.ToString(),
        role = "Shipper",
        nbf = now.ToUnixTimeSeconds(),
        exp = now.AddMinutes(10).ToUnixTimeSeconds()
    }));
    var unsignedToken = $"{header}.{payload}";
    var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.ASCII.GetBytes(unsignedToken));
    return $"{unsignedToken}.{Base64Url(signature)}";
}

static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
