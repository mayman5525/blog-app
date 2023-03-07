namespace Blog.Data.FileManager
{
    public interface IFileManager
    {

        FileStream ImageStream(string type,string image);
        Task<string> SaveImage(string type, IFormFile image);
        bool RemoveImage(string type, string image);
    }
}