namespace CopyTradeMarketApi.Host.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            UnauthorizedAccessException  => (StatusCodes.Status401Unauthorized,    "Unauthorized"),
            KeyNotFoundException         => (StatusCodes.Status404NotFound,        "Not Found"),
            ConflictException            => (StatusCodes.Status409Conflict,        "Conflict"),
            TooManyRequestsException     => (StatusCodes.Status429TooManyRequests, "Too Many Requests"),
            InvalidOperationException    => (StatusCodes.Status400BadRequest,      "Bad Request"),
            _                            => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        _logger.LogError(ex, "Unhandled exception: {Method} {Path}", context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = _env.IsProduction() ? null : ex.Message
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
