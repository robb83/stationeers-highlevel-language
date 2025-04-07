import 'string_reader.dart';

class Token {
  TokenTypes type;
  String value;

  Token(this.type, this.value);
}

enum TokenTypes {
  Comment,
  Identifier,
  Number,
  String,
  SymbolLeftParentheses,
  SymbolRightParentheses,
  SymbolLeftBrace,
  SymbolRightBrace,
  SymbolLeftBracket,
  SymbolRightBracket,
  SymbolComma,
  SymbolDot,
  SymbolSemicolon,
  SymbolColon,
  SymbolQuestionMark,
  SymbolExclamationMark,
  SymbolEqual,
  SymbolLessThen,
  SymbolGreaterThen,
  SymbolPlus,
  SymbolMinus,
  SymbolAsterik,
  SymbolSlash,
  SymbolAnd,
  SymbolPipe,
  SymbolTilde,
  SymbolHat,
}

class Lexer {
  final whitespaces = {' ', '\n', '\r', '\t'};
  final digits = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
  final hex = {
    '0',
    '1',
    '2',
    '3',
    '4',
    '5',
    '6',
    '7',
    '8',
    '9',
    'a',
    'A',
    'b',
    'B',
    'c',
    'C',
    'd',
    'D',
    'e',
    'E',
    'f',
    'F',
  };
  final bin = {'0', '1', '_'};
  final identifier = {
    '_',
    '0',
    '1',
    '2',
    '3',
    '4',
    '5',
    '6',
    '7',
    '8',
    '9',
    'A',
    'a',
    'B',
    'b',
    'C',
    'c',
    'D',
    'd',
    'E',
    'e',
    'F',
    'f',
    'G',
    'g',
    'H',
    'h',
    'I',
    'i',
    'J',
    'j',
    'K',
    'k',
    'L',
    'l',
    'M',
    'm',
    'N',
    'n',
    'O',
    'o',
    'P',
    'p',
    'Q',
    'q',
    'R',
    'r',
    'S',
    's',
    'T',
    't',
    'U',
    'u',
    'V',
    'v',
    'W',
    'w',
    'X',
    'x',
    'Y',
    'y',
    'Z',
    'z',
  };
  final symbols = {
    '(': TokenTypes.SymbolLeftParentheses,
    ')': TokenTypes.SymbolRightParentheses,
    '{': TokenTypes.SymbolLeftBrace,
    '}': TokenTypes.SymbolRightBrace,
    '[': TokenTypes.SymbolLeftBracket,
    ']': TokenTypes.SymbolRightBracket,
    ',': TokenTypes.SymbolComma,
    '.': TokenTypes.SymbolDot,
    '?': TokenTypes.SymbolQuestionMark,
    '!': TokenTypes.SymbolExclamationMark,
    ';': TokenTypes.SymbolSemicolon,
    ':': TokenTypes.SymbolColon,
    '=': TokenTypes.SymbolEqual,
    '<': TokenTypes.SymbolLessThen,
    '>': TokenTypes.SymbolGreaterThen,
    '+': TokenTypes.SymbolPlus,
    '-': TokenTypes.SymbolMinus,
    '*': TokenTypes.SymbolAsterik,
    '/': TokenTypes.SymbolSlash,
    '&': TokenTypes.SymbolAnd,
    '|': TokenTypes.SymbolPipe,
    '~': TokenTypes.SymbolTilde,
    '^': TokenTypes.SymbolHat,
  };
  StringReader iterator;

  Lexer(this.iterator);

  factory Lexer.fromString(String source) {
    return Lexer(StringReader(source));
  }

  List<Token> lexer() {
    List<Token> result = [];

    while (iterator.next()) {
      if (iterator.current_in(whitespaces)) {
        continue;
      } else if (iterator.current_is('#')) {
        iterator.mark();
        iterator.seek_until('\n');
        // result.add(Token(TokenTypes.Comment, iterator.get()));
      } else if (iterator.current_is('"')) {
        iterator.mark();
        iterator.next();

        while (!iterator.current_is('"')) {
          if (iterator.current_is('\\') && iterator.peek_is('"')) {
            iterator.next();
          }

          iterator.next();
        }

        if (!iterator.current_is('"')) {
          throw new Exception("Unterminated string literal");
        }

        result.add(Token(TokenTypes.String, iterator.getInnerContent().replaceAll("\\\"", "\"")));
      } else if (iterator.current_is('\$')) {
        iterator.mark();
        iterator.seek_until_in(hex);
        result.add(Token(TokenTypes.Number, iterator.get()));
      } else if (iterator.current_is('%')) {
        iterator.mark();
        iterator.seek_until_in(bin);
        result.add(Token(TokenTypes.Number, iterator.get()));
      } else if (iterator.current_in(digits) ||
          (iterator.current_is('.') && iterator.peek_in(digits))) {
        var dot = iterator.current_is('.');
        iterator.mark();

        while (iterator.peek_in(digits) || (!dot && iterator.peek_is('.'))) {
          dot = dot || iterator.peek_is('.');
          iterator.next();
        }

        result.add(Token(TokenTypes.Number, iterator.get()));
      } else if (iterator.current_in(identifier)) {
        iterator.mark();
        iterator.seek_until_in(identifier);
        result.add(Token(TokenTypes.Identifier, iterator.get()));
      } else if (iterator.current_in_map(symbols)) {
        iterator.mark();
        var symbol = iterator.get();
        result.add(Token(symbols[symbol]!, symbol));
      } else {
        throw new Exception("Incorrect syntax.");
      }
    }

    return result;
  }
}
