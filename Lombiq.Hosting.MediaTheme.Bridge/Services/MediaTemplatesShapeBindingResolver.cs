using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Implementation;
using OrchardCore.Liquid;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Lombiq.Hosting.MediaTheme.Bridge.Services;

public class MediaTemplatesShapeBindingResolver : IShapeBindingResolver
{
    private readonly ILiquidTemplateManager _liquidTemplateManager;
    private readonly IHttpContextAccessor _hca;
    private readonly HtmlEncoder _htmlEncoder;
    private readonly IMediaThemeCachingService _mediaThemeCachingService;

    public MediaTemplatesShapeBindingResolver(
        ILiquidTemplateManager liquidTemplateManager,
        IHttpContextAccessor hca,
        HtmlEncoder htmlEncoder,
        IMediaThemeCachingService mediaThemeCachingService)
    {
        _liquidTemplateManager = liquidTemplateManager;
        _hca = hca;
        _htmlEncoder = htmlEncoder;
        _mediaThemeCachingService = mediaThemeCachingService;
    }

    public async Task<ShapeBinding> GetShapeBindingAsync(string shapeType) =>
        !AdminAttribute.IsApplied(_hca.HttpContext) &&
        await _mediaThemeCachingService.GetMemoryCachedMediaTemplateAsync(shapeType) is { } mediaTemplate
            ? new()
            {
                BindingName = shapeType,
                BindingSource = shapeType,
                BindingAsync = displayContext => BindingAsync(displayContext, mediaTemplate.Content),
            }
            : null;

    private async Task<IHtmlContent> BindingAsync(DisplayContext displayContext, string text)
    {
        var content = await _liquidTemplateManager.RenderHtmlContentAsync(text, _htmlEncoder, displayContext.Value);
        return content;
    }
}
