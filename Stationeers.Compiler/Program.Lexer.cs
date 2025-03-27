using System;
using System.Collections.Generic;
using System.Linq;

namespace Stationeers.Compiler
{
    public class Lexer
    {
        private static readonly Dictionary<string, TokenType> ReservedKeywords;

        private string _code;
        private int _position;

        static Lexer()
        {
            ReservedKeywords = new Dictionary<string, TokenType>();
            ReservedKeywords.Add(Keywords.VAR, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.WHILE, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.IF, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ELIF, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ELSE, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.RETURN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.BREAK, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.CONTINUE, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.DEVICE, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.LOOP, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.DEF, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.FN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.HASH, TokenType.Keyword);

            ReservedKeywords.Add(Keywords.NAN, TokenType.Constant);
            ReservedKeywords.Add(Keywords.PINF, TokenType.Constant);
            ReservedKeywords.Add(Keywords.NINF, TokenType.Constant);
            ReservedKeywords.Add(Keywords.EPSILON, TokenType.Constant);
            ReservedKeywords.Add(Keywords.PI, TokenType.Constant);
            ReservedKeywords.Add(Keywords.DEG2RAD, TokenType.Constant);
            ReservedKeywords.Add(Keywords.RAD2DEG, TokenType.Constant);

            ReservedKeywords.Add(Keywords.ABS, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ACOS, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ASIN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ATAN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ATAN2, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.CEIL, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.COS, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.EXP, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.FLOOR, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.LOG, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.MAX, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.MIN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.MOD, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.RAND, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.ROUND, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.SIN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.SQRT, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.TAN, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.TRUNC, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.SELECT, TokenType.Keyword);

            ReservedKeywords.Add(Keywords.SLA, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.SLL, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.SRA, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.SRL, TokenType.Keyword);

            ReservedKeywords.Add(Keywords.SLEEP, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.YIELD, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.HCF, TokenType.Keyword);

            ReservedKeywords.Add(Keywords.AND, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.NOR, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.NOT, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.OR, TokenType.Keyword);
            ReservedKeywords.Add(Keywords.XOR, TokenType.Keyword);
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

                if (current == '$')
                {
                    string number = ReadNumberAsHex();
                    tokens.Add(new Token(TokenType.Number, number));
                    continue;
                }

                if (current == '%')
                {
                    string number = ReadNumberAsBin();
                    tokens.Add(new Token(TokenType.Number, number));
                    continue;
                }

                if (char.IsLetter(current) || current == '_')
                {
                    string identifier = ReadIdentifier();
                    if (ReservedKeywords.TryGetValue(identifier, out TokenType ttype))
                    {
                        tokens.Add(new Token(ttype, identifier));
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
                        tokens.Add(new Token(TokenType.Symbol_LogicalNot, "!"));
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
                    case '&':
                        if (_position + 1 < _code.Length && _code[_position + 1] == '&')
                        {
                            _position++;
                            tokens.Add(new Token(TokenType.Symbol_LogicalAnd, "&"));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Symbol_And, "&"));
                        }
                        break;
                    case '|':
                        if (_position + 1 < _code.Length && _code[_position + 1] == '|')
                        {
                            _position++;
                            tokens.Add(new Token(TokenType.Symbol_LogicalOr, "|"));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Symbol_Pipe, "|"));
                        }
                        break;
                    case '~':
                        tokens.Add(new Token(TokenType.Symbol_Tilde, "~"));
                        break;
                    case '^':
                        tokens.Add(new Token(TokenType.Symbol_Hat, "^"));
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
                    case '[':
                        tokens.Add(new Token(TokenType.Symbol_LeftBracket, current.ToString()));
                        break;
                    case ']':
                        tokens.Add(new Token(TokenType.Symbol_RightBracket, current.ToString()));
                        break;
                    case '?':
                        tokens.Add(new Token(TokenType.Symbol_QuestionMark, current.ToString()));
                        break;
                    case ':':
                        tokens.Add(new Token(TokenType.Symbol_Colon, current.ToString()));
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
            {
                _position++;
            }
            return _code.Substring(start, _position - start);
        }

        private string ReadNumberAsHex()
        {
            char[] hexdigits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f' };

            int start = _position++;
            while (_position < _code.Length && hexdigits.Contains(_code[_position]))
            {
                _position++;
            }

            return _code.Substring(start, _position - start);
        }

        private string ReadNumberAsBin()
        {
            char[] bindigits = new char[] { '0', '1', '_' };

            int start = _position++;
            while (_position < _code.Length && bindigits.Contains(_code[_position]))
            {
                _position++;
            }

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
