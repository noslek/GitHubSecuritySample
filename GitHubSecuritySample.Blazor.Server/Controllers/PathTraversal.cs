namespace GitHubSecuritySample.Blazor.Server.Controllers
{
    public class PathTraversal
    {
        public void DownloadFile(string fileName)
        {
            // BAD: User input used directly in file path
            string path = Path.Combine("C:\\Reports", fileName);
            byte[] content = File.ReadAllBytes(path);
        }
    }
}
