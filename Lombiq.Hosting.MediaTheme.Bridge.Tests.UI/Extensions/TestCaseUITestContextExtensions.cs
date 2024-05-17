using Lombiq.Hosting.MediaTheme.Bridge.Constants;
using Lombiq.Hosting.MediaTheme.Tests.UI.Extensions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OrchardCore.Media;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Hosting.MediaTheme.Bridge.Tests.UI.Extensions;

public static class TestCaseUITestContextExtensions
{
    [SuppressMessage(
        "Security",
        "CA5400:HttpClient may be created without enabling CheckCertificateRevocationList",
        Justification = "It's only disabled for local testing.")]
    public static async Task TestMediaThemeTemplatePageAsync(this UITestContext context, string tenantPrefix = null)
    {
        await context.ExecuteMediaThemeSampleRecipeDirectlyAsync();
        await context.GoToMediaThemeTestContentPageAsync();
        var mediaOptions = await context.GetTenantOptionsAsync<MediaOptions>();

        // Instead of going to the page directly (which will fail) we'll check the response status code directly.
        var templatesPageUri = context.GetAbsoluteUri(
            $"{mediaOptions.Value.AssetsRequestPath}/{Paths.MediaThemeTemplatesWebPath}/Example.liquid");
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using var client = new HttpClient(handler);

        var response = await client.GetAsync(templatesPageUri);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
