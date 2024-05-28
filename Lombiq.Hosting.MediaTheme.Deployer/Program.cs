using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using static Lombiq.Hosting.MediaTheme.Deployer.Constants.PathConstants;
using static System.Console;

namespace Lombiq.Hosting.MediaTheme.Deployer;

public class CommandLineOptions
{
    [Option('p', "path", Required = true, HelpText = "Path of your theme project.")]
    public string? ThemePath { get; set; }

    // This parameter can still be useful if the base theme can't be parsed out of the Manifest easily, like if it uses
    // constants for the ID instead of string literals.
    [Option(
        'i',
        "base-id",
        Required = false,
        HelpText = "ID of the base theme, if any. If left empty, will attempt to get the value from the theme's Manifest.")]
    public string? BaseThemeId { get; set; }

    [Option(
        'c',
        "clear",
        Required = false,
        HelpText = "Whether or not to clear the Media Theme media folder of all files before deployment.")]
    public bool ClearMediaHostingFolder { get; set; } = true;

    [Option(
        'd',
        "deployment-path",
        Required = false,
        HelpText = "The path where you want the deployment package to be written to.")]
    public string? DeploymentPackagePath { get; set; }

    [Option(
        'f',
        "deployment-file-name",
        Required = false,
        HelpText = "The file name of the deployment package zip and the value for the recipe name property.")]
    public string? DeploymentFileName { get; set; }

    [Option(
        'u',
        "remote-deployment-url",
        Required = false,
        HelpText = "The URL to use for Remote Deployment, as indicated on the Orchard Core admin.")]
    public string? RemoteDeploymentUrl { get; set; }

    [Option(
        'n',
        "remote-deployment-client-name",
        Required = false,
        HelpText = "The \"Client Name\" part of the Remote Deployment client's credentials.")]
    public string? RemoteDeploymentClientName { get; set; }

    [Option(
        'k',
        "remote-deployment-client-api-key",
        Required = false,
        HelpText = "The \"Client API Key\" part of the Remote Deployment client's credentials.")]
    public string? RemoteDeploymentClientApiKey { get; set; }
}

internal static partial class Program
{
    internal static readonly string[] FeaturesToEnable = ["Lombiq.Hosting.MediaTheme.Bridge", "Lombiq.Hosting.MediaTheme"];

    public static Task Main(string[] args) =>
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithNotParsed(HandleParseError)
            .WithParsedAsync(RunOptionsAsync);

    private static void HandleParseError(IEnumerable<Error> errors)
    {
        var errorsList = errors.ToList();
        if (errorsList.Count != 0)
        {
            foreach (var error in errorsList)
            {
                WriteLine(error);
            }
        }
        else
        {
            WriteLine("Unknown error.");
        }
    }

    private static async Task RunOptionsAsync(CommandLineOptions options)
    {
        try
        {
            await RunOptionsInnerAsync(options);
        }
        catch (Exception ex)
        {
            WriteLine("Deployment failed with the following exception: {0}", ex);
            Environment.ExitCode = 1;
        }
    }

    private static async Task RunOptionsInnerAsync(CommandLineOptions options)
    {
        // Creating directory for the deployment.
        var newDirectoryPath = CreateNewDirectoryPath(options);

        try
        {
            if (Directory.Exists(newDirectoryPath)) Directory.Delete(newDirectoryPath, recursive: true);

            Directory.CreateDirectory(newDirectoryPath);

            WriteLine("The \"{0}\" directory was created successfully.", newDirectoryPath);
        }
        catch (Exception)
        {
            WriteLine("Creating the directory {0} failed.", newDirectoryPath);
            throw;
        }

        var themePath = options.ThemePath;

        if (string.IsNullOrEmpty(themePath))
        {
            throw new ArgumentException("The theme's path must be provided.");
        }

        var recipeSteps = new JArray();

        // Creating Feature step to enable the Media Theme theme and Bridge module.
        var featureStep = JObject.FromObject(new
        {
            name = "Feature",
            enable = FeaturesToEnable,
        });
        recipeSteps.Add(featureStep);

        // Creating Themes step to set Media Theme as the site theme.
        var themesStep = JObject.FromObject(new
        {
            name = "Themes",
            Site = "Lombiq.Hosting.MediaTheme",
        });
        recipeSteps.Add(themesStep);

        var baseThemeId = string.IsNullOrEmpty(options.BaseThemeId) ? null : options.BaseThemeId;

        if (baseThemeId == null)
        {
            var manifestPath = Path.Combine(themePath, "Manifest.cs");
            var manifestContent = await File.ReadAllTextAsync(manifestPath);
            var baseThemeMatch = BaseThemeRegex().Match(manifestContent);

            if (baseThemeMatch.Success)
            {
                baseThemeId = baseThemeMatch.Groups["baseThemeId"].Value;
            }
        }

        // Creating Media Theme step.
        var mediaThemeStep = JObject.FromObject(new
        {
            name = "mediatheme",
            ClearMediaThemeFolder = options.ClearMediaHostingFolder,
            BaseThemeId = baseThemeId,
        });
        recipeSteps.Add(mediaThemeStep);

        // Creating media step.
        var files = new JArray();

        void AddFile(string rootPath, string filePath)
        {
            // These need to use forward slashes on every platform due to Orchard's import logic.
            var importPath = Path.Combine(rootPath, filePath).Replace('\\', '/');
            var templateJObject = JObject.FromObject(new
            {
                SourcePath = importPath,
                TargetPath = importPath,
            });

            files.Add(templateJObject);
        }

        // Getting assets.
        var assetsPath = Path.Combine(themePath, LocalThemeWwwRootDirectory);
        if (Directory.Exists(assetsPath))
        {
            var allAssetsPaths = Directory.EnumerateFiles(assetsPath, "*", SearchOption.AllDirectories);

            foreach (var assetPath in allAssetsPaths)
            {
                AddFile(MediaThemeAssetsCopyDirectoryPath, assetPath[(assetsPath.Length + 1)..]);
            }

            // Copying assets to deployment directory.
            CopyDirectory(
                assetsPath,
                Path.Join(newDirectoryPath, MediaThemeAssetsCopyDirectoryPath),
                areLiquidFiles: false);
        }

        // Getting templates.
        var templatesPath = Path.Combine(themePath, LocalThemeViewsDirectory);
        if (Directory.Exists(templatesPath))
        {
            var allTemplatesPaths = Directory
                .EnumerateFiles(templatesPath, "*" + LiquidFileExtension, SearchOption.TopDirectoryOnly);

            foreach (var templatePath in allTemplatesPaths)
            {
                AddFile(MediaThemeTemplatesCopyDirectoryPath, templatePath[(templatesPath.Length + 1)..]);
            }

            // Copying templates to deployment directory.
            CopyDirectory(
                templatesPath,
                Path.Join(newDirectoryPath, MediaThemeTemplatesCopyDirectoryPath),
                areLiquidFiles: true,
                recursive: false);
        }

        var mediaStep = JObject.FromObject(new
        {
            name = "media",
            Files = files,
        });
        recipeSteps.Add(mediaStep);

        CreateRecipeAndWriteIt(options, recipeSteps, newDirectoryPath);

        // Zipping the directory.
        var zipFilePath = newDirectoryPath + ".zip";
        ZipFile.CreateFromDirectory(newDirectoryPath, zipFilePath);

        // Getting rid of the original directory.
        Directory.Delete(newDirectoryPath, recursive: true);

        WriteLine("{0} was created successfully. ", zipFilePath);

        if (!string.IsNullOrEmpty(options.RemoteDeploymentUrl))
        {
            await RemoteDeploymentHelper.DeployAsync(options, zipFilePath);
        }
    }

