using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace LogTool
{
    class Program
    {
        static void Main(string[] args)
        {
            //Work().Wait();
            Try2();
        }

        private static async Task Work()
        {
            var msBuildWorkspace = MSBuildWorkspace.Create();
            var project = await msBuildWorkspace
                //.OpenProjectAsync("C:\\Users\\vad7ik\\Documents\\VisualStudio2017\\Projects\\CodeTool1\\CodeTool1\\CodeTool1.csproj");
                .OpenProjectAsync("D:\\CONGEN\\cnt\\agent-library2\\AgentSkills\\AgentSkills\\FetchnExtract\\FetchnExtract.csproj");

            foreach (var document in project.Documents)
            {
                var syntaxTree = document.GetSyntaxTreeAsync();

                var semanticModel = await document.GetSemanticModelAsync();
                var immutableArray = semanticModel.LookupStaticMembers(0);

                //CSharpCompilation.Create("").AddReferences()
                //semanticModel.LookupNamespacesAndTypes(0, )
                //semanticModel.AnalyzeDataFlow()
                ///semanticModel.GetSymbolInfo(variableDeclaration.Declaration.Type)
                new TraceVisitor().Visit((await syntaxTree).GetRoot());
            }

            //Console.WriteLine(syntaxNode.DescendantNodes(x=>x.Kind()==SyntaxKind.MethodDeclaration,true).FirstOrDefault());

            //Traverse(syntaxNode);
            Console.WriteLine("alldone");
            Console.ReadLine();
        }

        private static List<string> LogTypesList = new List<string> {"Connotate.Logging.ILogger"};
        private static List<string> BusinesTypesList = new List<string> {""};
        private static List<MethodCallInfo> methodCallInfos = new List<MethodCallInfo>();
        public static void Try2()
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace
                //.OpenSolutionAsync("C:\\Users\\vad7ik\\Documents\\VisualStudio2017\\Projects\\CodeTool1\\CodeTool1\\CodeTool1.csproj")
                .OpenProjectAsync("D:\\CONGEN\\cnt\\agent-library2\\AgentSkills\\AgentSkills\\FetchnExtract\\FetchnExtract.csproj")
                .Result;
            foreach (var document in solution.Documents)
            //foreach (var document in solution.Projects.SelectMany(project => project.Documents))
            {
                var rootNode = document.GetSyntaxRootAsync().Result;
                var semanticModel = document.GetSemanticModelAsync().Result;

                var methodDeclarationSyntaxs = rootNode
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>();

                foreach (var methodDeclarationSyntax in methodDeclarationSyntaxs)
                {
                    foreach (var inMethod in methodDeclarationSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        //System.Console.WriteLine(inMethod);

                        if (inMethod.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                        {
                            var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax.Expression);
                            IFieldSymbol fieldSymbol = null;
                            if(symbolInfo.Symbol is IFieldSymbol)
                            {
                                fieldSymbol = (IFieldSymbol) symbolInfo.Symbol;
                                Console.Write(fieldSymbol.Type);
                                Console.Write(" <-> ");
                            }
                            Console.Write(symbolInfo.Symbol);
                            Console.Write(" <-> ");
                            Console.WriteLine(memberAccessExpressionSyntax);

                            var parentClassSyntax = methodDeclarationSyntax.Parent.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().First();

                            if (fieldSymbol != null)
                            {
                                string message = "";
                                if ( LogTypesList.Contains(fieldSymbol.Type.ToString()))
                                {
                                    message = ((InvocationExpressionSyntax)memberAccessExpressionSyntax.Parent).ArgumentList.Arguments
                                        .OfType<ArgumentSyntax>().SelectMany(_=>_.DescendantNodes().OfType<LiteralExpressionSyntax>()).FirstOrDefault()?.ToString();
                                }
                                var methodCallInfo = new MethodCallInfo
                                {
                                    From = new MethodInfo
                                    {
                                        ClassName = $"{((NamespaceDeclarationSyntax)parentClassSyntax.Parent).Name}" +
                                                    $".{parentClassSyntax.Identifier.ToFullString()}",
                                        MethodName = methodDeclarationSyntax.Identifier.ToString(),
                                        SourcePlace = methodDeclarationSyntax.GetLocation()?.GetLineSpan().ToString(),
                                        Message = message
                                    },
                                    To = new MethodInfo
                                    {
                                        ClassName = fieldSymbol.ToString(),
                                        MethodName = memberAccessExpressionSyntax.Name.ToString(),
                                        Message = fieldSymbol.Type.ToString(),
                                        SourcePlace = memberAccessExpressionSyntax.GetLocation().GetLineSpan().ToString()
                                    }
                                };

                                methodCallInfos.Add(methodCallInfo);
                            }
                        }
                    }

                    //Console.WriteLine(variableDeclaration.ToString());


                    //var syntaxTokens = variableDeclaration.ChildTokens();
                    //foreach (var methodCallExpression in syntaxTokens())
                    //{
                    //    var symbolInfo = semanticModel.GetSymbolInfo(methodCallExpression);
                    //}
                    //var typeSymbol = symbolInfo.Symbol; // the type symbol for the variable..
                    //if (typeSymbol.Name.Contains("log"))
                    //{
                    //    Console.ForegroundColor = ConsoleColor.Red;
                    //}
                    //else
                    //{
                    //    Console.ForegroundColor = ConsoleColor.White;
                    //}
                    //Console.WriteLine(typeSymbol);
                }
            }

            string Filter(string s) {
                return s?.Replace($"{'"'}","\\\"");
            }

            //[name=val]
            var edges = methodCallInfos.Select(_=> $"{Filter(_.From.MethodName)} -> {Filter(_.To.MethodName)}[label=\"{Filter(_.From.Message)}\"];{Environment.NewLine}");
            var digraphG = $"digraph G {{ {string.Concat(edges)} }}";

            File.WriteAllText("gr.dot", digraphG);

            Console.ReadLine();

            var jsonSerializer = new DataContractJsonSerializer(typeof(List<MethodCallInfo>));
            try
            {
                Console.WriteLine("saving to file");
                jsonSerializer.WriteObject(File.Create("out.json"), methodCallInfos);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"failed saving {exception}");
            }
            Console.ReadLine();
        }

        private static void Traverse(SyntaxNode node, int i = 0)
        {
            foreach (var childNode in node.ChildNodes())
            {
                if (childNode.IsKind(SyntaxKind.MethodDeclaration))
                {
                    Console.WriteLine(string.Concat(Enumerable.Repeat("", i)) + childNode + "" + childNode.Kind() + childNode.GetType().FullName);

                    Console.WriteLine("------------------------------------");
                }

                Traverse(childNode, i + 1);
            }
        }
    }
}
