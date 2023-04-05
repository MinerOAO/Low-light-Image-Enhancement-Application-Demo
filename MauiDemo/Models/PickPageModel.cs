using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MauiDemo.Models
{
    public class PickPageModel
    {
        public PickPageModel()
        {
            StateV2 = new ModelStateV2();
            _type = InferenceType.Entire;

            _gamma = 1.0f;
            _strength = 0.01f;
            _quality = 100;
        }
        ~PickPageModel()
        {

        }

        public ModelStateV2 StateV2;

        public CancellationTokenSource cts;

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

        private float _gamma;
        public float Gamma { get { return _gamma; } set { _gamma = value; } }

        private float _strength;
        public float Strength { get { return _strength; } set { _strength = value; } }

        private byte _quality;
        public byte Quality { get { return _quality; } set { _quality = value; } }

        private InferenceType _type;
        public InferenceType Type { get { return _type; }  set { _type = value; } }

        private const byte _internalCropFactor = 2;
        private Image<Rgb24> _image;
        public string originalImgName;
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
                PickerTitle = "Pick a image",
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
        public async Task<FileResult> ShotPhoto()
        {
            FileResult photo = null;
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                return photo;
            }
            try
            {
                photo = await MediaPicker.Default.CapturePhotoAsync();
                if(photo != null) 
                    return photo;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return photo;
            }
            return photo;

        }
        public async Task LoadToRgb24(Stream stream, string fileName)
        {
            StateV2.ModelState = InternalState.Idle;
            //load image
            if (fileName != null)
                originalImgName = fileName;
            else
                originalImgName = null;

            if (stream == null)
            {
                _image = null;
                StateV2.ModelState = InternalState.Idle;
                return;
            }
            else
            {
                //ImageSharp needs to pre-seed generics to
                //avoid "Attempting to JIT compile method... while running in aot-only mode."
                //https://github.com/dotnet/runtime/issues/71210
                //https://github.com/dotnet/runtime/issues/52559
                //LLVM ENABLED
                var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
                _image = image.CloneAs<Rgb24>();
                StateV2.ModelState = InternalState.ImageLoaded;
            }
        }
        public ImageSource LoadToDisplay(Stream stream)
        {
            return ImageSource.FromStream(() => stream);
        }
        private Image<Rgb24> FitImage()
        {
            var image = _image.CloneAs<Rgb24>();
            if (_isDownSample)
            {
                image.Mutate(x => x.Resize(_image.Width / 2, _image.Height / 2));
            }
            int actualFactor = _internalCropFactor * _externalCropFactor;
            int actualWidth = image.Width - image.Width % actualFactor;
            int actualHeight = image.Height - image.Height % actualFactor;
            image.Mutate(x => x.Crop(actualWidth, actualHeight));
            return image;
        }
        public async Task<string> Inference()
        {
            if (StateV2.ModelState != InternalState.ImageLoaded)
                return null;
            StateV2.ModelState = InternalState.Inferencing;
            string resultName = null;

            try 
            {
                await Task.Run(async () =>
                {
                    var fittImage = FitImage();
                    var ortWrapper = await OnnxRuntimeWrapper.LoadModel("Bread_onnx_fullres_test.onnx");
                    // 量化时，注意onnxruntime的python版本与C# nupackage版本中opset算子版本
                    // x86-64 with VNNI, GPU with Tensor Core int8 support and ARM with dot-product instructions can get better performance in general.
                    //var ort = await OnnxRuntimeWrapper.LoadModel("Bread_onnx_optimized_dynamic_quantized.onnx");
                    resultName = await ortWrapper.StartInference(fittImage, _gamma, _strength, _quality, _type);
                });
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                StateV2.ModelState = InternalState.Idle;
                return resultName;
            }
            StateV2.ModelState = InternalState.ImageLoaded;
            return resultName;
        }
    }
    public enum InternalState
    {
        Idle,
        ImageLoaded,
        Inferencing
    }
    public class ModelStateV2 : INotifyPropertyChanged
    {
        // These fields hold the values for the public properties.  
        private InternalState _state;

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // The constructor is private to enforce the factory pattern.  
        public ModelStateV2()
        {
            _state = InternalState.Idle;
        }

        public InternalState ModelState
        {
            get
            {
                return this._state;
            }

            set
            {
                if (value != this._state)
                {
                    this._state = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}

