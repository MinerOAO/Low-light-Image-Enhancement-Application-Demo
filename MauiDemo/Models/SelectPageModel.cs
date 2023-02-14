using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MauiDemo.Models
{
    class SelectPageModel
    {
        public int TestCounter { get { return _testCounter; } set { _testCounter = value; } }

        private int _testCounter = 0;
        private Image<Rgb24> _image;
        FilePickerFileType customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg" , ".png" , ".tif" } }, // file extension
                    { DevicePlatform.Android, new[] { "image/jpeg" } } // MIME type
                });
        public async Task<FileResult> PickImage()
        {
            //PickOptions options = PickOptions.Images;
            PickOptions options = new PickOptions()
            {
                PickerTitle = "Testing Picker Options",
                FileTypes = customFileType
            };
            try
            {
                var result = await FilePicker.PickAsync(options);
                if (result != null)
                {
                    //if (result.FileName.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                    //    result.FileName.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                    return result;
                }

                return null;
            }
            catch (Exception exceptions)
            {
                // TO MessageSys
                Console.WriteLine(exceptions.Message);
            }
            return null;
        }
        public async Task LoadToRgb24(Stream stream)
        {
            //load image
            var tuple = await SixLabors.ImageSharp.Image.LoadWithFormatAsync(stream);
            _image = tuple.Image.CloneAs<Rgb24>();

            int actualWidth = _image.Width;
            int actualHeight = _image.Height;
            if (_image.Width % 2 != 0)
                actualWidth = _image.Width - 1;
            if (_image.Height % 2 != 0)
                actualHeight = _image.Height - 1;
            _image.Mutate(x => x.Crop(actualWidth, actualHeight));
        }
        public ImageSource LoadToDisplay(Stream stream)
        {
            return ImageSource.FromStream(() => stream);
        }
        public async Task Inference(float gamma, float strength, int quality)
        {
            try 
            {
                var ort = await OnnxRuntimeWrapper.LoadModel("Bread_onnx_optimized.onnx");
                ort.StartInference(ref _image, gamma, strength, quality);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}
