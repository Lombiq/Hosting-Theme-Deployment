using Lombiq.HelpfulLibraries.OrchardCore.Users;
using OrchardCore.Security.Permissions;
using System.Collections.Generic;

namespace Lombiq.Hosting.MediaTheme.Bridge.Permissions;

public sealed class MediaThemeDeploymentPermissions : AdminPermissionBase
{
    public static readonly Permission ManageMediaTheme = new(nameof(ManageMediaTheme), "Manage Media Theme.");

    protected override IEnumerable<Permission> AdminPermissions => [ManageMediaTheme];
}
