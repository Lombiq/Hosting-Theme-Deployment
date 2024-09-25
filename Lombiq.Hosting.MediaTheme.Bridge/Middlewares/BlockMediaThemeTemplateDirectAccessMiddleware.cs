using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using OrchardCore.Media;
using OrchardCore.Routing;
using System;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Middlewares;

public class BlockMediaThemeTemplateDirectAccessMiddleware
{
    private readonly RequestDelegate _next;

    private readonly PathString _assetsRequestPath;

    public BlockMediaThemeTemplateDirectAccessMiddleware(
        RequestDelegate next,
        IOptions<MediaOptions> mediaOptions)
    {
        _next = next;
        _assetsRequestPath = mediaOptions.Value.AssetsRequestPath;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isMediaThemeTemplateRequest = context.Request.Path.StartsWithNormalizedSegments(
            _assetsRequestPath + "/" + Paths.MediaThemeTemplatesWebPath,
            StringComparison.OrdinalIgnoreCase,
            out _);

        // Since this middleware needs to run early (see comment in Startup), the user's authentication state won't yet
        // be available. So, we can't let people with the ManageMediaTheme permission still see the templates.
        if (!isMediaThemeTemplateRequest)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.Headers.Append(HeaderNames.ContentLength, "0");
        await context.Response.Body.FlushAsync(context.RequestAborted);
        // Use Complete instead of Abort which actually causes a 502 Bad Gateway response.
        await context.Response.CompleteAsync();
    }
}
