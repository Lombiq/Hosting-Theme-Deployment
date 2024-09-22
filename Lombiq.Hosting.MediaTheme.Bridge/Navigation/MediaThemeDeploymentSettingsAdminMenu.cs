using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;
using static Lombiq.Hosting.MediaTheme.Bridge.Permissions.MediaThemeDeploymentPermissions;

namespace Lombiq.Hosting.MediaTheme.Bridge.Navigation;

public class MediaThemeDeploymentSettingsAdminMenu : INavigationProvider
{
    private readonly IStringLocalizer T;

    public MediaThemeDeploymentSettingsAdminMenu(IStringLocalizer<MediaThemeDeploymentSettingsAdminMenu> stringLocalizer) => T = stringLocalizer;

    public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase)) return ValueTask.CompletedTask;

        builder
            .Add(T["Configuration"], configuration => configuration
                .Add(T["Media Theme"], T["Media Theme"].PrefixPosition(), entry => entry
                    .AddClass("mediatheme").Id("mediatheme")
                    .Action("Index", "Admin", new { area = FeatureNames.MediaThemeBridge })
                    .Permission(ManageMediaTheme)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
