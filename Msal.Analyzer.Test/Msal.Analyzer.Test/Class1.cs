using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Msal.Analyzer.Test
{
    public class Class1
    {
        void Method(bool flag, int value)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString()).WithClientSecret("someSecret").Build();
            _ = app.GetAuthorizationRequestUrl(new[] { "" }).ExecuteAsync().Result;
            //app.UserTokenCache.SetBeforeAccess(null);
        }
    }
}
