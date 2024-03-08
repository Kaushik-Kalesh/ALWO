using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Test;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace ALWO
{
    public class AppInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public BitmapImage Icon { get; set; }
    }

    class InstalledApps
    {
        private static  List<AppInfo> installedApps = new List<AppInfo>();
        private const string START_MENU_PATH = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs";

        private async static Task AddApp(string dir)
        {
            foreach (var shortcut in Directory.GetFiles(dir, "*.lnk"))
            {
                if (string.IsNullOrEmpty(ShortcutResolver.ResolveShortcut(shortcut))) { continue; }

                BitmapImage bitmapImage = new BitmapImage();
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(ShortcutResolver.ResolveShortcut(shortcut));
                    StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                    bitmapImage.SetSource(thumbnail);
                }
                catch
                {
                    //bitmapImage.UriSource = new Uri("C:\\Users\\kaush\\source\\repos\\ALWO\\ALWO\\Assets\\Square44x44Logo.scale-200.png");
                    continue;
                }

                installedApps.Add(new AppInfo
                {
                    Name = Path.GetFileNameWithoutExtension(shortcut),
                    Path = ShortcutResolver.ResolveShortcut(shortcut),
                    Icon = bitmapImage
                });
            }
        }

        public async static Task<List<AppInfo>> GetInstalledApps()
        {
            await AddApp(START_MENU_PATH);

            foreach (var dir in Directory.GetDirectories(START_MENU_PATH))
            {
                await AddApp(dir);
            }
            
            return installedApps.DistinctBy(e => e.Path)
                                .OrderBy(e => e.Name)
                                .ToList();
        }
    }
}
