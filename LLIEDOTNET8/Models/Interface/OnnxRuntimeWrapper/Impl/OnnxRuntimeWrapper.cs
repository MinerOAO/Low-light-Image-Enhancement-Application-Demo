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
    }
}
