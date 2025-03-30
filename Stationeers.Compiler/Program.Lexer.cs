using System;
using System.Collections.Generic;
using System.Linq;

namespace Stationeers.Compiler
{
    public class Lexer
    {
        private static readonly Dictionary<string, TokenType> ReservedKeywords;
        private static readonly char[] BinDigits = new char[] { '0', '1', '_' };
        private static readonly char[] HexDigits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f' };

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
                }
                else if (current == '#')
                {
                    tokens.Add(ReadComment());
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
                    tokens.Add(ReadIdentifierOrKeyword());
                }
                else
                {
                    switch (current)
                    {
                        case '=':
                            if (_position + 1 < _code.Length && _code[_position + 1] == '=')
                            {
                                tokens.Add(new Token(TokenType.Symbol_EqualEqual, "==", _position, ++_position));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.Symbol_Equal, "=", _position, _position));
                            }
                            break;
                        case '>':
                            if (_position + 1 < _code.Length && _code[_position + 1] == '=')
                            {
                                tokens.Add(new Token(TokenType.Symbol_GreaterThenOrEqual, ">=", _position, ++_position));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.Symbol_GreaterThen, ">", _position, _position));
                            }
                            break;
                        case '<':
                            if (_position + 1 < _code.Length && _code[_position + 1] == '=')
                            {
                                tokens.Add(new Token(TokenType.Symbol_LessThenOrEqual, "<=", _position, ++_position));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.Symbol_LessThen, ">", _position, _position));
                            }
                            break;
                        case '&':
                            if (_position + 1 < _code.Length && _code[_position + 1] == '&')
                            {
                                tokens.Add(new Token(TokenType.Symbol_LogicalAnd, "&", _position, ++_position));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.Symbol_And, "&", _position, _position));
                            }
                            break;
                        case '|':
                            if (_position + 1 < _code.Length && _code[_position + 1] == '|')
                            {
                                tokens.Add(new Token(TokenType.Symbol_LogicalOr, "|", _position, ++_position));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.Symbol_Pipe, "|", _position, _position));
                            }
                            break;
                        case '!':
                            tokens.Add(new Token(TokenType.Symbol_LogicalNot, "!", _position, _position));
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
                return new Token(TokenType.String, _code.Substring(start, _position - start - 1), begin, _position++);
            }
            else
            {
                throw new Exception("Unterminated string literal");
            }
        }

        private Token ReadIdentifierOrKeyword()
        {
            int start = _position;

            while (_position < _code.Length && (char.IsLetterOrDigit(_code[_position]) || _code[_position] == '_'))
            {
                _position++;
            }

            String identifier = _code.Substring(start, _position - start);

            if (ReservedKeywords.TryGetValue(identifier, out TokenType ttype))
            {
                return new Token(ttype, identifier, start, _position - 1);
            }
            else
            {
                return new Token(TokenType.Identifier, identifier, start, _position - 1);
            }
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
