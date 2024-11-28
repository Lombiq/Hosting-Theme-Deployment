using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using Lombiq.Hosting.MediaTheme.Bridge.Models;
using Lombiq.Hosting.MediaTheme.Bridge.Services;
using OrchardCore.Deployment;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Deployment;

public class MediaThemeDeploymentSource : DeploymentSourceBase<MediaThemeDeploymentStep>
{
    private readonly IMediaThemeStateStore _mediaThemeStateStore;

    public MediaThemeDeploymentSource(IMediaThemeStateStore mediaThemeStateStore) =>
        _mediaThemeStateStore = mediaThemeStateStore;

    protected override async Task ProcessAsync(MediaThemeDeploymentStep step, DeploymentPlanResult result)
    {
        var mediaThemeState = await _mediaThemeStateStore.GetMediaThemeStateAsync();

        result.Steps.Add(new JsonObject(new Dictionary<string, JsonNode>
        {
            ["name"] = RecipeStepIds.MediaTheme,
            [nameof(MediaThemeDeploymentStep.ClearMediaThemeFolder)] = step.ClearMediaThemeFolder,
            [nameof(MediaThemeStateDocument.BaseThemeId)] = mediaThemeState.BaseThemeId,
        }));
    }
}
