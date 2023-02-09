using DeepLDemo;
using ImageProcess.ImageSharp;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageProcess.Custom
{
    public static class CustomRGBYCbCr
    {
        private const byte MAX_RGB = 255;
        private const byte MIN_RGB = 0;
        private const float MAX_Y = 255.0f;
        private const float MIN_Y = 0.0f;
        private const float MAX_CBCR = 127.0f;
        private const float MIN_CBCR = -128.0f;
        public struct RGB
        {
            //internal storage
            private byte r;
            private byte g;
            private byte b;

            public byte R 
            { 
                get { return r; } 
                set { r = ValueVerify(value); } 
            }
            public byte G 
            { 
                get { return g; } 
                set { g = ValueVerify(value); } 
            }
            public byte B 
            { 
                get { return b; } 
                set { b = ValueVerify(value); } 
            }

            private static byte ValueVerify(byte value) 
            { 
                if(value < MIN_RGB)
                {
                    value = MIN_RGB;
                    return value;
                }
                else if(value > MAX_RGB)
                {
                    value = MAX_RGB;    
                    return value;
                }
                return value;
            }

            public RGB(byte rr, byte gg, byte bb) 
            { 
                this.r = rr;
                this.g = gg;
                this.b = bb;    
            }
            public RGB(): this(0, 0, 0) { }
        }

        public struct YCbCr
        {
            private float y;
            private float cb;
            private float cr;

            public float Y {
                get { return y; } 
                set { y = ValueVerify(MIN_Y, value, MAX_Y); } 
            }
            public float Cb {
                get { return cb; } 
                set { cb = ValueVerify(MIN_CBCR, value, MAX_CBCR); } 
            }
            public float Cr {
                get { return cr; } 
                set { cr = ValueVerify(MIN_CBCR, value, MAX_CBCR); } 
            }

            private static float ValueVerify(float min, float value, float max)
            {
                if(value < min)
                {
                    value = min;
                    return value;
                }
                if(value > max)
                {
                    value = max;    
                    return value;   
                }
                return value;
            }
            public YCbCr(float yy, float cbcb, float crcr)
            {
                this.y = yy;
                this.cb = crcr;
                this.cr = crcr;
            }
            public YCbCr() : this(0.0f, 0.0f, 0.0f){ }

        }
        public static void FileReader()
        {
            return;
        }
        public static YCbCr RGBToYCbCr(RGB rgbColor)
        {
            YCbCr result = new YCbCr();

            float clamp_r = rgbColor.R/ MAX_RGB;
            float clamp_g = rgbColor.G/ MAX_RGB;
            float clamp_b = rgbColor.B/ MAX_RGB;    

            //paralize
            result.Y = 0.29900f * clamp_r + 0.58700f * clamp_g + 0.11400f * clamp_b;
            result.Cb = -0.16874f * clamp_r + -0.33126f * clamp_g + 0.50000f * clamp_b;
            result.Cr = 0.5000f * clamp_r + -0.41869f * clamp_g + -0.08131f * clamp_b;

            return result;
        }
        public static RGB YCbCrTORGB(YCbCr ycbcrColor)
        {
            RGB result = new RGB();

            


            return result;
        }
    }

}
namespace ImageProcess.ImageSharp
{
    public class ImageLoader
    {
        private Image<Rgb24> _rgbImage;

