using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
{
    public partial class OnnxRuntimeWrapper
    {
        private OnnxRuntimeWrapper() { }
        private OnnxRuntimeWrapper(byte[] model) 
        {
            _model = model;
        }
        public async static Task<OnnxRuntimeWrapper> LoadModel(string modelName)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(modelName);
            var data = new byte[stream.Length];
            await stream.ReadAsync(data);
            var instance = new OnnxRuntimeWrapper(data);
            return instance;
        }
        //Global Implement
        private partial DenseTensor<float> ToRGBTensor(Image<Rgb24> RGBImage)
        {
            //WIP
            var input_tensor = new DenseTensor<float>(new[] { 1, 3, RGBImage.Width, RGBImage.Height });
            Console.WriteLine($"{RGBImage.Width}, {RGBImage.Height}");
            //https://docs.sixlabors.com/articles/imagesharp/pixelbuffers.html
            RGBImage.ProcessPixelRows(accessor =>
            {
                Console.WriteLine($"{accessor.Width}, {accessor.Height}");
                for (int y = 0; y < accessor.Height; ++y)
                {
                    Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; ++x)
                    {
                        ref var rgbColor = ref pixelRow[x];
                        input_tensor[0, 0, x, y] = rgbColor.R / 255.0f;
                        input_tensor[0, 1, x, y] = rgbColor.G / 255.0f;
                        input_tensor[0, 2, x, y] = rgbColor.B / 255.0f;
                    }
                }
            });
            return input_tensor;
        }
        private async partial void TensorResultToJPEG(IEnumerable<float> outputData)
        {
            var imageArray = outputData.Select(x => (byte)(x * 255.0f)).ToArray();
            Console.WriteLine($"{imageArray.Length}");
            //RGBImage.SaveAsJpeg("origin.jpg");
            //WIP 宽高数据
            //需要定制修改pytorch模型输出形状进行优化
            var RGBImage = new Image<Rgb24>(Width, Height);
            RGBImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; ++y)
                {
                    Span<Rgb24> pixelRow = accessor.GetRowSpan(y);

                    int factor = accessor.Height * accessor.Width;

                    for (int x = 0; x < accessor.Width; ++x)
                    {
                        //WIP
                        ref var pixel = ref pixelRow[x];
                        // 不会越界?
                        int arrayPointer = accessor.Height * x + y;
                        pixel.R = imageArray[arrayPointer];
                        pixel.G = imageArray[arrayPointer + factor];
                        pixel.B = imageArray[arrayPointer + 2 * factor];

                    }
                }
            });

            string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, "Test_01.jpg");
            using FileStream fileStream = System.IO.File.OpenWrite(targetFile);
            await RGBImage.SaveAsJpegAsync(fileStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
            {
                Quality = _quality
            });
        }
        public partial void StartInference(ref Image<Rgb24> RGBImage, float gamma, float strength, int quality)
        {
            _width = RGBImage.Width;
            _height = RGBImage.Height;
            _quality = quality;

            Tensor<float> gammaTensor, strengthTensor;

            gammaTensor = new DenseTensor<float>(1);
            gammaTensor.SetValue(0, gamma);

            strengthTensor = new DenseTensor<float>(1);

            strengthTensor.SetValue(0, strength);

            //You can create an implicitly-typed array in which
            //the type of the array instance is inferred from
            //the elements specified in the array initializer.
            var inputTensor = ToRGBTensor(RGBImage);


            //Console.WriteLine($"{gamma.Dimensions.ToString()},{gamma.Length}, {gamma.GetValue(0)}");
            //Console.WriteLine($"{strength.Dimensions.ToString()},{strength.Length}, {strength.GetValue(0)}");
            //Console.WriteLine($"{input_image.Dimensions.ToString()},{input_image.Length}");

            NamedOnnxValue onnxGamma = NamedOnnxValue.CreateFromTensor<float>("gamma", gammaTensor);
            NamedOnnxValue onnxStrength = NamedOnnxValue.CreateFromTensor<float>("strength", strengthTensor);
            NamedOnnxValue onnxImage = NamedOnnxValue.CreateFromTensor<float>("input_image", inputTensor);
            Console.WriteLine("Data Loaded!");

            Run(new List<NamedOnnxValue>()
            {
                onnxImage,
                onnxGamma,
                onnxStrength,
            });

            TensorResultToJPEG(outputData);
        }

    }
}
