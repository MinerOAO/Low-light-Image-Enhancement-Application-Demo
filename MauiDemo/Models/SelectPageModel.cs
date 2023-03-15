using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MauiDemo.Models
{
    class SelectPageModel
    {
        public SelectPageModel()
        {

        }
        private int _testCounter = 0;
        public int TestCounter { get { return _testCounter; } set { _testCounter = value; } }

        private byte _externalCropFactor = 1;
        public byte ExternalCropFactor { get { return _externalCropFactor; } set {
                if (value < 1)
                    _externalCropFactor = 1;
                else
                    _externalCropFactor = value;
            } }

        private bool _isDownSample = false;
        public bool IsDownSample { get { return _isDownSample; } set { _isDownSample = value; } }


        private float _gamma = 1.0f;
        public float Gamma { get { return _gamma; } set { _gamma = value; } }

        private float _strength = 0.01f;
        public float Strength { get { return _strength; } set { _strength = value; } }

        private byte _quality = 100;
        public byte Quality { get { return _quality; } set { _quality = value; } }

        private InferenceType _type = InferenceType.Entire;
        public InferenceType Type { get { return _type; }  set { _type = value; } }

        private const byte _internalCropFactor = 2;
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

            if(_isDownSample) 
            {
                _image.Mutate(x => x.Resize(_image.Width / 2, _image.Height / 2));
            }

            int actualFactor = _internalCropFactor * _externalCropFactor;
            int actualWidth = _image.Width - _image.Width % actualFactor;
            int actualHeight = _image.Height - _image.Height % actualFactor;
            _image.Mutate(x => x.Crop(actualWidth, actualHeight));
        }
        public ImageSource LoadToDisplay(Stream stream)
        {
            return ImageSource.FromStream(() => stream);
        }
        public async Task Inference()
        {
            try 
            {          
                using (_image)
                {
                    var ort = await OnnxRuntimeWrapper.LoadModel("Bread_onnx_all_halfres_test.onnx");
                    // 量化时，注意onnxruntime的python版本与C# nupackage版本中opset算子版本
                    // x86-64 with VNNI, GPU with Tensor Core int8 support and ARM with dot-product instructions can get better performance in general.
                    //var ort = await OnnxRuntimeWrapper.LoadModel("Bread_onnx_optimized_dynamic_quantized.onnx");
                    await ort.StartInference(_image, _gamma, _strength, _quality, _type);
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}
