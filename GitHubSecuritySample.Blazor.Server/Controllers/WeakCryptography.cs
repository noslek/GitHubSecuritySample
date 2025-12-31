using System.Security.Cryptography;
using System.Text;

namespace GitHubSecuritySample.Blazor.Server.Controllers
{
    public class WeakCryptography
    {
        public byte[] HashPassword(string password)
        {
            // BAD: MD5 is cryptographically weak
            using (var md5 = MD5.Create())
            {


                return md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
