namespace CryptoTrading.Middleware
{
    /// <summary>
    /// Middleware to ensure every request has a correlation ID for distributed tracing
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation ID from header, or generate new one
            var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            // Store in HttpContext.Items for use throughout the request
            context.Items["CorrelationId"] = correlationId;

            // Add to response headers
            context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

            // Add to logging scope
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in request with CorrelationId: {CorrelationId}", correlationId);
                    throw;
                }
            }
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}

