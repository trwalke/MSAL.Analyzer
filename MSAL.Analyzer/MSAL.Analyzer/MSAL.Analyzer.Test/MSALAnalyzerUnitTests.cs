using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = MSAL.Analyzer.Test.CSharpCodeFixVerifier<
    MSAL.Analyzer.MSALAnalyzerAnalyzer,
    MSAL.Analyzer.MSALAnalyzerCodeFixProvider>;

namespace MSAL.Analyzer.Test
{
    [TestClass]
    public class MSALAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Microsoft.Identity.Client;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   void Method(bool flag, int value)
            {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString()).WithClientSecret(string.Empty).Build();
            var uri = app.GetAuthorizationRequestUrl(new[] { "" }).ExecuteAsync().Result;
            app.UserTokenCache.SetAfterAccess(null);
            }
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("MSALAnalyzer").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
