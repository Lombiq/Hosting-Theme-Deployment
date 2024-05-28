using OrchardCore.DisplayManagement.Manifest;
using static Lombiq.Hosting.MediaTheme.Bridge.Constants.FeatureNames;

[assembly: Theme(
    Name = "Lombiq Hosting - Media Theme - Test Theme",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/Hosting-Media-Theme",
    Version = "0.0.1",
    Description = "A theme only used for testing that demonstrates the local development version of a Media Theme.",
    Dependencies = [MediaThemeBridge]
)]
