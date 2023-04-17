using Android.Graphics;
using Android.Opengl;
using CommunityToolkit.Maui.Storage;
using Java.Security.Cert;
using SkiaSharp;
using static Android.InputMethodServices.Keyboard;

namespace MauiDemo.Models.Interface.PickPageModel
{
    public partial class PickPageModel
    {
        public async partial Task LoadToRgb24(Stream stream, string fileName)
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
            }
            else
            {
                //ImageSharp needs to pre-seed generics to
                //avoid "Attempting to JIT compile method... while running in aot-only mode."
                //https://github.com/dotnet/runtime/issues/71210
                //https://github.com/dotnet/runtime/issues/52559
                //LLVM ENABLED
                await Task.Run(() =>
                {
                    var image = SKBitmap.Decode(stream);
                    _image = image.Copy();
                });
                StateV2.ModelState = InternalState.ImageLoaded;
            }
            return;
        }
        private SKBitmap FitSKBitmap()
        {
            var image = _image.Copy();

            if(_isDownSample)
                image = image.Resize(new SKSizeI(_image.Width / 2, _image.Height / 2), SKFilterQuality.High);

            int actualFactor = _internalCropFactor * _externalCropFactor;
            int actualWidth = image.Width - image.Width % actualFactor;
            int actualHeight = image.Height - image.Height % actualFactor;

            if(actualWidth == image.Width && actualHeight == image.Height)
            {
                return image;
            }

            SKBitmap cropped = new SKBitmap(actualWidth, actualHeight);
            unsafe
            {
                IntPtr sourceAddr = image.GetPixels();
                uint* sourcePtr = (uint*)sourceAddr.ToPointer();

                IntPtr destAddr = cropped.GetPixels();
                uint* destPtr = (uint*)destAddr.ToPointer();
                for (int y = 0; y < image.Height; ++y)
                {
                    for (int x = 0; x < image.Width; ++x)
                    {
                        if (x < actualWidth && y < actualHeight)
                        {
                            *destPtr = *sourcePtr;
                            ++destPtr;
                        }
                        ++sourcePtr;
                    }
                }
            }

            return cropped;
        }
        public async partial Task<string> Inference()
        {
            if (StateV2.ModelState != InternalState.ImageLoaded)
                return null;
            StateV2.ModelState = InternalState.Inferencing;
            string resultName = null;

            try
            {
                await Task.Run(async () =>
                {
                    var fittImage = FitSKBitmap();
                    var ortWrapper = await OnnxRuntimeWrapper.OnnxRuntimeWrapper.LoadModel("Bread_onnx_fullres_new_test.onnx");
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
}
