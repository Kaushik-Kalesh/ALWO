using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
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

    // TODO: Fetch WindowsApps (Microsoft Store) applications 
    class InstalledApplications
    {
        private static  List<AppInfo> installedApps = new List<AppInfo>();
        private static List<string> shortcutsDirectories = new List<string> {
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs",
            @"C:\Users\kaush\AppData\Roaming\Microsoft\Windows\Start Menu\Programs"
        };

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
            foreach (var shrtDir in shortcutsDirectories)
            {
                await AddApp(shrtDir);

                foreach (var dir in Directory.GetDirectories(shrtDir))
                {
                    await AddApp(dir);
                }
            }
                return installedApps.DistinctBy(e => e.Path)
                                    .OrderBy(e => e.Name)
                                    .ToList();
        }
    }
}
