using PhotoSauce.MagicScaler;

namespace Blog.Data.FileManager
{
    public class FileManager : IFileManager
    {
        private string _imagePath;

        public FileManager(IConfiguration config)
        {
            _imagePath = config["Path:Images"];
        }

        public FileStream ImageStream(string type, string image)
        {
            return new FileStream(Path.Combine(_imagePath, type, image), FileMode.Open, FileAccess.Read);
        }

        public bool RemoveImage(string type, string image)
        {
            try
            {
                var file = Path.Combine(_imagePath, type, image);
                if (File.Exists(file))
                    File.Delete(file);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public async Task<string> SaveImage(string type, IFormFile image)
        {
            try
            {
                var save_path = Path.Combine(_imagePath, type);
                if (!Directory.Exists(save_path))
                {
                    Directory.CreateDirectory(save_path);
                }

                //Internet Explorer Error C:/User/Foo/image.jpg
                //var fileName = image.FileName;
                var mime = image.FileName.Substring(image.FileName.LastIndexOf('.'));
                var fileName = $"img_{DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}{mime}";
                var filePath = Path.Combine(save_path, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    MagicImageProcessor.ProcessImage(image.OpenReadStream(), fileStream, ImageOptions());
                }


                return filePath;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "Error";
            }
        }

        private ProcessImageSettings ImageOptions() => new ProcessImageSettings
        {
            Width = 800,
            Height = 500,
            /*ResizeMode = CropScaleMode.Crop,
            SaveFormat = FileFormat.Jpeg,
            JpegQuality = 100,
            JpegSubsampleMode = ChromaSubsampleMode.Subsample420*/
        };
    }
}