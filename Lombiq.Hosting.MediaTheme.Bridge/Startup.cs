using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using Lombiq.Hosting.MediaTheme.Bridge.Deployment;
using Lombiq.Hosting.MediaTheme.Bridge.Middlewares;
using Lombiq.Hosting.MediaTheme.Bridge.Navigation;
using Lombiq.Hosting.MediaTheme.Bridge.Permissions;
using Lombiq.Hosting.MediaTheme.Bridge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement;
using OrchardCore.Environment.Extensions;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;
using System;

namespace Lombiq.Hosting.MediaTheme.Bridge;

public class Startup : StartupBase
{
    // Make sure the middlewares run first, so we can block Media Theme template requests in time.
    public override int ConfigureOrder => int.MinValue;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionProvider, MediaThemeDeploymentPermissions>();
        services.AddNavigationProvider<MediaThemeDeploymentSettingsAdminMenu>();
        services.AddSingleton<IMediaThemeStateStore, MediaThemeStateStore>();
        services.Decorate<IExtensionManager, ExtensionManagerDecorator>();
        services.AddScoped<IShapeBindingResolver, MediaTemplatesShapeBindingResolver>();
        services.AddScoped<IMediaThemeManager, MediaThemeManager>();
        services.AddRecipeExecutionStep<MediaThemeStep>();
        services.AddDeployment<MediaThemeDeploymentSource, MediaThemeDeploymentStep, MediaThemeDeploymentStepDriver>();
        services.AddScoped<IAuthorizationHandler, ManageMediaThemeFolderAuthorizationHandler>();
        services.AddScoped<IMediaThemeCachingService, MediaThemeCachingService>();
        services.AddOrchardServices();
        services.Decorate<IFileVersionProvider, FileVersionProviderDecorator>();
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        app.UseMiddleware<MediaThemeAssetUrlRewritingMiddleware>();
        app.UseMiddleware<BlockMediaThemeTemplateDirectAccessMiddleware>();
    }
}
