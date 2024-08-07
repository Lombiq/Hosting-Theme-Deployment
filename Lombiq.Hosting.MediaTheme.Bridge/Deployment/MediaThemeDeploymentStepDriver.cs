using Lombiq.Hosting.MediaTheme.Bridge.ViewModels;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using System.Threading.Tasks;
using static Lombiq.HelpfulLibraries.OrchardCore.Contents.CommonContentDisplayTypes;
using static Lombiq.HelpfulLibraries.OrchardCore.Contents.CommonLocationNames;

namespace Lombiq.Hosting.MediaTheme.Bridge.Deployment;

public class MediaThemeDeploymentStepDriver : DisplayDriver<DeploymentStep, MediaThemeDeploymentStep>
{
    public override IDisplayResult Display(MediaThemeDeploymentStep model, BuildDisplayContext context) =>
        Combine(
            View($"{nameof(MediaThemeDeploymentStep)}_Fields_Summary", model).Location(Summary, Content),
            View($"{nameof(MediaThemeDeploymentStep)}_Fields_Thumbnail", model).Location(Thumbnail, Content)
        );

    public override IDisplayResult Edit(MediaThemeDeploymentStep model, BuildEditorContext context) =>
        Initialize<MediaThemeDeploymentStepViewModel>(
            $"{nameof(MediaThemeDeploymentStep)}_Fields_Edit",
            viewModel => viewModel.ClearMediaThemeFolder = model.ClearMediaThemeFolder)
        .Location(Content);

    public override async Task<IDisplayResult> UpdateAsync(MediaThemeDeploymentStep model, UpdateEditorContext context)
    {
        await context.Updater.TryUpdateModelAsync(model, Prefix, viewModel => viewModel.ClearMediaThemeFolder);

        return await EditAsync(model, context);
    }
}
