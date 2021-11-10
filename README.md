# MSAL.Analyzer
Analyzer for MSAL.NET

Analyzer logic here
https://github.com/trwalke/MSAL.Analyzer/blob/accefe4f67119fa8e84270e7617cf3b26cd1b08f/MSAL.Analyzer/MSAL.Analyzer/MSAL.Analyzer/MSALAnalyzerAnalyzer.cs#L29-L97

When running analyzer, MSAL.Analyzer.Vsix must be start up project.

Once new VS instance loads, open Msal.Analyzer.Test to see analyszer running live

.gitignore not configured yet



 This analyzer checks for the use of ConfidentialClientApplicationBuilder.Create() reports when it sees that SetBeforeAccess is not used.
 This is just a POC to see if this will work.
 
 Issues
 Why does the logic not find the Build() invocation but does find the Create()
 Why does the analyzer logic loop twice?
 How do I get the name and location of the variable in the InvocationExpressionSyntax/MemberAccessExpressionSyntax?
 Why does the analyzer not detect the usage of SetBeforeAccess?
 
