using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Permissions;

public class MediaThemeDeploymentPermissions : IPermissionProvider
{
    public static readonly Permission ManageMediaTheme = new(
        nameof(ManageMediaTheme),
        "Manage Media Theme.");

    public Task<IEnumerable<Permission>> GetPermissionsAsync() =>
        Task.FromResult(new[]
        {
            ManageMediaTheme,
        }
        .AsEnumerable());

    public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
        [
            new PermissionStereotype
            {
                Name = "Administrator",
                Permissions = [ManageMediaTheme],
            },
        ];
}
