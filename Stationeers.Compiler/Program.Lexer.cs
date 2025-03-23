using System;
using System.Collections.Generic;

namespace Stationeers.Compiler
{
    public class Lexer
    {
        private static readonly Dictionary<string, TokenType> Keywords;

        private string _code;
        private int _position;

        static Lexer()
        {
            Keywords = new Dictionary<string, TokenType>();
            Keywords.Add("var", TokenType.Keyword);
            Keywords.Add("while", TokenType.Keyword);
            Keywords.Add("if", TokenType.Keyword);
            Keywords.Add("elif", TokenType.Keyword);
            Keywords.Add("else", TokenType.Keyword);
            Keywords.Add("return", TokenType.Keyword);
            Keywords.Add("break", TokenType.Keyword);
            Keywords.Add("continue", TokenType.Keyword);
            Keywords.Add("Device", TokenType.Keyword);
        }

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
                    continue;
                }

                if (current == '#')
                {
                    string value = ReadComment();
                    // tokens.Add(new Token(TokenType.Comment, value));
                    continue;
                }

                if (current == '"')
                {
                    string value = ReadString();
                    tokens.Add(new Token(TokenType.String, value));
                    continue;
                }

                if (char.IsDigit(current) || (current == '-' && _position + 1 < _code.Length && (char.IsDigit(_code[_position + 1]) || _code[_position + 1] == '.')) || (current == '.' && _position + 1 < _code.Length && char.IsDigit(_code[_position + 1])))
                {
                    string number = ReadNumber();
                    tokens.Add(new Token(TokenType.Number, number));
                    continue;
                }

                if (char.IsLetter(current) || current == '_')
                {
                    string identifier = ReadIdentifier();
                    if (Keywords.ContainsKey(identifier))
                    {
                        tokens.Add(new Token(TokenType.Keyword, identifier));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Identifier, identifier));
                    }

                    continue;
                }

                switch (current)
                {
                    case '!':
                        tokens.Add(new Token(TokenType.Symbol_Not, "!"));
                        break;
                    case '=':
                        if (_position + 1 < _code.Length && _code[_position + 1] == '=')
                        {
                            _position++;
                            tokens.Add(new Token(TokenType.Symbol_EqualEqual, "=="));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Symbol_Equal, "="));
                        }
                        break;
                    case '>':
                        if (_position + 1 < _code.Length && _code[_position + 1] == '=')
                        {
                            _position++;
                            tokens.Add(new Token(TokenType.Symbol_GreaterThenOrEqual, ">="));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Symbol_GreaterThen, ">"));
                        }
                        break;
                    case '<':
                        if (_position + 1 < _code.Length && _code[_position + 1] == '=')
                        {
                            _position++;
                            tokens.Add(new Token(TokenType.Symbol_LessThenOrEqual, "<="));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Symbol_LessThen, ">"));
                        }
                        break;
                    case '+':
                        tokens.Add(new Token(TokenType.Symbol_Plus, "+"));
                        break;
                    case '-':
                        tokens.Add(new Token(TokenType.Symbol_Minus, "-"));
                        break;
                    case '*':
                        tokens.Add(new Token(TokenType.Symbol_Asterik, "*"));
                        break;
                    case '/':
                        tokens.Add(new Token(TokenType.Symbol_Slash, "/"));
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.Symbol_LeftParentheses, "("));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.Symbol_RightParentheses, ")"));
                        break;
                    case '.':
                        tokens.Add(new Token(TokenType.Symbol_Dot, "."));
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.Symbol_Comma, ","));
                        break;
                    case '{':
                        tokens.Add(new Token(TokenType.Symbol_LeftBrace, "{"));
                        break;
                    case '}':
                        tokens.Add(new Token(TokenType.Symbol_RightBrace, "}"));
                        break;
                    case ';':
                        tokens.Add(new Token(TokenType.Symbol_Semicolon, current.ToString()));
                        break;
                    default:
                        throw new Exception($"Unexpected character: {current}");
                }

                _position++;
            }

            return tokens;
        }

        private string ReadComment()
        {
            int start = _position + 1;
            while (_position < _code.Length && _code[_position] != '\n')
            {
                _position++;
            }

            return _code.Substring(start, _position - start);
        }

        private string ReadString()
        {
            _position++; // "

            int start = _position;
            while (_position < _code.Length && _code[_position] != '"')
            {
                _position++;
            }

            if (_position < _code.Length && _code[_position] == '"')
            {
                _position++; // "
                return _code.Substring(start, _position - start - 1);
            }
            else
            {
                throw new Exception("Unterminated string literal");
            }
        }

        private string ReadIdentifier()
        {
            int start = _position;

            while (_position < _code.Length && (char.IsLetterOrDigit(_code[_position]) || _code[_position] == '_'))
                _position++;
            return _code.Substring(start, _position - start);
        }

        private string ReadNumber()
        {
            int start = _position;
            bool hasDot = false;
            bool hasMinus = false;

            if (_code[_position] == '-')
            {
                hasMinus = true;
                _position++;
            }

            while (_position < _code.Length && (char.IsDigit(_code[_position]) || (!hasDot && _code[_position] == '.') ))
                _position++;
            return _code.Substring(start, _position - start);
        }
    }
}