        public Image<Rgb24> RGBImage { get { return _rgbImage; } private set { } }
        public ImageLoader(string filepath)
        {
            _rgbImage = Image.Load<Rgb24>(filepath);
            int actualWidth = _rgbImage.Width;
            int actualHeight = _rgbImage.Height;    
            if(_rgbImage.Width % 2 != 0)
                actualWidth = _rgbImage.Width - 1;
            if (_rgbImage.Height % 2 != 0)
                actualHeight = _rgbImage.Height - 1;
            _rgbImage.Mutate(x => x.Crop(actualWidth, actualHeight));
        }
        public DenseTensor<float> ToYCbCrTensor()
        {
            //WIP
            var input_tensor = new DenseTensor<float>(new[] { 1, 3, RGBImage.Width, RGBImage.Height });
            ColorSpaceConverter converter = new ColorSpaceConverter();
            //https://docs.sixlabors.com/articles/imagesharp/pixelbuffers.html
            RGBImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; ++y)
                {
                    Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; ++x)
                    {
                        Rgb rgbColor = pixelRow[x];
                        YCbCr ycbcrColor = converter.ToYCbCr(rgbColor);
                        input_tensor[0, 0, x, y] = ycbcrColor.Y;
                        input_tensor[0, 1, x, y] = ycbcrColor.Cb;
                        input_tensor[0, 2, x, y] = ycbcrColor.Cr;
                    }
                }
            });
            return input_tensor;
        }
        public DenseTensor<float> ToRGBTensor()
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
        public void TensorResultToJPEG(IEnumerable<float> outputData)
        {
            var imageArray = outputData.Select(x => (byte)(x * 255.0f)).ToArray();
            Console.WriteLine($"{imageArray.Length}");
            //RGBImage.SaveAsJpeg("origin.jpg");
            //WIP 宽高数据
            //需要定制修改pytorch模型输出形状进行优化
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
            RGBImage.SaveAsJpeg("Test.jpg");
        }
    }
}
namespace DeepLDemo
{
    class Demo 
    {
        private byte[] _model;
        public byte[] Model { get { return _model; } private set { } }

        public Demo(string modelPath) : this(File.ReadAllBytes(modelPath)){ }
        public Demo(byte[] model)
        {
            this._model = model;
            
        }
        public IEnumerable<float> Run(List<NamedOnnxValue> inputData)
        {
            var options = new SessionOptions();
            options.AddSessionConfigEntry("enable_profiling", "true");
            options.AppendExecutionProvider_CPU();

            //https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html#configuration-options
            //The DirectML execution provider does not support the use of memory pattern optimizations or parallel execution in onnxruntime.
            //Additionally, as the DirectML execution provider does not support parallel execution,
            //it does not support multi-threaded calls to Run on the same inference session.
            //should be disabled
            options.AppendExecutionProvider_DML();
            options.EnableMemoryPattern = false;
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;

            try
            {
                //Must: model(binary or string path)
                //Optional: session weights
                //DIRECTML Package issue : https://github.com/microsoft/onnxruntime/issues/13429
                using var _ort = new InferenceSession(Model, options);
                // WIP:
                foreach (var result in _ort.Run(inputData))
                {
                    //result.AsEnumerable<float>().ToArray()
                    return result.AsEnumerable<float>();
                }
               
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.ToString());
            }
            return null;
        }
    }

}
namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Demo test = new Demo("C:\\Users\\zi200\\source\\repos\\ConsoleApp2\\ConsoleApp2\\Bread_onnx_optimized.onnx");

            Tensor<float> gamma, strength;
            Tensor<float> input_image;

            gamma = new DenseTensor<float>(1);
            gamma.SetValue(0, 1.0f);

            strength = new DenseTensor<float>(1);

            strength.SetValue(0, 0.01f);

            //You can create an implicitly-typed array in which
            //the type of the array instance is inferred from
            //the elements specified in the array initializer.
            var image_loader = new ImageLoader("C:\\Users\\zi200\\source\\repos\\ConsoleApp2\\ConsoleApp2\\2.jpg");
            input_image = image_loader.ToRGBTensor();


            Console.WriteLine($"{gamma.Dimensions.ToString()},{gamma.Length}, {gamma.GetValue(0)}");
            Console.WriteLine($"{strength.Dimensions.ToString()},{strength.Length}, {strength.GetValue(0)}");
            Console.WriteLine($"{input_image.Dimensions.ToString()},{input_image.Length}");

            NamedOnnxValue onnxGamma = NamedOnnxValue.CreateFromTensor<float>("gamma", gamma);
            NamedOnnxValue onnxStrength = NamedOnnxValue.CreateFromTensor<float>("strength", strength);
            NamedOnnxValue onnxImage = NamedOnnxValue.CreateFromTensor<float>("input_image", input_image);

            Console.WriteLine("Data Loaded!");

            image_loader.TensorResultToJPEG(test.Run(new List<NamedOnnxValue>()
            {
                onnxImage,
                onnxGamma,
                onnxStrength,
            }));

        }
    }
}