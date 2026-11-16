namespace ShipperMvp.Application.Common.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException() : base("Dữ liệu đã thay đổi bởi người dùng khác.") { }

    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}
