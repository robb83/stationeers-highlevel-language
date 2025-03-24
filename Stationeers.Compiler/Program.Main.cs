﻿using System;
using System.IO;
using Stationeers.Compiler.AST;

namespace Stationeers.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = File.ReadAllText(@"tests\test000.src");
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var program = parser.Parse();
            var generator = new OutputGenerator(program);
            generator.Print();

            Console.ReadKey();
        }
    }
}
