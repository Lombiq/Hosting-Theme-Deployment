using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Tests.UI.Extensions;

public static class TestCaseUITestContextExtensions
{
    public static async Task TestMediaThemeDeployedBehaviorAsync(this UITestContext context, string tenantPrefix = null)
    {
        await context.ExecuteMediaThemeSampleRecipeDirectlyAsync();
        await context.GoToMediaThemeTestContentPageAsync();
        AssertElements(context, "v", tenantPrefix);
    }

    public static async Task TestMediaThemeLocalBehaviorAsync(this UITestContext context, string tenantPrefix = null)
    {
        await context.SetThemeDirectlyAsync("Lombiq.Hosting.MediaTheme.Tests.Theme");
        await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
        AssertElements(context, "mediatheme", tenantPrefix);
    }

    private static void AssertElements(UITestContext context, string cacheBustingParameterName, string tenantPrefix)
    {
        context.Exists(By.XPath(
            $"//head//link[contains(@href, '{GetTenantUrlPrefix(tenantPrefix)}/mediatheme/example.css?{cacheBustingParameterName}=')]").Hidden());
        context.Exists(By.XPath("//p[contains(., 'This is an example template hosted in Media Theme.')]"));
        context.Exists(By.XPath($"//img[contains(@src, '{GetTenantUrlPrefix(tenantPrefix)}/mediatheme/example.png')]"));
        context.Exists(By.XPath($"//img[contains(@src, '{GetTenantUrlPrefix(tenantPrefix)}/mediatheme/example2.png?{cacheBustingParameterName}=')]"));
    }

    private static string GetTenantUrlPrefix(string tenantName) =>
        string.IsNullOrEmpty(tenantName) ? string.Empty : "/" + tenantName;
}
