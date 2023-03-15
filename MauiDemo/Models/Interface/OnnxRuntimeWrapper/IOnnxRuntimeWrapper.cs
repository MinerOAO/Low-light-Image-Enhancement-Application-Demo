using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.ML.OnnxRuntime;
using System.Diagnostics;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
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

        private Dictionary<int, IEnumerable<float>> outputData = new Dictionary<int, IEnumerable<float>>();
        public partial Task StartInference(Image<Rgb24> RGBImage, float gamma, float strength, int quality, InferenceType type = InferenceType.Entire);
        //Multi-platform Method Restricts
        //partial methods to be without access modifiers
        //returns void
        partial void Run(List<NamedOnnxValue> inputData, ref int sessionID);

        public async static Task<OnnxRuntimeWrapper> LoadModel(string modelName)
        {
            using (var rawStream = await FileSystem.OpenAppPackageFileAsync(modelName))
            {
                using (var ms = new MemoryStream())
                {
                    await rawStream.CopyToAsync(ms);

                    var buffer = new byte[ms.Length];
                    var msBuffer = ms.GetBuffer();
                    for(int i = 0; i < ms.Length; ++i)
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
