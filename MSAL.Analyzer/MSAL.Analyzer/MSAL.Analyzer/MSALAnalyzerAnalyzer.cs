using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MSAL.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MSALAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MSALAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            /*Issues
             How do I get the name and location of the variable in the InvocationExpressionSyntax/MemberAccessExpressionSyntax?
             Why does the analyzer not detect the usage of SetBeforeAccess?
             */
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction((compilationStartContext) =>
            {
                compilationStartContext.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockContext =>
                {
                    // We only care about method bodies.
                    if (codeBlockContext.OwningSymbol.Kind != SymbolKind.Method) return;
                    var method = (IMethodSymbol)codeBlockContext.OwningSymbol;

                    var CreateMethod = "Create";
                    var SetBeforeAccessMethod = "SetBeforeAccess";
                    bool CreateMethodInvocationFound = false;
                    bool setBeforeOrAfterAccessInvocationFound = false;
                    MemberAccessExpressionSyntax targetInvocation = null;

                    codeBlockContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
                    {
                        var BuilderType = compilationStartContext?.Compilation?.GetTypeByMetadataName("Microsoft.Identity.Client.ConfidentialClientApplicationBuilder");
                        var TokenCacheType = compilationStartContext?.Compilation?.GetTypeByMetadataName("Microsoft.Identity.Client.ITokenCache");

                        //var declaration = syntaxNodeContext.Node as VariableDeclaratorSyntax;

                        //var x = declaration.DescendantNodes().Where(childNode => (childNode as MemberAccessExpressionSyntax).Name?.Identifier.ValueText == CreateMethod);

                        var node = syntaxNodeContext.Node as InvocationExpressionSyntax;
                        if (node == null)
                        {
                            return;
                        }

                        var expression = node.Expression as MemberAccessExpressionSyntax;

                        if (expression == null)
                        {
                            return;
                        }

                        var nameSyntax = expression.Expression as IdentifierNameSyntax;

                        if (nameSyntax == null)
                        {
                            return;
                        }

                        var typeInfo = codeBlockContext?.SemanticModel?.GetTypeInfo(nameSyntax).Type as INamedTypeSymbol;

                        if (typeInfo?.ConstructedFrom == null)
                        {
                            return;
                        }

                        var methodName = expression.Name?.Identifier.ValueText;
                        if (typeInfo.ConstructedFrom.Equals(BuilderType) && CreateMethod.Equals(methodName))
                        {
                            CreateMethodInvocationFound = true;
                            targetInvocation = expression;
                        }

                        if (/*typeInfo.ConstructedFrom.Equals(TokenCacheType) &&*/ SetBeforeAccessMethod.Equals(methodName))
                        {
                            setBeforeOrAfterAccessInvocationFound = true;
                        }

                    }, SyntaxKind.InvocationExpression);

                    codeBlockContext.RegisterCodeBlockEndAction(ctx =>
                    {
                        if (CreateMethodInvocationFound && !setBeforeOrAfterAccessInvocationFound)
                        {
                            ctx.ReportDiagnostic(Diagnostic.Create(Rule, targetInvocation.GetLocation()));
                        }

                        CreateMethodInvocationFound = false;
                        setBeforeOrAfterAccessInvocationFound = false;
                    });
                });
            });
        }

        public /*override*/ void Initialize2(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction((compilationStartContext) =>
            {
                //Define the names of the classes we are analyzing
                var BuilderType = compilationStartContext.Compilation.GetTypeByMetadataName("Microsoft.Identity.Client.ConfidentialClientApplicationBuilder");
                var TokenCacheType = compilationStartContext.Compilation.GetTypeByMetadataName("Microsoft.Identity.Client.ITokenCache");

                var CreateMethod = "Create";
                var SetBeforeAccessMethod = "SetBeforeAccess";

                /*Issues
                 How do I get the name and location of the variable in the InvocationExpressionSyntax/MemberAccessExpressionSyntax?
                 Why does the analyzer not detect the usage of SetBeforeAccess?
                 */
                compilationStartContext.RegisterSyntaxNodeAction((analysisContext) =>
                {
                    //Get all expressions
                    var invocations =
                        analysisContext.Node.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    var hasValueCalled = new HashSet<string>();
                    string clientAppName = string.Empty;
                    string tokenCacheName = string.Empty;
                    MemberAccessExpressionSyntax targetInvocation = null;

                    foreach (var expression in invocations)
                    {
                        var invocation = expression.Expression as MemberAccessExpressionSyntax;
                        var e = invocation.Expression as IdentifierNameSyntax;

                        if (e == null)
                            continue;

                        //Get type info
                        var typeInfo = analysisContext.SemanticModel.GetTypeInfo(e).Type as INamedTypeSymbol;

                        if (typeInfo?.ConstructedFrom == null)
                            continue;

                        //NOTE: Inorder to run this with the unit test, the check for the BuilderType and TokenCacheType has to be disabled. for some reason, the test cannot construct these during unit testing. 
                        //The rest of the logic will work as expected however.
                        //Verify that we are looking at the right type BuilderType
                        if (typeInfo.ConstructedFrom.Equals(BuilderType) && !hasValueCalled.Contains(CreateMethod))
                        {
                            //clientAppName = e.Identifier.Text;


                            //if(invocation.Parent.GetText().ToString() == "Build")
                            //Checking for Build() does not work. There is probably an issue when searching through the
                            //builder pattern. Create seems to work though.

                            //Verify that the create method is called
                            if (invocation.Name.ToString().Contains(CreateMethod))
                            {
                                //analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, e.GetLocation()));

                                targetInvocation = invocation;
                                //We have now identified that the Create method was used. Storing in hashSet
                                hasValueCalled.Add(CreateMethod);
                                continue;
                            }
                        }

                        //checking for TokenCacheType
                        if (typeInfo.ConstructedFrom.Equals(TokenCacheType))
                        {
                            //Checking to see if SetBeforeAccess was called
                            if (invocation.Name.ToString() == SetBeforeAccessMethod)
                            {
                                hasValueCalled.Add(SetBeforeAccessMethod);
                                break;
                            }

                            continue;
                        }

                        continue;
                    }

                    //Check to see if app is created without token cache being set up.
                    if (hasValueCalled.Contains(CreateMethod) && !hasValueCalled.Contains(SetBeforeAccessMethod))
                    {
                        if (targetInvocation != null)
                        {
                            analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, targetInvocation.GetLocation()));
                        }
                    }

                }, SyntaxKind.InvocationExpression);
            });
        }


        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
