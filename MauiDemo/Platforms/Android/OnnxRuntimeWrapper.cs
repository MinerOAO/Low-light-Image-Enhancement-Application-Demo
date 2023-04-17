using Android.Util;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using System.Diagnostics;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
{
    public partial class OnnxRuntimeWrapper
    {
        private partial int Run(ref Tensor<float> gammaTensor, ref Tensor<float> strengthTensor, ref DenseTensor<float> inputTensor)
        {
            int sessionID = -1;

            NamedOnnxValue onnxGamma = NamedOnnxValue.CreateFromTensor<float>("gamma", gammaTensor);
            NamedOnnxValue onnxStrength = NamedOnnxValue.CreateFromTensor<float>("strength", strengthTensor);
            NamedOnnxValue onnxImage = NamedOnnxValue.CreateFromTensor<float>("input_image", inputTensor);

            var options = new SessionOptions();
            options.AddSessionConfigEntry("enable_profiling", "true");
            //options.AppendExecutionProvider_Nnapi();
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
        //https://learn.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/bitmaps/pixel-bits
        private uint MakePixel(byte red, byte green, byte blue, byte alpha) =>
        (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
        private DenseTensor<float> ToRGBTensor(SKBitmap RGBImage)
        {
            var input_tensor = new DenseTensor<float>(new[] { 1, 3, RGBImage.Width, RGBImage.Height });
            Console.WriteLine($"{RGBImage.Width}, {RGBImage.Height}");
            //https://docs.sixlabors.com/articles/imagesharp/pixelbuffers.html
            unsafe
            {
                IntPtr pixelsAddr = RGBImage.GetPixels();
                byte* ptr = (byte*)pixelsAddr.ToPointer();

                for (int y = 0; y < RGBImage.Height; ++y)
                {
                    for (int x = 0; x < RGBImage.Width; ++x)
                    {
                        input_tensor[0, 0, x, y] = (*ptr++) / 255.0f;
                        input_tensor[0, 1, x, y] = (*ptr++) / 255.0f;
                        input_tensor[0, 2, x, y] = (*ptr++) / 255.0f;
                        ptr++;
                    }
                }
            }
            return input_tensor;
        }
        private void WriteTensorResultToCanvas(int sessionID, SKBitmap RGBImage, 
            int upLeftX, int upLeftY, int width, int height)
        {
            //stopwatch
            //Rectangle坐标系原点位于左下,
            if (outputData.TryGetValue(sessionID, out var data))
            {
                var imageArray = data.Select(x => (byte)(x * 255.0f)).ToArray();

                SKColor[] pixels = RGBImage.Pixels;

                int stride = width * height;
                for (int y = upLeftY; y < upLeftY + height; ++y)
                {
                    for (int x = upLeftX; x < upLeftX + width; ++x)
                    {
                        int arrayPointer = height * (x - upLeftX) + (y - upLeftY);
                        pixels[width * y + x] = new SKColor(imageArray[arrayPointer],
                            imageArray[arrayPointer + stride],
                            imageArray[arrayPointer + 2 * stride],
                            0xFF);
                    }
                }
                RGBImage.Pixels = pixels;
            }
            else throw new Exception();
        }
        private async Task<string> SaveCanvasToImageFile(SKBitmap RGBImage)
        {
            string imgName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
            try
            {
                string targetFile = System.IO.Path.Combine(FileSystem.Current.CacheDirectory, imgName);
                using FileStream fileStream = System.IO.File.OpenWrite(targetFile);
                await Task.Run(() =>
                {
                    RGBImage.Encode(fileStream, SKEncodedImageFormat.Jpeg, _quality);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Debug.WriteLine(e);
            }
            return imgName;
        }
        public async partial Task<string> StartInference(SKBitmap RGBImage, float gamma, float strength, int quality, InferenceType type = InferenceType.Entire)
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

                        using (var imageCanvas = new SKBitmap(_width, _height))
                        {
                            WriteTensorResultToCanvas(sessionID, imageCanvas,
                                0, 0, _width, _height);
                            resultImgName = await SaveCanvasToImageFile(imageCanvas);
                        }
                        break;
                    }
            }
            return resultImgName;
        }
    }
}
