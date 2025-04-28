using System.Diagnostics;

namespace LLIEDOTNET8.Models.Interface.PickPageModel
{
    public partial class PickPageModel
    {
        public partial Task<FileResult> PickImage()
        {
            //PickOptions options = PickOptions.Images;
            PickOptions options = new PickOptions()
            {
                PickerTitle = "Pick a image",
                FileTypes = customFileType
            };
            try
            {
                var result = FilePicker.PickAsync(options);
                if (result != null)
                {
                    return result;
                }

                return null;
            }
            catch (Exception exceptions)
            {
                // To MessageSys
                Console.WriteLine(exceptions.Message);
            }
            return null;
        }
        public partial Task<FileResult> ShotPhoto()
        {
            Task<FileResult> photo = null;
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                return photo;
            }
            try
            {
                photo = MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                    return photo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return photo;
            }
            return photo;
        }
    }
}
