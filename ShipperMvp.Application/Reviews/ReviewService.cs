namespace ShipperMvp.Application.Reviews;

using Microsoft.EntityFrameworkCore;
using ShipperMvp.Application.Common.Exceptions;
using ShipperMvp.Application.Common.Interfaces;
using ShipperMvp.Domain.Entities;
using ShipperMvp.Domain.Enums;

public sealed class ReviewService(IApplicationDbContext db) : IReviewService
{
    public async Task<ReviewResponse> CreateAsync(int customerId, CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5) throw new BadRequestException("Điểm đánh giá phải từ 1 đến 5.");
        if (request.Comment?.Trim().Length > 1000) throw new BadRequestException("Bình luận tối đa 1000 ký tự.");

        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn hàng.");
        if (order.CustomerId != customerId) throw new ForbiddenAccessException("Bạn không có quyền đánh giá đơn hàng này.");
        if (order.Status != OrderStatus.Completed) throw new BadRequestException("Chỉ có thể đánh giá đơn hàng đã hoàn thành.");
        if (order.DriverId is null) throw new BadRequestException("Đơn hàng chưa được gán shipper.");
        if (await db.Reviews.AnyAsync(r => r.OrderId == order.Id, ct)) throw new BadRequestException("Đơn hàng này đã được đánh giá.");

        var review = new Review { OrderId = order.Id, CustomerId = customerId, DriverId = order.DriverId.Value,
            Rating = request.Rating, Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(), CreatedAt = DateTime.UtcNow };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        return await db.Reviews.Where(r => r.Id == review.Id)
            .Select(r => new ReviewResponse(r.Id, r.OrderId, r.CustomerId, r.DriverId, r.Rating, r.Comment, r.CreatedAt, r.Customer.FullName))
            .SingleAsync(ct);
    }

    public async Task<DriverRatingResponse> GetDriverRatingAsync(int driverId, CancellationToken ct = default)
    {
        if (!await db.Drivers.AnyAsync(d => d.Id == driverId, ct)) throw new NotFoundException("Không tìm thấy shipper.");
        var reviews = await db.Reviews.Where(r => r.DriverId == driverId).OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponse(r.Id, r.OrderId, r.CustomerId, r.DriverId, r.Rating, r.Comment, r.CreatedAt, r.Customer.FullName))
            .ToListAsync(ct);
        return new DriverRatingResponse(driverId, reviews.Count == 0 ? 0 : reviews.Average(r => r.Rating), reviews.Count, reviews);
    }
}
