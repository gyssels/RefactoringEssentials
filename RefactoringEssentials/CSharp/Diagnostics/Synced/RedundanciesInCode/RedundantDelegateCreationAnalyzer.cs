using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RedundantDelegateCreationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            CSharpDiagnosticIDs.RedundantDelegateCreationAnalyzerID,
            GettextCatalog.GetString("Explicit delegate creation expression is redundant"),
            GettextCatalog.GetString("Redundant explicit delegate declaration"),
            DiagnosticAnalyzerCategories.RedundanciesInCode,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.CreateFor(CSharpDiagnosticIDs.RedundantDelegateCreationAnalyzerID),
            customTags: DiagnosticCustomTags.Unnecessary
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (nodeContext) =>
                {
                    Diagnostic diagnostic;
                    if (TryGetDiagnostic(nodeContext, out diagnostic))
                    {
                        nodeContext.ReportDiagnostic(diagnostic);
                    }
                },
                 SyntaxKind.ExpressionStatement
            );
        }
        public event EventHandler<EventArgs> Changed;

        void HandleChanged(object sender, EventArgs e)
        {
        }

        void someMethod()
        {
            Changed += new EventHandler<EventArgs>(HandleChanged);
            Changed += HandleChanged;
            Changed -= HandleChanged;


        }

        private static bool TryGetDiagnostic(SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
        {
            diagnostic = default(Diagnostic);
            if (nodeContext.IsFromGeneratedCode())
                return false;


            var semanticModel = nodeContext.SemanticModel;
            var expressionStatement = nodeContext.Node as ExpressionStatementSyntax;
            var addOrSubstractExpression = expressionStatement?.Expression as AssignmentExpressionSyntax;
            var rightMember = addOrSubstractExpression?.Right as ObjectCreationExpressionSyntax;

            if (rightMember == null || rightMember.ArgumentList.Arguments.Count != 1)
                return false; 

            var leftTypeInfo = ModelExtensions.GetTypeInfo(semanticModel, addOrSubstractExpression.Left).ConvertedType;
            if (leftTypeInfo == null || leftTypeInfo.Kind.Equals(SyntaxKind.EventDeclaration))
                return false;

            //Why do I need to make that check ? 
            //var rightTypeInfo = ModelExtensions.GetTypeInfo(semanticModel, addOrSubstractExpression.Right).ConvertedType;
            //if (rightTypeInfo == null || rightTypeInfo.IsErrorType() || leftTypeInfo.Equals(rightTypeInfo))
            //    return false;

            diagnostic = Diagnostic.Create(descriptor, addOrSubstractExpression.GetLocation());
            return true;
        }
    }
}