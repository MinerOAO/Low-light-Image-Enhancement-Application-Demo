using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MauiDemo.Models
{
    class SelectPageModel
    {
        private Image<Rgb24> _image;
        FilePickerFileType customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg" } }, // file extension
                    { DevicePlatform.Android, new[] { "image/jpeg" } } // MIME type
                });
        public async Task<FileResult> SelectImage()
        {
            //PickOptions options = PickOptions.Images;
            PickOptions options = new PickOptions()
            {
                PickerTitle = "Testing Picker Options",
                FileTypes = customFileType
            };
            try
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    //if (result.FileName.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                    //    result.FileName.EndsWith("png", StringComparison.OrdinalIgnoreCase))

                    using var iostream = await result.OpenReadAsync();
                    //load image
                    var tuple = await SixLabors.ImageSharp.Image.LoadWithFormatAsync(iostream);
                    _image = tuple.Image.CloneAs<Rgb24>();
                    //show
                    var image = ImageSource.FromStream(() => iostream);

                }

                return result;
            }
            catch (Exception exceptions)
            {
                // TO MessageSys
            }
            return null;
        }
    }
}
