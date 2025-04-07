using System;
using System.IO;
using Stationeers.Compiler.AST;

namespace Stationeers.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                PrintUsage();
            }
            else if (!File.Exists(args[0]))
            {
                Console.WriteLine("File not found: {0}.", args[0]);
            }
            else
            {
                var source = File.ReadAllText(args[0]);
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens);
                var program = parser.Parse();
                var generator = new OutputGenerator(program);
                Console.WriteLine(generator.Print());
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} filepath", System.AppDomain.CurrentDomain.FriendlyName);
        }
    }
}
