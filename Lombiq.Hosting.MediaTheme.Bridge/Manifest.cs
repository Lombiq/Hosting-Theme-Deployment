using Lombiq.HelpfulExtensions;
using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Lombiq Hosting - Media Theme Bridge",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/Hosting-Media-Theme",
    Version = "0.0.1",
    Description = "Provides the processing logic for the Media Theme.",
    Category = "Hosting",
    Dependencies = ["OrchardCore.Deployment", "OrchardCore.Media", FeatureIds.GoogleTag]
)]
