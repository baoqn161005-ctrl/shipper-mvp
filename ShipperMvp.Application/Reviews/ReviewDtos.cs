namespace ShipperMvp.Application.Reviews;

public sealed record CreateReviewRequest(int OrderId, int Rating, string? Comment);
public sealed record ReviewResponse(int Id, int OrderId, int CustomerId, int DriverId, int Rating, string? Comment, DateTime CreatedAt, string CustomerName);
public sealed record DriverRatingResponse(int DriverId, double AverageRating, int ReviewCount, IReadOnlyList<ReviewResponse> Reviews);
