using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
{
    public partial class OnnxRuntimeWrapper
    {
        private OnnxRuntimeWrapper() { }
        private OnnxRuntimeWrapper(byte[] model) 
        {
            _model = model;
        }
        //Global Implement
        private DenseTensor<float> ToRGBTensor(Image<Rgb24> RGBImage)
        {
            //WIP
            var input_tensor = new DenseTensor<float>(new[] { 1, 3, RGBImage.Width, RGBImage.Height });
            Console.WriteLine($"{RGBImage.Width}, {RGBImage.Height}");
            //https://docs.sixlabors.com/articles/imagesharp/pixelbuffers.html
            RGBImage.ProcessPixelRows(accessor =>
            {
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
        private void WriteTensorResultToCanvas(int sessionID, Image<Rgb24> RGBImage, Rectangle drawArea)
        {
            //Rectangle坐标系原点位于左下,
            if (outputData.TryGetValue(sessionID, out var data))
            {
                var imageArray = data.Select(x => (byte)(x * 255.0f)).ToArray();
                //RGBImage.SaveAsJpeg("origin.jpg");
                //WIP 宽高数据
                //需要定制修改pytorch模型输出形状进行优化
                RGBImage.ProcessPixelRows(accessor =>
                {
                    for (int y = drawArea.Top; y < drawArea.Bottom; ++y)
                    {
                        Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                        
                        int stride = Math.Abs(drawArea.Width * drawArea.Height);

                        for (int x = drawArea.Left; x < drawArea.Right; ++x)
                        {
                            //WIP
                            ref var pixel = ref pixelRow[x];
                            // 小画布，减去Left&&Top从0,0开始
                            int arrayPointer = Math.Abs(drawArea.Height) * (x - drawArea.Left) + (y - drawArea.Top);
                            pixel.R = imageArray[arrayPointer];
                            pixel.G = imageArray[arrayPointer + stride];
                            pixel.B = imageArray[arrayPointer + 2 * stride];
                        }
                    }
                });
            }
            else throw new Exception();
        }
        private async Task SaveCanvasToImageFile(Image<Rgb24> RGBImage)
        {
            try
            {
                string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg");
                using FileStream fileStream = System.IO.File.OpenWrite(targetFile);
                await RGBImage.SaveAsJpegAsync(fileStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
                {
                    Quality = _quality
                });
            }
            catch(Exception e) 
            {
                Console.WriteLine(e.ToString());
                Debug.WriteLine(e);
            }
        }
        public async partial Task StartInference(Image<Rgb24> RGBImage, float gamma, float strength, int quality, InferenceType type)
        {
            _width = RGBImage.Width;
            _height = RGBImage.Height;
            _quality = quality;

            Tensor<float> gammaTensor, strengthTensor;

            gammaTensor = new DenseTensor<float>(1);
            gammaTensor.SetValue(0, gamma);

            strengthTensor = new DenseTensor<float>(1);

            strengthTensor.SetValue(0, strength);


            switch (type)
            {
                case InferenceType.Split:
                    {
                        int factor = 2;
                        int startX = 0; 
                        int startY = 0;
                        int endX = _width / factor;
                        int endY = _height / factor;
                        using (var imageCanvas = new Image<Rgb24>(_width, _height))
                        {
                            while (startY < _height)
                            {
                                while (startX < _width)
                                {
                                    var cropRect = new Rectangle(startX, startY, endX - startX, endY - startY);
                                    using (var cropImage = RGBImage.Clone())
                                    {
                                        cropImage.Mutate(x => x.Crop(cropRect));

                                        var inputTensor = ToRGBTensor(cropImage);

                                        NamedOnnxValue onnxGamma = NamedOnnxValue.CreateFromTensor<float>("gamma", gammaTensor);
                                        NamedOnnxValue onnxStrength = NamedOnnxValue.CreateFromTensor<float>("strength", strengthTensor);
                                        NamedOnnxValue onnxImage = NamedOnnxValue.CreateFromTensor<float>("input_image", inputTensor);

                                        int sessionID = -1;
                                        Run(new List<NamedOnnxValue>()
                                        {
                                            onnxImage,
                                            onnxGamma,
                                            onnxStrength,
                                        }, ref sessionID);
                                        WriteTensorResultToCanvas(sessionID, imageCanvas, cropRect);
                                    }
                                    
                                    startX = endX;
                                    endX += _width / factor;
                                }
                                startX = 0;
                                endX = _width / factor;
                                startY = endY;
                                endY += _height / factor;
                            }
                            await SaveCanvasToImageFile(imageCanvas);
                        }
                        break;
                    }
                case InferenceType.Entire:
                default:
                    {
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

                        int sessionID = -1;
                        Run(new List<NamedOnnxValue>()
                        {
                            onnxImage,
                            onnxGamma,
                            onnxStrength,
                        }, ref sessionID);

                        using (var imageCanvas = new Image<Rgb24>(_width, _height))
                        {
                            WriteTensorResultToCanvas(sessionID, imageCanvas, 
                                new Rectangle(0, 0, _width, _height));
                            await SaveCanvasToImageFile(imageCanvas);
                        }
                        break;
                    }
            }

        }
    }
}
