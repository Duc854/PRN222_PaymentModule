using PaymentModule.Business.Exceptions;
using PaymentModule.Web.Infrastructure;
using Microsoft.Extensions.Logging;

namespace PaymentModule.Web.Middleware
{
    public class CustomExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _fileLogger;

        public CustomExceptionLoggingMiddleware(RequestDelegate next)
        {
            _next = next;

            var provider = new FileLoggerProvider("Logs/Exceptions.log");
            _fileLogger = provider.CreateLogger(nameof(CustomExceptionLoggingMiddleware));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case PaymentException pex:
                        _fileLogger.LogWarning(ex,
                            "💳 PaymentException xảy ra [TransactionId: {TransactionId}] - {Message}",
                            pex.TransactionID ?? "N/A", ex.Message);
                        break;

                    case ShippingApiException sex:
                        _fileLogger.LogWarning(sex,
                            "🚚 ShippingApiException xảy ra: {Message}", sex.Message);
                        break;

                    case EmailException eex:
                        _fileLogger.LogWarning(eex,
                            "📧 EmailException xảy ra: {Message}", eex.Message);
                        break;

                    default:
                        _fileLogger.LogError(ex,
                            "❗ Exception không xác định: {Message}", ex.Message);
                        break;
                }

                throw;
            }
        }
    }

    public static class CustomExceptionLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomExceptionLoggingMiddleware>();
        }
    }
}
