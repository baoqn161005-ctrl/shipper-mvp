namespace ShipperMvp.Application.Reviews;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(int customerId, CreateReviewRequest request, CancellationToken ct = default);
    Task<DriverRatingResponse> GetDriverRatingAsync(int driverId, CancellationToken ct = default);
}
