using CommunityToolkit.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            string path = Path.Combine(FileSystem.Current.CacheDirectory, TempName);
            using Stream stream = System.IO.File.OpenRead(path);
            var fileResult = await FileSaver.Default.SaveAsync(TempName, stream, token);

            File.Delete(path);

            return fileResult;
        }
        public ImageSource ReadImageFromTemp()
        {
            return ImageSource.FromFile(Path.Combine(FileSystem.Current.CacheDirectory, TempName));
        }
    }
}
