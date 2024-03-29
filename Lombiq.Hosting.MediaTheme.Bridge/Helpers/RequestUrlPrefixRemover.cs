using OrchardCore.Environment.Shell;
using System;

namespace Lombiq.Hosting.MediaTheme.Bridge.Helpers;

internal static class RequestUrlPrefixRemover
{
    public static string RemoveIfHasPrefix(string path, ShellSettings shellSettings)
    {
        var requestUrlPrefix = shellSettings.RequestUrlPrefix;
        if (string.IsNullOrEmpty(requestUrlPrefix) ||
            !path.StartsWith("/" + requestUrlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return path[(requestUrlPrefix.Length + 1)..];
    }
}
