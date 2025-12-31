namespace GitHubSecuritySample.Blazor.Server.Controllers
{
    public class HardcodedCredentials
    {
        // BAD: Hardcoded password
        private string dbPassword = "SuperSecret123!";
        private string apiKey = "sk-1234567890abcdef";

        public string GetConnectionString()
        {
            return $"Server=myserver;Password={dbPassword}";
        }
    }
}
