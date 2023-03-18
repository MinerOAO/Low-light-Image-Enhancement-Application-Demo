using Microsoft.ML.OnnxRuntime;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
{
    public partial class OnnxRuntimeWrapper
    {
        partial void Run(List<NamedOnnxValue> inputData, ref int sessionID)
        {
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
                    using (var result = _session.Run(inputData))
                    {
                        sessionID = _session.GetHashCode();
                        outputData.Add(sessionID, result.First().AsEnumerable<float>().ToList());
                    }
                    //result.AsEnumerable<float>().ToArray()
                }
            }
            catch(OnnxRuntimeException ortEx)
            {
                Console.WriteLine(ortEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
