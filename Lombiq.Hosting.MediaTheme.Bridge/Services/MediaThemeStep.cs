using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using OrchardCore.FileStorage;
using OrchardCore.Media;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Services;

public sealed class MediaThemeStep : IRecipeStepHandler
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IMediaThemeManager _mediaThemeManager;

    public MediaThemeStep(
        IMediaFileStore mediaFileStore,
        IMediaThemeManager mediaThemeManager)
    {
        _mediaFileStore = mediaFileStore;
        _mediaThemeManager = mediaThemeManager;
    }

    public async Task ExecuteAsync(RecipeExecutionContext context)
    {
        if (!string.Equals(context.Name, RecipeStepIds.MediaTheme, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var model = context.Step.Deserialize<MediaThemeStepModel>();

        await _mediaThemeManager.UpdateBaseThemeAsync(model.BaseThemeId);

        if (model.ClearMediaThemeFolder)
        {
            await _mediaFileStore.TryDeleteDirectoryAsync(_mediaFileStore.Combine(Paths.MediaThemeRootFolder));
        }
    }

    private sealed class MediaThemeStepModel
    {
        public string BaseThemeId { get; set; } = string.Empty;
        public bool ClearMediaThemeFolder { get; set; } = true;
    }
}
