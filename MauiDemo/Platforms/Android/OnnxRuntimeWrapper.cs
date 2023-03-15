using Microsoft.ML.OnnxRuntime;

namespace MauiDemo.Models.Interface.OnnxRuntimeWrapper
{
    public partial class OnnxRuntimeWrapper
    {
        partial void Run(List<NamedOnnxValue> inputData, ref int sessionID)
        {
            var options = new SessionOptions();
            options.AddSessionConfigEntry("enable_profiling", "true");
            //options.AppendExecutionProvider_Nnapi();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            try
            {
                using (var _ort = new InferenceSession(Model, options))
                {
                    using (var result = _ort.Run(inputData))
                    {
                        sessionID = _ort.GetHashCode();
                        outputData.Add(sessionID, result.First().AsEnumerable<float>().ToList());
                    }
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
        }
    }
}
