using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace LLIEDOTNET8.Models.Interface.OnnxRuntimeWrapper
{
    public enum InferenceType
    {
        Entire,
        Split
    }
    public partial class OnnxRuntimeWrapper
    {
        //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/invoke-platform-code?view=net-maui-7.0#implement-the-api-per-platform
        //声明
        private int _width = 0;
        private int _height = 0;
        private int _quality = 100;
        public int Width { get { return _width; } set { _width = value; } }
        public int Height { get { return _height; } set { _height = value; } }

        //FileSystem.OpenAppPackageFileAsync
        //Files that were added to the project with the Build Action of MauiAsset can be opened with this method.
        //.NET MAUI projects will process any file in the Resources\Raw folder as a MauiAsset.
        private readonly byte[] _model;
        public byte[] Model { get { return _model; } private set { } }

        private InferenceSession _session = null;

        private Dictionary<int, IEnumerable<float>> outputData = new Dictionary<int, IEnumerable<float>>();
#if ANDROID
        public partial Task<string> StartInference(SKBitmap RGBImage, float gamma, float strength, int quality, InferenceType type = InferenceType.Entire);
#else
        public partial Task<string> StartInference(Image<Rgb24> RGBImage, float gamma, float strength, int quality, InferenceType type = InferenceType.Entire);
#endif

        //Multi-platform Method Restricts
        //partial methods to be without access modifiers
        //returns void
        private partial int Run(ref Tensor<float> gammaTensor, ref Tensor<float> strengthTensor, ref DenseTensor<float> inputTensor);


        //Constructor
        public async static Task<OnnxRuntimeWrapper> LoadModel(string modelName = "Bread_onnx_fullres_new_test.onnx")
        {
            using (var rawStream = await FileSystem.OpenAppPackageFileAsync(modelName))
            {
                using (var ms = new MemoryStream())
                {
                    await rawStream.CopyToAsync(ms);

                    var buffer = new byte[ms.Length];
                    var msBuffer = ms.GetBuffer();
                    for (int i = 0; i < ms.Length; ++i)
                    {
                        buffer[i] = msBuffer[i];
                    }
                    var instance = new OnnxRuntimeWrapper(buffer);
                    return instance;
                }
            }
        }
    }
}
