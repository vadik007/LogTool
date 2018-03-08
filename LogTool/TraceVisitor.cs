using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LogTool
{
    class TraceVisitor:CSharpSyntaxWalker
    {
        /// <inheritdoc />
        public override void Visit(SyntaxNode node)
        {
            if (node is InvocationExpressionSyntax invocationNode )
            {
                var identifierSubNode = ((IdentifierNameSyntax) invocationNode.Expression.DescendantNodes()
                    .FirstOrDefault(_ => _.Kind() == SyntaxKind.IdentifierName));
                //identifierSubNode.
                //identifierSubNode.Identifier.
                WriteLog($"{node} | {identifierSubNode}");
            }
            base.Visit(node);
        }

        private void WriteLog(object e) => Console.WriteLine(e);
        private void WriteLog(SyntaxNode e) => Console.WriteLine(e);
    }


    
}
