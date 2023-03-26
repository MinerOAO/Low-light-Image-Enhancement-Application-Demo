using CommunityToolkit.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace MauiDemo.Models
{
    public class ResultPageModel
    {
        public string OriginalName { get; set; }
        public string TempName { get; set; }

        private ResultPageModel() { }
        public ResultPageModel(string originalName, string tempName)
        {
            OriginalName = originalName;
            TempName = tempName;
        }

        public async Task<FileSaverResult> SaveImgToDestination(CancellationToken token)
        {
            var readPermission = await GrantPermissions<Permissions.StorageRead>();
            if (readPermission != PermissionStatus.Granted)
                return null;
            var writePermission = await GrantPermissions<Permissions.StorageWrite>();
            if (writePermission != PermissionStatus.Granted)
                return null;

            string path = Path.Combine(FileSystem.Current.CacheDirectory, TempName);
            using Stream stream = System.IO.File.OpenRead(path);
            var fileResult = await FileSaver.Default.SaveAsync(Path.GetFileNameWithoutExtension(OriginalName) + "_" + TempName,
                stream, token);

            //File.Delete(path);

            return fileResult;
        }
        private async Task<PermissionStatus> GrantPermissions<T>() where T : BasePermission, new()
        {
            //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/permissions?view=net-maui-7.0&tabs=windows
            //API 33 BUG
            //https://github.com/dotnet/maui/issues/11275
            PermissionStatus status = await Permissions.CheckStatusAsync<T>();
            if (status != PermissionStatus.Granted)
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    status = await Permissions.RequestAsync<T>();
                }
            }
            return status;
        }
        public ImageSource ReadImageFromTemp()
        {
            return ImageSource.FromFile(Path.Combine(FileSystem.Current.CacheDirectory, TempName));
        }
        public async Task ShareImgAfterSave(string filePath)
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share Image",
                File = new ShareFile(filePath)
            });
        }
    }
}
