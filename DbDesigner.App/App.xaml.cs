using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DbDesigner.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        TryLoadAppIcon();
        base.OnStartup(e);
    }

    private void TryLoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "appicon.ico");
        if (!File.Exists(iconPath))
        {
            return;
        }

        Resources["AppIcon"] = new BitmapImage(new Uri(iconPath));
    }
}