    private static void CopyDirectory(
        string sourceDir,
        string destinationDirectory,
        bool areLiquidFiles,
        bool recursive = true)
    {
        // Get information about the source directory.
        var directory = new DirectoryInfo(sourceDir);

        // Check if the source directory exists.
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {directory.FullName}");
        }

        // Cache directories before we start copying.
        var directories = directory.GetDirectories();

        // Create the destination directory.
        Directory.CreateDirectory(destinationDirectory);

        // Get the files in the source directory and copy to the destination directory.
        foreach (var file in directory.GetFiles())
        {
            var fileName = file.Name;

            // Files should follow the template name convention (with underscores only), not the template file name
            // convention (with dots and hyphens). Note that these don't just differ in the interchangeable characters
            // but may also differ in the order of sections. E.g.
            // [Stereotype]_[DisplayType]__[PartType]__[PartName]__[DisplayMode]_Display is
            // "Widget_Summary__ServicePart__Services__CustomMode_Display" as template name but
            // "Widget-ServicePart-Services-CustomMode.Display.Summary.liquid" as a file name (note e.g. "Summary" being
            // in a different location).
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (areLiquidFiles && (fileNameWithoutExtension.Contains('.') || fileNameWithoutExtension.Contains('-')))
            {
                throw new InvalidOperationException(
                    "Liquid templates must follow the template name, not template *file* name conventions, i.e. they " +
                    " should only contain underscores, not dots and hyphens. See the docs for details: " +
                    "https://docs.orchardcore.net/en/latest/docs/reference/modules/Templates/. Offending file: " +
                    fileName);
            }

            string targetFilePath = Path.Combine(destinationDirectory, fileName);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method.
        if (recursive)
        {
            foreach (var subDirectory in directories)
            {
                string newDestinationDir = Path.Combine(destinationDirectory, subDirectory.Name);
                CopyDirectory(subDirectory.FullName, newDestinationDir, areLiquidFiles);
            }
        }
    }

    private static string CreateNewDirectoryPath(CommandLineOptions values)
    {
        var deploymentPathCommandLineValue = values.DeploymentPackagePath;
        var deploymentPath = !string.IsNullOrEmpty(deploymentPathCommandLineValue)
            ? deploymentPathCommandLineValue
            : Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());

        return values.DeploymentFileName != null
            ? Path.Join(deploymentPath, values.DeploymentFileName)
            : Path.Join(deploymentPath, MediaThemeDeploymentDirectory)
              + DateTime.Now.ToString("ddMMMyyyyHHmmss", CultureInfo.CurrentCulture); // #spell-check-ignore-line
    }

    private static void CreateRecipeAndWriteIt(CommandLineOptions options, JArray steps, string newDirectoryPath)
    {
        // Creating the recipe itself.
        var recipe = JObject.FromObject(new
        {
            name = options.DeploymentFileName ?? "MediaTheme",
            displayName = "Media Theme",
            description = "A recipe created with the media-theme-deployment tool.",
            author = string.Empty,
            website = string.Empty,
            version = string.Empty,
            issetuprecipe = false, // #spell-check-ignore-line
            categories = new JArray(),
            tags = new JArray(),
            steps,
        });

        // Creating JSON file.
        using var file = File.CreateText(Path.Join(newDirectoryPath, RecipeFile));
        using var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented };
        recipe.WriteTo(writer);

        file.Close();
    }

    [GeneratedRegex(@"BaseTheme\s*=\s*""(?<baseThemeId>.*)""", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BaseThemeRegex();
}
