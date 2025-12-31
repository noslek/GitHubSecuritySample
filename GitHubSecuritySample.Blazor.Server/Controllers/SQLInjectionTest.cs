using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using GitHubSecuritySample.Module.BusinessObjects;

namespace GitHubSecuritySample.Blazor.Server.Controllers
{
    public class SQLInjectionTest : ViewController
    {
        public void UnsafeSearch(string userInput)
        {
            // BAD: Direct string concatenation in SQL
            string query = "SELECT * FROM ApplicationUser WHERE Name = '" + userInput + "'";
            var result = ObjectSpace.GetObjects<ApplicationUser>(query);
        }
    }
}
