using System;
using System.Collections.Generic;
using System.Linq;

namespace Stationeers.Compiler
{
    public class Lexer
    {
        private static readonly char[] BinDigits = new char[] { '0', '1', '_' };
        private static readonly char[] HexDigits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f' };

        private string _code;
        private int _position;

        public Lexer(string code)
        {
            _code = code;
            _position = 0;
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            while (_position < _code.Length)
            {
                char current = _code[_position];

                if (char.IsWhiteSpace(current))
                {
                    _position++;
                }
                else if (current == '#')
                {
                    ReadComment(); // ignore comment
                }
                else if (current == '"')
                {
                    tokens.Add(ReadString());
                }
                else if (char.IsDigit(current) || (current == '.' && _position + 1 < _code.Length && char.IsDigit(_code[_position + 1])))
                {
                    tokens.Add(ReadNumber());
                }
                else if (current == '$')
                {
                    tokens.Add(ReadNumberAsHex());
                }
                else if (current == '%')
                {
                    tokens.Add(ReadNumberAsBin());
                }
                else if (char.IsLetter(current) || current == '_')
                {
                    tokens.Add(ReadIdentifier());
                }
                else
                {
                    switch (current)
                    {
                        case '=':
                            tokens.Add(new Token(TokenType.Symbol_Equal, "=", _position, _position));
                            break;
                        case '>':
                            tokens.Add(new Token(TokenType.Symbol_GreaterThen, ">", _position, _position));
                            break;
                        case '<':
                            tokens.Add(new Token(TokenType.Symbol_LessThen, ">", _position, _position));
                            break;
                        case '&':
                            tokens.Add(new Token(TokenType.Symbol_And, "&", _position, _position));
                            break;
                        case '|':
                            tokens.Add(new Token(TokenType.Symbol_Pipe, "|", _position, _position));
                            break;
                        case '!':
                            tokens.Add(new Token(TokenType.Symbol_ExclamationMark, "!", _position, _position));
                            break;
                        case '~':
                            tokens.Add(new Token(TokenType.Symbol_Tilde, "~", _position, _position));
                            break;
                        case '^':
                            tokens.Add(new Token(TokenType.Symbol_Hat, "^", _position, _position));
                            break;
                        case '+':
                            tokens.Add(new Token(TokenType.Symbol_Plus, "+", _position, _position));
                            break;
                        case '-':
                            tokens.Add(new Token(TokenType.Symbol_Minus, "-", _position, _position));
                            break;
                        case '*':
                            tokens.Add(new Token(TokenType.Symbol_Asterik, "*", _position, _position));
                            break;
                        case '/':
                            tokens.Add(new Token(TokenType.Symbol_Slash, "/", _position, _position));
                            break;
                        case '(':
                            tokens.Add(new Token(TokenType.Symbol_LeftParentheses, "(", _position, _position));
                            break;
                        case ')':
                            tokens.Add(new Token(TokenType.Symbol_RightParentheses, ")", _position, _position));
                            break;
                        case '.':
                            tokens.Add(new Token(TokenType.Symbol_Dot, ".", _position, _position));
                            break;
                        case ',':
                            tokens.Add(new Token(TokenType.Symbol_Comma, ",", _position, _position));
                            break;
                        case '{':
                            tokens.Add(new Token(TokenType.Symbol_LeftBrace, "{", _position, _position));
                            break;
                        case '}':
                            tokens.Add(new Token(TokenType.Symbol_RightBrace, "}", _position, _position));
                            break;
                        case ';':
                            tokens.Add(new Token(TokenType.Symbol_Semicolon, ";", _position, _position));
                            break;
                        case '[':
                            tokens.Add(new Token(TokenType.Symbol_LeftBracket, "[", _position, _position));
                            break;
                        case ']':
                            tokens.Add(new Token(TokenType.Symbol_RightBracket, "]", _position, _position));
                            break;
                        case '?':
                            tokens.Add(new Token(TokenType.Symbol_QuestionMark, "?", _position, _position));
                            break;
                        case ':':
                            tokens.Add(new Token(TokenType.Symbol_Colon, ":", _position, _position));
                            break;
                        default:
                            throw new Exception($"Unexpected character: {current}");
                    }

                    _position++;
                }
            }

            return tokens;
        }

        private Token ReadComment()
        {
            int start = _position++; // #

            while (_position < _code.Length && _code[_position] != '\n')
            {
                _position++;
            }

            return new Token(TokenType.Comment, _code.Substring(start, _position - start), start, _position - 1);
        }

        private Token ReadString()
        {
            int begin = _position++; // "
            int start = _position;

            while (_position < _code.Length && _code[_position] != '"')
            {
                if (_code[_position] == '\\' && _position + 1 < _code.Length && _code[_position + 1] == '\"')
                {
                    ++_position;
                }

                _position++;
            }

            if (_position < _code.Length && _code[_position] == '"')
            {
                return new Token(TokenType.String, _code.Substring(start, _position - start), begin, _position++);
            }
            else
            {
                throw new Exception("Unterminated string literal");
            }
        }

        private Token ReadIdentifier()
        {
            int start = _position;

            while (_position < _code.Length && (char.IsLetterOrDigit(_code[_position]) || _code[_position] == '_'))
            {
                _position++;
            }

            return new Token(TokenType.Identifier, _code.Substring(start, _position - start), start, _position - 1);
        }

        private Token ReadNumberAsHex()
        {
            int start = _position++;
            while (_position < _code.Length && HexDigits.Contains(_code[_position]))
            {
                _position++;
            }

            return new Token(TokenType.Number, _code.Substring(start, _position - start), start, _position - 1);
        }

        private Token ReadNumberAsBin()
        {
            int start = _position++;
            while (_position < _code.Length && BinDigits.Contains(_code[_position]))
            {
                _position++;
            }

            return new Token(TokenType.Number, _code.Substring(start, _position - start), start, _position - 1);
        }

        private Token ReadNumber()
        {
            int start = _position;
            bool hasDot = false;

            if (_code[_position] == '.')
            {
                hasDot = true;
                _position++;
            }

            while (_position < _code.Length && (char.IsDigit(_code[_position]) || (!hasDot && _code[_position] == '.')))
            {
                _position++;
            }

            return new Token(TokenType.Number, _code.Substring(start, _position - start), start, _position - 1);
        }
    }
}
