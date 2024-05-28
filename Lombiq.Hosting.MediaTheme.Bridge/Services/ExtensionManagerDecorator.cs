using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Services;

public class ExtensionManagerDecorator : IExtensionManager
{
    private readonly IExtensionManager _decorated;
    private readonly IMediaThemeStateStore _mediaThemeStateStore;

    public ExtensionManagerDecorator(
        IExtensionManager decorated,
        IMediaThemeStateStore mediaThemeStateStore)
    {
        _decorated = decorated;
        _mediaThemeStateStore = mediaThemeStateStore;
    }

    public IEnumerable<IFeatureInfo> GetFeatureDependencies(string featureId)
    {
        var dependencies = _decorated.GetFeatureDependencies(featureId).ToList();

        if (featureId != FeatureNames.MediaTheme) return dependencies;

        var baseThemeId = GetBaseThemeId();
        if (string.IsNullOrEmpty(baseThemeId)) return dependencies;

        var allFeatures = GetFeatures().ToArray();
        var baseTheme = allFeatures.Find(feature => feature.Id == baseThemeId);
        dependencies.Add(baseTheme);

        // The base theme has to be the last dependency, see ThemeFeatureBuilderEvents in Orchard's source.
        var mediaTheme = dependencies.First(theme => theme.Id == FeatureNames.MediaTheme);
        dependencies.Remove(mediaTheme);
        dependencies.Add(mediaTheme);

        return dependencies;
    }

    public IExtensionInfo GetExtension(string extensionId) =>
        _decorated.GetExtension(extensionId);

    public IEnumerable<IExtensionInfo> GetExtensions() =>
        _decorated.GetExtensions();

    public Task<ExtensionEntry> LoadExtensionAsync(IExtensionInfo extensionInfo) =>
        _decorated.LoadExtensionAsync(extensionInfo);

    public IEnumerable<IFeatureInfo> GetFeatures() =>
        _decorated.GetFeatures();

    public IEnumerable<IFeatureInfo> GetFeatures(string[] featureIdsToLoad)
    {
        if (featureIdsToLoad.Contains(FeatureNames.MediaTheme))
        {
            var baseThemeId = GetBaseThemeId();
            if (!string.IsNullOrEmpty(baseThemeId)) featureIdsToLoad = [.. featureIdsToLoad, baseThemeId];
        }

        return _decorated.GetFeatures(featureIdsToLoad);
    }

    public IEnumerable<IFeatureInfo> GetDependentFeatures(string featureId) =>
        _decorated.GetDependentFeatures(featureId);

    public Task<IEnumerable<FeatureEntry>> LoadFeaturesAsync() =>
        _decorated.LoadFeaturesAsync();

    public Task<IEnumerable<FeatureEntry>> LoadFeaturesAsync(string[] featureIdsToLoad) =>
        _decorated.LoadFeaturesAsync(featureIdsToLoad);

    private string GetBaseThemeId() =>
        // It'll be retrieved from cache so it's not an issue.
        _mediaThemeStateStore.GetMediaThemeStateAsync().GetAwaiter().GetResult()?.BaseThemeId;
}
