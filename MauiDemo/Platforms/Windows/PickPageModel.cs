using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

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
                var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
                _image = image.CloneAs<Rgb24>();
                StateV2.ModelState = InternalState.ImageLoaded;
            }
            return;
        }
        private Image<Rgb24> FitImageSharp()
        {
            var image = _image.CloneAs<Rgb24>();
            if (IsPreViewDownSample)
            {
                if (_image.Width > _image.Height)
                    image.Mutate(x => x.Resize(256, _image.Height / _image.Width * 256));
                else
                    image.Mutate(x => x.Resize(_image.Width / _image.Height * 256, 256));
            }
            else if (IsDownSample)
            {
                image.Mutate(x => x.Resize(_image.Width / 2, _image.Height / 2));
            }
            int actualFactor = _internalCropFactor * _externalCropFactor;
            int actualWidth = image.Width - image.Width % actualFactor;
            int actualHeight = image.Height - image.Height % actualFactor;
            image.Mutate(x => x.Crop(actualWidth, actualHeight));
            return image;
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
                    using (var fittImage = FitImageSharp())
                    {
                        var ortWrapper = await OnnxRuntimeWrapper.OnnxRuntimeWrapper.LoadModel();
                        // 量化时，注意onnxruntime的python版本与C# nupackage版本中opset算子版本
                        // x86-64 with VNNI, GPU with Tensor Core int8 support and ARM with dot-product instructions can get better performance in general.
                        //var ort = await OnnxRuntimeWrapper.LoadModel("Bread_onnx_optimized_dynamic_quantized.onnx");
                        resultName = await ortWrapper.StartInference(fittImage, Gamma, Strength, Quality, Type);
                    }
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
