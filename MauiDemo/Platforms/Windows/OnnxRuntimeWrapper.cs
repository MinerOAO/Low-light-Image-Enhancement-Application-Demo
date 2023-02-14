using Microsoft.ML.OnnxRuntime;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
{
    public partial class OnnxRuntimeWrapper
    {
        partial void Run(List<NamedOnnxValue> inputData)
        {
            var options = new SessionOptions();
            options.AddSessionConfigEntry("enable_profiling", "true");
            options.AppendExecutionProvider_CPU();

            //https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html#configuration-options
            //The DirectML execution provider does not support the use of memory pattern optimizations or parallel execution in onnxruntime.
            //Additionally, as the DirectML execution provider does not support parallel execution,
            //it does not support multi-threaded calls to Run on the same inference session.
            //should be disabled

            //WIP : System.EntryPointNotFoundException
            //options.AppendExecutionProvider_CUDA();
            //options.AppendExecutionProvider_DML();

            options.EnableMemoryPattern = false;
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;

            try
            {
                //Must: model(binary or string path)
                //Optional: session weights
                //DIRECTML Package issue : https://github.com/microsoft/onnxruntime/issues/13429
                using var _ort = new InferenceSession(Model, options);
                // WIP:
                var result = _ort.Run(inputData);
                //result.AsEnumerable<float>().ToArray()
                outputData = result.First().AsEnumerable<float>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return;
        }
    }
}
