using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Text;
using Stationeers.Compiler.AST;

namespace Stationeers.Compiler
{
    public partial class Parser
    {
        private readonly List<Token> _tokens;
        private int _position = 0;
        private Dictionary<string, string> _variables;
        private List<TokenType> _comparsionOperators;
        private List<TokenType> _termOperators;
        private List<TokenType> _factorOperators;
        private List<TokenType> _logicOperators;
        private List<String> _builtInFunctions;

        public Parser(List<Token> tokens)
        {
            this._tokens = tokens;
            this._variables = new Dictionary<string, string>();

            _comparsionOperators = new List<TokenType>
            {
                TokenType.Symbol_EqualEqual,
                TokenType.Symbol_NotEqual,
                TokenType.Symbol_LessThen,
                TokenType.Symbol_LessThenOrEqual,
                TokenType.Symbol_GreaterThen,
                TokenType.Symbol_GreaterThenOrEqual
            };

            _termOperators = new List<TokenType>
            {
                TokenType.Symbol_Plus,
                TokenType.Symbol_Minus
            };

            _factorOperators = new List<TokenType>
            {
                TokenType.Symbol_Asterik,
                TokenType.Symbol_Slash
            };

            _logicOperators = new List<TokenType>
            {
                TokenType.Symbol_LogicalAnd,
                TokenType.Symbol_LogicalOr
            };

            _builtInFunctions = new List<string>
            {
                Keywords.RAND,
                Keywords.ABS,
                Keywords.ACOS,
                Keywords.ASIN,
                Keywords.ATAN,
                Keywords.ATAN2,
                Keywords.CEIL,
                Keywords.COS,
                Keywords.EXP,
                Keywords.FLOOR,
                Keywords.LOG,
                Keywords.ROUND,
                Keywords.SIN,
                Keywords.SQRT,
                Keywords.TAN,
                Keywords.TRUNC,
                Keywords.MAX,
                Keywords.MIN,
                Keywords.MOD,
                Keywords.XOR,
                Keywords.NOR,
                Keywords.NOT,
                Keywords.AND,
                Keywords.OR,
                Keywords.SELECT,
                Keywords.SLA,
                Keywords.SLL,
                Keywords.SRA,
                Keywords.SRL
            };
        }

        public ProgramNode Parse()
        {
            List<Node> statements = new List<Node>();

            for (_position = 0; _position < _tokens.Count;)
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }

            return new ProgramNode(statements);
        }

