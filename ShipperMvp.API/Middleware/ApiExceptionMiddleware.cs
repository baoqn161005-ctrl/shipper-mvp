namespace ShipperMvp.API.Middleware;

using Microsoft.AspNetCore.Mvc;
using ShipperMvp.Application.Common.Exceptions;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy"),
                ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Không có quyền truy cập"),
                BadRequestException => (StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ"),
                ConcurrencyException => (StatusCodes.Status409Conflict, "Đơn hàng đã được nhận"),
                _ => (StatusCodes.Status500InternalServerError, "Lỗi máy chủ")
            };
            if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Unhandled API exception");
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Detail = exception.Message });
        }
    }
}
