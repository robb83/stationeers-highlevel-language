import 'lexer.dart';
import 'parser.dart';
import 'generator.dart';
import 'dart:io';

void main() {

    var source = File('../Examples/test000.src').readAsStringSync();
    var tokens = Lexer.fromString(source).lexer();
    var program = Parser(tokens).parse();
    var output = Generator(program).generate();
    print(output);
}
