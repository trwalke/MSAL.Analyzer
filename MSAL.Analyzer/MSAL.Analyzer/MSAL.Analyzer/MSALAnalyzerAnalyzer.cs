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

                        var methodName = expression.Name?.Identifier.ValueText;

                        if (CreateMethod.Equals(methodName))
                        {
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

                            if (typeInfo.ConstructedFrom.Equals(BuilderType))
                            {
                                CreateMethodInvocationFound = true;
                                targetInvocation = expression;
                                return;
                            }
                        }

                        if (SetBeforeAccessMethod.Equals(methodName))
                        {
                            var tokenCacheName = expression.DescendantNodes().Where(dNode =>dNode is IdentifierNameSyntax)
                                                 .Where(dNode => (dNode as IdentifierNameSyntax).Identifier.Text.Equals("UserTokenCache")).FirstOrDefault();
                            
                            if (tokenCacheName != null)
                            {
                                //var type1 = codeBlockContext.SemanticModel.GetTypeInfo(tokenCacheName).Type as INamedTypeSymbol;
                                setBeforeOrAfterAccessInvocationFound = true;
                            }
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
                        return;
                    });
                });
            });
        }
    }
}
