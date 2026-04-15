using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace CopyTradeMarketApi.Shared.Filters;

/// <summary>
/// Action filter that enforces the <c>API-KEY</c> header on protected endpoints.
/// Only active in the Development environment; all other environments pass through
/// without validation. Apply via <c>[ServiceFilter(typeof(DevApiKeyFilter))]</c>.
/// </summary>
public class DevApiKeyFilter(IWebHostEnvironment env) : IActionFilter
{
    private const string HeaderName = "API-KEY";
    private const string ValidKey   = "SimulatedKeyForDev";

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!env.IsDevelopment()) return;

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key)
            || key != ValidKey)
            context.Result = new UnauthorizedResult();
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context) { }
}
