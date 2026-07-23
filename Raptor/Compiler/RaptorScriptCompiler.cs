using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Raptor.Compiler
{
    public static class RaptorScriptCompiler
    {
        public static string Compile(
            string sourceText,
            DiagnosticReporter reporter,
            Dictionary<string, int>? propertyMappings = null,
            bool printAst = false
        )
        {
            return Compile(sourceText, out _, reporter, propertyMappings, printAst);
        }

        public static string Compile(
            string sourceText,
            out IReadOnlyDictionary<string, int> variables,
            DiagnosticReporter reporter,
            Dictionary<string, int>? propertyMappings = null,
            bool printAst = false
        )
        {
            var lexer = new Lexer(sourceText, reporter);
            var tokens = lexer.ScanTokens();
            if (reporter.HasErrors)
            {
                throw new CompileException("Syntax errors detected.");
            }
            var parser = new Parser(tokens, reporter);
            var program = parser.Parse();
            if (reporter.HasErrors)
            {
                throw new CompileException("Syntax errors detected.");
            }
            program = (ProgramNode)ASTOptimizer.OptimizeNode(program);
            if (printAst)
            {
                Console.WriteLine("=== Abstract syntax tree ===");
                AstPrinter.Print(program);
                Console.WriteLine("============================");
            }
            var emitter = new Emitter(program, reporter, propertyMappings);
            try
            {
                string code = emitter.Emit();
                if (reporter.HasErrors)
                {
                    throw new CompileException("Compilation errors detected.");
                }
                variables = emitter.Globals;
                return code;
            }
            catch (Emitter.EmitException)
            {
                throw new CompileException("Compilation errors detected.");
            }
        }
    }

    [Serializable]
    public class CompileException : Exception
    {
        public CompileException() { }

        public CompileException(string? message)
            : base(message) { }

        public CompileException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }
}