        private Node ParseStatement()
        {
            if (CheckCurrent(TokenType.Keyword, Keywords.VAR))
            {
                return ParseVariableDecleration();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.LOOP))
            {
                return ParseLoopStatement();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.WHILE))
            {
                return ParseConditionalLoopStatement();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.IF))
            {
                return ParseIfStatement();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.BREAK))
            {
                return ParseBreakNode();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.CONTINUE))
            {
                return ParseContinueNode();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.DEVICE))
            {
                return ParseDeviceConfig();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.YIELD))
            {
                return ParseFunctionCall(true);
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.SLEEP))
            {
                return ParseFunctionCall(true);
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.HCF))
            {
                return ParseFunctionCall(true);
            }
            else if (_tokens[_position].Type == TokenType.Identifier)
            {
                return ParseAssigment();
            }
            else if (_tokens[_position].Type == TokenType.Symbol_LeftBrace)
            {
                Consume(); // {

                List<Node> statements = new List<Node>();
                while (Until(TokenType.Symbol_RightBrace))
                {
                    var statement = ParseStatement();
                    statements.Add(statement);
                }

                ConsumeIf(TokenType.Symbol_RightBrace); // }
                return new BlockNode(statements);
            }
            else
            {
                throw new Exception($"Syntax error or not supported token: { _tokens[_position].Type }, {_tokens[_position].Value }.");
            }
        }

        private Node ParseDeviceConfig()
        {
            // Device(Type, Port)
            // Device(Type, BatchMode)
            // Device(Type, Name, BatchMode)

            // Type:
            // StructureAirConditioner, StructureFurnace, StructureAdvancedFurnace, StructureFiltration,
            // StructureGasSensor, StructureGrowLight, StructureSolidFuelGenerator, StructureBattery,
            // StructureBatteryLarge, StructureVolumePump, StructureLiquidVolumePump, StructureActiveVent, StructureLogicMemory, ...

            // Port: db, d0, d1, d2, ....

            // BatchMode: Average, Maximum, Minimum, Sum

            int p = 0;
            Token[] arguments = new Token[3];

            Consume(); // Device
            ConsumeIf(TokenType.Symbol_LeftParentheses); // (

            while (Until(TokenType.Symbol_RightParentheses))
            {
                if (p >= arguments.Length)
                {
                    throw new Exception("Too many arguments for DeviceConfiguration.");
                }

                arguments[p++] = ConsumeIfAny(TokenType.Identifier, TokenType.String);

                if (!CheckCurrent(TokenType.Symbol_RightParentheses))
                {
                    ConsumeIf(TokenType.Symbol_Comma); // ,
                }
            }

            ConsumeIf(TokenType.Symbol_RightParentheses); // )

            var type = arguments[0]; // Identifier
            if (type == null)
            {
                throw new Exception("");
            }

            // Device(Type, Port)
            if (arguments[1] != null && IsDevicePort(arguments[1].Value))
            {
                var port = arguments[1];
                return new DeviceConfigNode(type.Value, port.Value, null, null);
            }

            // Device(Type, "Name", BatchMode)
            if (arguments[1] != null && arguments[1].Type == TokenType.String && arguments[2] != null && IsBatchMode(arguments[2].Value))
            {
                var name = arguments[1];
                return new DeviceConfigNode(type.Value, null, name.Value, arguments[2].Value);
            }

            // Device(Type, BatchMode)
            if (arguments[1] != null && IsBatchMode(arguments[1].Value))
            {
                var name = arguments[1];
                return new DeviceConfigNode(type.Value, null, null, arguments[1].Value);
            }

            throw new Exception("Invalid Device Configuration.");
        }

        private bool IsBatchMode(String value)
        {
            return String.Compare("Average", value, StringComparison.Ordinal) == 0
                || String.Compare("Minimum", value, StringComparison.Ordinal) == 0
                || String.Compare("Maximum", value, StringComparison.Ordinal) == 0
                || String.Compare("Sum", value, StringComparison.Ordinal) == 0;
        }

        private bool IsDevicePort(String value)
        {
            return !(String.IsNullOrEmpty(value) || value.Length != 2 || value[0] != 'd' || !(value[1] == 'b' || Char.IsDigit(value[1])));
        }

        private Node ParseLoopStatement()
        {
            Consume(); // loop
            var statement = ParseStatement();
            return new LoopNode(statement);
        }

        private Node ParseConditionalLoopStatement()
        {
            Consume(); // while
            ConsumeIf(TokenType.Symbol_LeftParentheses); // (

            var condition = ParseExpression();

            ConsumeIf(TokenType.Symbol_RightParentheses); // )

            var statement = ParseStatement();
            return new ConditionalLoopNode(condition, statement);
        }

        private Node ParseFunctionCall(bool statement = false)
        {
            var arguments = new List<Node>();
            var identifier = Consume(); // keyword or identifier
            ConsumeIf(TokenType.Symbol_LeftParentheses);

            if (!CheckCurrent(TokenType.Symbol_RightParentheses))
            {
                arguments.Add(ParseExpression());
            }

            while (Until(TokenType.Symbol_RightParentheses))
            {
                ConsumeIf(TokenType.Symbol_Comma);
                arguments.Add(ParseExpression());
            }

            ConsumeIf(TokenType.Symbol_RightParentheses);

            if (statement)
            {
                ConsumeIf(TokenType.Symbol_Semicolon);
            }

            return new CallNode(identifier.Value, arguments);
        }

        private Node ParseBreakNode()
        {
            Consume(); // continue
            ConsumeIf(TokenType.Symbol_Semicolon);

            return new BreakNode();
        }

        private Node ParseContinueNode()
        {
            Consume(); // continue
            ConsumeIf(TokenType.Symbol_Semicolon);

            return new ContinueNode();
        }

        private Node ParseIfStatement()
        {
            Node condition = null;
            Node statement = null;
            Node alternate = null;

            Consume(); // if or elif
            ConsumeIf(TokenType.Symbol_LeftParentheses); // (

            condition = ParseExpression();

            ConsumeIf(TokenType.Symbol_RightParentheses); // )

            statement = ParseStatement();

            if (CheckCurrent(TokenType.Keyword, Keywords.ELIF))
            {
                alternate = ParseIfStatement();
            }
            else if (CheckCurrent(TokenType.Keyword, Keywords.ELSE))
            {
                Consume();
                alternate = ParseStatement();
            }

            return new ConditionalStatementNode(condition, statement, alternate);
        }

        private Node ParseAssigment()
        {
            Token identifier = _tokens[_position++];
            Token property = null;
            Node index = null;

            if (!_variables.ContainsKey(identifier.Value))
            {
                throw new Exception($"Variable not declared: {identifier.Value}.");
            }

            if (CheckCurrent(TokenType.Symbol_LeftBracket))
            {
                Consume(); // [

                if (CheckCurrent(TokenType.Number))
                {
                    var number = Consume();
                    index = new NumericNode(number.Value);
                } 
                else if (CheckCurrent(TokenType.Identifier))
                {
                    var indeIdentifier = Consume();
                    index = new IdentifierNode(indeIdentifier.Value);
                }
                else
                {
                    throw new Exception("Index missing or invalid.");
                }

                ConsumeIf(TokenType.Symbol_RightBracket); // ]
            }

            if (CheckCurrent(TokenType.Symbol_Dot))
            {
                Consume(); // .
                property = ConsumeIf(TokenType.Identifier);
            }

            if (index != null && property == null)
            {
                throw new Exception("Not supported expression: index without slotLogicType.");
            }

            ConsumeIf(TokenType.Symbol_Equal); // =

            var expr = ParseExpression();

            ConsumeIf(TokenType.Symbol_Semicolon);

            return new AssigmentNode(new IdentifierNode(identifier.Value, index, property?.Value), expr);
        }

        private Node ParseVariableDecleration()
        {
            Consume(); // var
            
            var identifier = ConsumeIf(TokenType.Identifier);

            if (_variables.ContainsKey(identifier.Value))
            {
                throw new Exception($"Variable already declared: {identifier.Value}.");
            }

            _variables.Add(identifier.Value, "");

            ConsumeIf(TokenType.Symbol_Equal); // =

            var expr = ParseExpression();

            ConsumeIf(TokenType.Symbol_Semicolon);

            return new VariableDeclerationNode(identifier.Value, expr);
        }

        private Node ParseExpression()
        {
            return ParseTernary();
        }

        private Node ParseTernary()
        {
            var node = ParseLogical();

            if (CheckCurrent(TokenType.Symbol_QuestionMark))
            {
                Consume();
                var left = ParseLogical();
                ConsumeIf(TokenType.Symbol_Colon);
                var right = ParseLogical();
                return new TernaryOpNode(node, left, right);
            }

            return node;
        }

        private Node ParseLogical()
        {
            var node = ParseComparsion();

            while (_position < _tokens.Count && _logicOperators.Contains(_tokens[_position].Type))
            {
                var op = ConvertToLogical(_tokens[_position++].Type);
                node = new LogicalNode(node, op, ParseComparsion());
            }

            return node;
        }

        private Node ParseComparsion()
        {
            var node = ParseUnary();

            while (_position < _tokens.Count && _comparsionOperators.Contains(_tokens[_position].Type))
            {
                var op = ConvertToComparsion(_tokens[_position++].Type);
                node = new ComparisonNode(node, op, ParseUnary());
            }

            return node;
        }

        private Node ParseUnary()
        {
            if (CheckCurrent(TokenType.Symbol_LogicalNot))
            {
                Consume();
                var op = UnaryOperationType.OpNot;
                return new UnaryOpNode(ParseUnary(), op);
            }

            return ParseTerm();
        }

        private Node ParseTerm()
        {
            var node = ParseFactor();

            while (_position < _tokens.Count && _termOperators.Contains(_tokens[_position].Type))
            {
                var op = ConvertToOperator(_tokens[_position++].Type);
                node = new BinaryOpNode(node, op, ParseFactor());
            }

            return node;
        }

        private Node ParseFactor()
        {
            var node = ParsePrimary();

            while (_position < _tokens.Count && _factorOperators.Contains(_tokens[_position].Type))
            {
                var op = ConvertToOperator(_tokens[_position++].Type);
                node = new BinaryOpNode(node, op, ParsePrimary());
            }

            return node;
        }

        private Node ParsePrimary()
        {
            if (CheckCurrent(TokenType.String))
            {
                return new StringNode(_tokens[_position++].Value);
            }

            if (CheckCurrent(TokenType.Number))
            {
                return new NumericNode(_tokens[_position++].Value);
            }

            if (CheckCurrent(TokenType.Constant))
            {
                if (Utils.IsConstantExpression(_tokens[_position].Value))
                {
                    return new ConstantNode(_tokens[_position++].Value);
                } 
                else
                {
                    throw new Exception($"Not supported constant: {_tokens[_position].Value}.");
                }
            }

            if (CheckCurrent(TokenType.Identifier))
            {
                Token identifier = _tokens[_position++];
                Token property = null;
                Node index = null;

                if (!_variables.ContainsKey(identifier.Value))
                {
                    throw new Exception($"Variable not declared: {identifier.Value}.");
                }

                if (CheckCurrent(TokenType.Symbol_LeftBracket))
                {
                    Consume(); // [

                    if (CheckCurrent(TokenType.Number))
                    {
                        var number = Consume();
                        index = new NumericNode(number.Value);
                    }
                    else if (CheckCurrent(TokenType.Identifier))
                    {
                        var indeIdentifier = Consume();
                        index = new IdentifierNode(indeIdentifier.Value);
                    }
                    else
                    {
                        throw new Exception($"Index missing or invalid ({identifier.Value}).");
                    }

                    ConsumeIf(TokenType.Symbol_RightBracket); // ]
                }

                if (CheckCurrent(TokenType.Symbol_Dot))
                {
                    Consume(); // .

                    if (CheckCurrent(TokenType.Identifier))
                    {
                        property = _tokens[_position++];
                    }
                    else
                    {
                        throw new Exception($"LogicType or Property missing ({identifier.Value}).");
                    }
                }

                if (index != null && property == null)
                {
                    throw new Exception("Not supported expression: index without slotLogicType.");
                }

                return new IdentifierNode(identifier.Value, index, property?.Value);
            }

            if (CheckCurrent(TokenType.Keyword, Keywords.HASH))
            {
                Consume();
                ConsumeIf(TokenType.Symbol_LeftParentheses);
                var token = ConsumeIf(TokenType.String);
                ConsumeIf(TokenType.Symbol_RightParentheses);

                return new HashNode(token.Value);
            }

            if (CheckCurrent(TokenType.Keyword, Keywords.DEVICE))
            {
                return ParseDeviceConfig();
            }

            if (CheckCurrent(TokenType.Keyword, _builtInFunctions))
            {
                return ParseFunctionCall();
            }

            if (CheckCurrent(TokenType.Symbol_LeftParentheses))
            {
                Consume();
                var expr = ParseExpression();
                ConsumeIf(TokenType.Symbol_RightParentheses);
                return expr;
            }

            throw new Exception($"Not supported expression token.");
        }

        private Token ConsumeIfAny(TokenType type1, TokenType type2)
        {
            if (CheckCurrent(type1) || CheckCurrent(type2))
            {
                return _tokens[_position++];
            }
            else
            {
                Token current = (_position < _tokens.Count ? _tokens[_position] : null);

                if (current == null)
                {
                    throw new Exception("Unexpected end of file.");
                }
                else
                {
                    throw new Exception($"Unexpected token: {current.Type}, {current.Value} (Expect: {type1} or {type2}).");
                }
            }
        }

        private Token ConsumeIf(TokenType type)
        {
            if (CheckCurrent(type))
            {
                return _tokens[_position++];
            }
            else
            {
                Token current = (_position < _tokens.Count ? _tokens[_position] : null);

                if (current == null)
                {
                    throw new Exception("Unexpected end of file.");
                } 
                else
                {
                    throw new Exception($"Unexpected token: {current.Type}, {current.Value} (Expect: {type}).");
                }
            }
        }

        private Token Consume()
        {
            return _tokens[_position++];
        }

        private bool Until(TokenType type)
        {
            return _position < _tokens.Count && _tokens[_position].Type != type;
        }

        private bool CheckCurrent(TokenType type)
        {
            return _position < _tokens.Count && _tokens[_position].Type == type;
        }

        private bool CheckCurrent(TokenType type, String value)
        {
            return _position < _tokens.Count && _tokens[_position].Type == type && String.Compare(value, _tokens[_position].Value, StringComparison.Ordinal) == 0;
        }

        private bool CheckCurrent(TokenType type, List<String> keywords)
        {
            return _position < _tokens.Count && _tokens[_position].Type == type && keywords.Contains(_tokens[_position].Value);
        }

        private LogicalOperatorType ConvertToLogical(TokenType tt)
        {
            switch (tt)
            {
                case TokenType.Symbol_LogicalAnd:
                    return LogicalOperatorType.OpAnd;
                case TokenType.Symbol_LogicalOr:
                    return LogicalOperatorType.OpOr;
                default:
                    throw new Exception($"Not supported logical operation type: {tt}.");
            }
        }

        private ComparsionOperatorType ConvertToComparsion(TokenType tt)
        {
            switch (tt)
            {
                case TokenType.Symbol_EqualEqual:
                    return ComparsionOperatorType.OpEqual;
                case TokenType.Symbol_NotEqual:
                    return ComparsionOperatorType.OpNotEqual;
                case TokenType.Symbol_LessThen:
                    return ComparsionOperatorType.OpLess;
                case TokenType.Symbol_LessThenOrEqual:
                    return ComparsionOperatorType.OpLessOrEqual;
                case TokenType.Symbol_GreaterThen:
                    return ComparsionOperatorType.OpGreater;
                case TokenType.Symbol_GreaterThenOrEqual:
                    return ComparsionOperatorType.OpGreaterOrEqual;
                default:
                    throw new Exception($"Not supported comparsion operation type: {tt}.");
            }
        }

        private OperatorType ConvertToOperator(TokenType tt)
        {
            switch (tt)
            {
                case TokenType.Symbol_Plus:
                    return OperatorType.OpAdd;
                case TokenType.Symbol_Minus:
                    return OperatorType.OpSub;
                case TokenType.Symbol_Asterik:
                    return OperatorType.OpMul;
                case TokenType.Symbol_Slash:
                    return OperatorType.OpDiv;
                default:
                    throw new Exception($"Not supported Operation Type: {tt}.");
            }
        }
    }
}
