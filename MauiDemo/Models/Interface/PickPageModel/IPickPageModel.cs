#if ANDROID
using SkiaSharp;
#else
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif
using MauiDemo.Models.Interface.OnnxRuntimeWrapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiDemo.Models.Interface.PickPageModel
{
    public partial class PickPageModel
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

#if ANDROID
        private SKBitmap _image;
#else
        private Image<Rgb24> _image;
#endif

        public ModelStateV2 StateV2;
        public CancellationTokenSource cts;

        private int _testCounter = 0;
        public int TestCounter { get { return _testCounter; } set { _testCounter = value; } }

        private byte _externalCropFactor = 1;
        public byte ExternalCropFactor
        {
            get { return _externalCropFactor; }
            set
            {
                if (value < 1)
                    _externalCropFactor = 1;
                else
                    _externalCropFactor = value;
            }
        }

        private bool _isDownSample = false;
        public bool IsDownSample { get { return _isDownSample; } set { _isDownSample = value; } }

        private bool _isPreViewDownSample = false;
        public bool IsPreViewDownSample { 
            get 
            { 
                if(_isPreViewDownSample == true)
                {
                    _isPreViewDownSample = false;
                    return true;
                }
                return _isPreViewDownSample;
            } 
            set { _isPreViewDownSample = value; } }

        private float _gamma;
        public float Gamma { get { return _gamma; } set { _gamma = value; } }

        private float _strength;
        public float Strength { get { return _strength; } set { _strength = value; } }

        private byte _quality;
        public byte Quality { get { return _quality; } set { _quality = value; } }

        private InferenceType _type;
        public InferenceType Type { get { return _type; } set { _type = value; } }

        private const byte _internalCropFactor = 2;

        public string originalImgName;
        FilePickerFileType customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg" , ".png" , ".tif" } }, // file extension
                    { DevicePlatform.Android, new[] { "image/jpeg" } } // MIME type
                });
        public partial Task<string> Inference();
        public partial Task<FileResult> PickImage();
        public partial Task<FileResult> ShotPhoto();
        public partial Task LoadToRgb24(Stream stream, string fileName);
        public ImageSource LoadToDisplay(Stream stream)
        {
            return ImageSource.FromStream(() => stream);
        }
    }
    public enum InternalState
    {
        Idle,
        ImageLoaded,
        Inferencing,
    }
    public class ModelStateV2 : INotifyPropertyChanged
    {
        // These fields hold the values for the public properties.  
        private InternalState _state;

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
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
                return _state;
            }

            set
            {
                if (value != _state)
                {
                    _state = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}

