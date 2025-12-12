using System;
using System.Linq;
using System.Windows;
using DbDesigner.App.ViewModels;

namespace DbDesigner.App.Theme;

public static class ThemeManager
{
    private static readonly Uri LightThemeUri = new("Theme/LightTheme.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeUri = new("Theme/DarkTheme.xaml", UriKind.Relative);

    public static void ApplyTheme(ThemeMode mode)
    {
        var resources = Application.Current?.Resources;
        if (resources == null)
        {
            return;
        }

        var targetUri = mode == ThemeMode.Dark ? DarkThemeUri : LightThemeUri;
        var dictionaries = resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => d.Source != null &&
            (d.Source.OriginalString.Contains("Theme/LightTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
             d.Source.OriginalString.Contains("Theme/DarkTheme.xaml", StringComparison.OrdinalIgnoreCase)));

        var newDictionary = new ResourceDictionary { Source = targetUri };

        if (existing != null)
        {
            var index = dictionaries.IndexOf(existing);
            dictionaries.Insert(index, newDictionary);
            dictionaries.Remove(existing);
        }
        else
        {
            dictionaries.Insert(0, newDictionary);
        }
    }
}
