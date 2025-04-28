using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace LLIEDOTNET8.Models.Interface.OnnxRuntimeWrapper
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
            //https://docs.sixlabors.com/articles/imagesharp/gettingstarted.html#performance
            //ImageSharp performs well with MAUI on both iOS and Android in release mode when correctly configured.
            //For Android we recommend enabling LLVM and AOT compilation in the project file:
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
            //Rectangle坐标系原点位于左下BottomLeft
            if (outputData.TryGetValue(sessionID, out var data))
            {
                var imageArray = data.Select(x => (byte)(x * 255.0f)).ToArray();
                RGBImage.ProcessPixelRows(accessor =>
                {
                    for (int y = drawArea.Top; y < drawArea.Bottom; ++y)
                    {
                        Span<Rgb24> pixelRow = accessor.GetRowSpan(y);

                        int stride = Math.Abs(drawArea.Width * drawArea.Height);

                        for (int x = drawArea.Left; x < drawArea.Right; ++x)
                        {
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
        private async Task<string> SaveCanvasToImageFile(Image<Rgb24> RGBImage)
        {
            string imgName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
            try
            {
                string targetFile = System.IO.Path.Combine(FileSystem.Current.CacheDirectory, imgName);
                using FileStream fileStream = System.IO.File.OpenWrite(targetFile);
                await RGBImage.SaveAsJpegAsync(fileStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
                {
                    Quality = _quality
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Debug.WriteLine(e);
            }
            return imgName;
        }
        private partial int Run(ref Tensor<float> gammaTensor, ref Tensor<float> strengthTensor, ref DenseTensor<float> inputTensor)
        {
            int sessionID = -1;

            NamedOnnxValue onnxGamma = NamedOnnxValue.CreateFromTensor<float>("gamma", gammaTensor);
            NamedOnnxValue onnxStrength = NamedOnnxValue.CreateFromTensor<float>("strength", strengthTensor);
            NamedOnnxValue onnxImage = NamedOnnxValue.CreateFromTensor<float>("input_image", inputTensor);

            var options = new SessionOptions();
            options.AddSessionConfigEntry("enable_profiling", "true");
            //options.AppendExecutionProvider_CPU();

            //Memory Consumption ?

            //options.ExecutionMode = ExecutionMode.ORT_PARALLEL;
            // NNAPI and CoreML do not support dynamic shapes
            /*
             * The .GPU package is large due to it containing CUDA kernels. Note that it is CUDA specific and not a generic package that works with any GPU.
             * Because the GPU package is large, it is not likely someone would want to use that on a mobile device, 
             * hence we only include the NNAPI execution provider in the much smaller CPU package.
             * The ORT NNAPI execution provider converts the ONNX model to an NNAPI model at runtime, and the NNAPI implementation on the device executes that NNAPI model. 
             * The NNAPI implementation on the device can potentially use GPU or NPU, but that implementation is device specific and varies by manufacturer. It also depends on the model as to how much of it can be run using NNAPI. 
             * The usability checker mentioned in https://onnxruntime.ai/docs/reference/mobile/helpers.html can give an idea about that.
             * skottmckay 2022-07-11
             */

            //https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html#configuration-options
            //The DirectML execution provider does not support the use of memory pattern optimizations or parallel execution in onnxruntime.
            //Additionally, as the DirectML execution provider does not support parallel execution,
            //it does not support multi-threaded calls to Run on the same inference session.
            //should be disabled

            //WIP : System.EntryPointNotFoundException
            //options.AppendExecutionProvider_CUDA();
            //options.AppendExecutionProvider_DML();

            //options.EnableMemoryPattern = false;
            //options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            try
            {
                //Must: model(binary or string path)
                //Optional: session && weights
                //DIRECTML Package issue : https://github.com/microsoft/onnxruntime/issues/13429
                using (_session = new InferenceSession(Model, options))
                {
                    // WIP:
                    using (var result = _session.Run(new List<NamedOnnxValue>(){
                            onnxImage, onnxGamma, onnxStrength
                    }))
                    {
                        sessionID = _session.GetHashCode();
                        outputData.Add(sessionID, result.First().AsEnumerable<float>().ToList());
                    }
                    //result.AsEnumerable<float>().ToArray()
                }
            }
            catch (OnnxRuntimeException ortEx)
            {
                Console.WriteLine(ortEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return sessionID;
        }
        public async partial Task<string> StartInference(Image<Rgb24> RGBImage, float gamma, float strength, int quality, InferenceType type)
        {
            string resultImgName = null;

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

                                        int sessionID = -1;
                                        sessionID = Run(ref gammaTensor, ref strengthTensor, ref inputTensor);

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
                            resultImgName = await SaveCanvasToImageFile(imageCanvas);
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

                        int sessionID = -1;
                        sessionID = Run(ref gammaTensor, ref strengthTensor, ref inputTensor);

                        using (var imageCanvas = new Image<Rgb24>(_width, _height))
                        {
                            WriteTensorResultToCanvas(sessionID, imageCanvas,
                                new Rectangle(0, 0, _width, _height));
                            resultImgName = await SaveCanvasToImageFile(imageCanvas);
                        }
                        break;
                    }
            }
            return resultImgName;
        }
    }
}
