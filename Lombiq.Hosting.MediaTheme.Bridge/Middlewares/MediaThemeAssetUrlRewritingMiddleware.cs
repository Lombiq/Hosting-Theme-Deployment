using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using Lombiq.Hosting.MediaTheme.Bridge.Helpers;
using Microsoft.AspNetCore.Http;
using OrchardCore.Environment.Shell;
using OrchardCore.FileStorage;
using OrchardCore.Media;
using OrchardCore.Themes.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Middlewares;

/// <summary>
/// Middleware to rewrite requests coming to ~/mediatheme to the real (Media or theme) paths the files are properly
/// loaded behind the scenes.
/// </summary>
internal sealed class MediaThemeAssetUrlRewritingMiddleware
{
    private readonly RequestDelegate _next;

    public MediaThemeAssetUrlRewritingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IMediaFileStore mediaFileStore,
        ISiteThemeService siteThemeService,
        ShellSettings shellSettings)
    {
        var isMediaThemeRequest = context.Request.Path
            .StartsWithSegments(new PathString(Routes.MediaThemeAssets), StringComparison.OrdinalIgnoreCase, out _);

        if (!isMediaThemeRequest)
        {
            await _next(context);
            return;
        }

        var assetRelativePath = context.Request.Path.Value?.Replace(
            Routes.MediaThemeAssets,
            string.Empty);
        var mediaPath = mediaFileStore.Combine(Paths.MediaThemeRootFolder, Paths.MediaThemeAssetsFolder, assetRelativePath);
        string assetUrl;
        if (!context.IsDevelopment() || await mediaFileStore.FileExistsAsync(mediaPath))
        {
            assetUrl = RequestUrlPrefixRemover.RemoveIfHasPrefix(mediaFileStore.MapPathToPublicUrl(mediaPath), shellSettings);

            if (!string.IsNullOrEmpty(shellSettings.RequestUrlPrefix) &&
                assetUrl.StartsWith("/" + shellSettings.RequestUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                assetUrl = assetUrl[(shellSettings.RequestUrlPrefix.Length + 1)..];
            }
        }
        else
        {
            var activeTheme = await siteThemeService.GetSiteThemeAsync();

            assetUrl = "/" + activeTheme.Id + assetRelativePath;
        }

        context.Request.Path = assetUrl;

        await _next(context);
    }
}
