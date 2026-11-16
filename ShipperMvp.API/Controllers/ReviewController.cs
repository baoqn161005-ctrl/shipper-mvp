namespace ShipperMvp.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipperMvp.Application.Common.Exceptions;
using ShipperMvp.Application.Reviews;

[ApiController]
[Route("api")]
public sealed class ReviewController(IReviewService reviews) : ControllerBase
{
    [HttpPost("reviews")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ReviewResponse>> Create(CreateReviewRequest request, CancellationToken ct)
    {
        var created = await reviews.CreateAsync(CurrentUserId(), request, ct);
        return Created($"/api/drivers/{created.DriverId}/rating", created);
    }

    [HttpGet("drivers/{driverId:int}/rating")]
    [AllowAnonymous]
    public async Task<ActionResult<DriverRatingResponse>> GetDriverRating(int driverId, CancellationToken ct) =>
        Ok(await reviews.GetDriverRatingAsync(driverId, ct));

    private int CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (int.TryParse(raw, out var id)) return id;
        throw new ForbiddenAccessException("Token không chứa user id hợp lệ.");
    }
}
