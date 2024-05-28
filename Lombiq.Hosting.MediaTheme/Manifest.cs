using OrchardCore.DisplayManagement.Manifest;
using static Lombiq.Hosting.MediaTheme.Bridge.Constants.FeatureNames;

[assembly: Theme(
    Name = "Lombiq Hosting - Media Theme",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/Hosting-Media-Theme",
    Version = "0.0.1",
    Description = "Allows developers to host their themes in the Orchard Core Media Library, including templates and assets.",
    Category = "Hosting",
    Dependencies = [MediaThemeBridge]
)]
