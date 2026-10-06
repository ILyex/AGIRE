using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Hawdh.Portal.Localization;

namespace Hawdh.Portal.Tests;

public sealed class LocalizationCatalogTests
{
    [Fact]
    public void Every_localization_key_used_by_a_component_has_arabic_and_french_text()
    {
        var root = FindRepositoryRoot();
        var componentRoot = Path.Combine(root, "src", "web", "Hawdh.Portal", "Components");
        var usedKeys = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), "T\\[\\\"([^\\\"]+)\\\"\\]")
                .Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(usedKeys);

        var text = new UiText();
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-DZ");
            foreach (var key in usedKeys)
            {
                Assert.Contains(key, UiText.Keys);
                Assert.False(string.IsNullOrWhiteSpace(text[key]), $"Arabic text is missing for '{key}'.");
                Assert.NotEqual(key, text[key]);
            }

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-DZ");
            foreach (var key in usedKeys)
            {
                Assert.False(string.IsNullOrWhiteSpace(text[key]), $"French text is missing for '{key}'.");
                Assert.NotEqual(key, text[key]);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private static string FindRepositoryRoot([CallerFilePath] string sourceFile = "")
    {
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Hawdh.Platform.slnx")))
                return directory.FullName;

        throw new DirectoryNotFoundException("Could not locate the Hawdh.Platform repository root.");
    }
}
