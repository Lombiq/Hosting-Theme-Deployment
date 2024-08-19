using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using Lombiq.Hosting.MediaTheme.Bridge.Models;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Environment.Shell;
using OrchardCore.FileStorage;
using OrchardCore.Media;
using OrchardCore.Modules.Manifest;
using OrchardCore.Themes.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Services;

public class MediaThemeManager : IMediaThemeManager
{
    private const string LiquidExtension = ".liquid";

    private readonly IMediaThemeStateStore _mediaThemeStateStore;
    private readonly IShellFeaturesManager _shellFeaturesManager;
    private readonly IMemoryCache _memoryCache;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly ISiteThemeService _siteThemeService;

    public MediaThemeManager(
        IMediaThemeStateStore mediaThemeStateStore,
        IShellFeaturesManager shellFeaturesManager,
        IMemoryCache memoryCache,
        IMediaFileStore mediaFileStore,
        ISiteThemeService siteThemeService)
    {
        _mediaThemeStateStore = mediaThemeStateStore;
        _shellFeaturesManager = shellFeaturesManager;
        _memoryCache = memoryCache;
        _mediaFileStore = mediaFileStore;
        _siteThemeService = siteThemeService;
    }

    public async Task UpdateBaseThemeAsync(string baseThemeId)
    {
        ThrowIfBaseThemeIdIsInvalid(baseThemeId);

        if (!string.IsNullOrEmpty(baseThemeId))
        {
            var baseThemeFeature = (await _shellFeaturesManager.GetAvailableFeaturesAsync())
                .FirstOrDefault(feature => feature.IsTheme() && feature.Id == baseThemeId)
                ?? throw new ArgumentException($"Theme with the given ID ({baseThemeId}) doesn't exist.", nameof(baseThemeId));
            await _shellFeaturesManager.EnableFeaturesAsync([baseThemeFeature], force: true);
        }

        var state = await _mediaThemeStateStore.LoadMediaThemeStateAsync();
        state.BaseThemeId = baseThemeId;
        await _mediaThemeStateStore.SaveMediaThemeStateAsync(state);

        // Invalidate the cache to have the harvesters include the shapes from the base theme.
        var currentTheme = await _siteThemeService.GetSiteThemeAsync();
        if (currentTheme.Id == FeatureNames.MediaTheme)
        {
            _memoryCache.Remove($"ShapeTable:{currentTheme.Id}");
        }
    }

    public async Task<IEnumerable<(string Id, string Name)>> GetAvailableBaseThemesAsync()
    {
        var enabledFeatures = await _shellFeaturesManager.GetEnabledFeaturesAsync();
        return (await _shellFeaturesManager.GetAvailableFeaturesAsync())
            .Where(feature =>
                feature.IsTheme() &&
                feature.Id != FeatureNames.MediaTheme &&
                !feature.Extension.Manifest.Tags.Any(tag =>
                    tag.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
                    tag.Equals(ManifestConstants.AdminTag, StringComparison.OrdinalIgnoreCase)) &&
                enabledFeatures.Any(enabledFeature => enabledFeature.Id == feature.Id))
            .Select(feature => (feature.Id, feature.Name));
    }

    public async Task<MediaTemplate> GetMediaTemplateByShapeTypeAsync(string shapeType)
    {
        var templatePath = GetPath(shapeType + LiquidExtension);

        // Check if the file exists with a different casing, before giving up.
        if (!await _mediaFileStore.FileExistsAsync(templatePath))
        {
            var basePath = GetPath();
            var file = await _mediaFileStore
                .GetDirectoryContentAsync(basePath)
                .Where(file => file.Name.EqualsOrdinalIgnoreCase(shapeType + LiquidExtension))
                .FirstOrDefaultAsync();

            if (file == null) return null;
            templatePath = GetPath(file.Name);
        }

        await using var templateFileStream = await _mediaFileStore.GetFileStreamAsync(templatePath);
        using var reader = new StreamReader(templateFileStream);
        var content = await reader.ReadToEndAsync();

        return new MediaTemplate
        {
            TemplatePath = templatePath,
            ShapeType = shapeType,
            Content = content,
        };
    }

    private string GetPath(string shapeType = null) =>
        shapeType == null
            ? _mediaFileStore.Combine(Paths.MediaThemeRootFolder, Paths.MediaThemeTemplatesFolder)
            : _mediaFileStore.Combine(Paths.MediaThemeRootFolder, Paths.MediaThemeTemplatesFolder, shapeType);

    private static void ThrowIfBaseThemeIdIsInvalid(string baseThemeId)
    {
        if (baseThemeId == FeatureNames.MediaTheme)
        {
            throw new ArgumentException("The Media Theme can't be its own base theme.", nameof(baseThemeId));
        }
    }
}
