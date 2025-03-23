using System;
using System.Collections.Generic;

namespace Stationeers.Compiler
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _position = 0;
        private Dictionary<string, string> _variables;
        private List<TokenType> _comparsionOperators;
        private List<TokenType> _termOperators;
        private List<TokenType> _factorOperators;

        public static class Keywords
        {
            public const String VAR = "var";
            public const String WHILE= "while";
            public const String BREAK = "break";
            public const String CONTINUE = "continue";
            public const String IF = "if";
            public const String ELIF = "elif";
            public const String ELSE = "else";
            public const String DEVICE = "Device";
        }

        public Parser(List<Token> tokens)
        {
            this._tokens = tokens;
            this._variables = new Dictionary<string, string>();

            _comparsionOperators = new List<TokenType>();
            _comparsionOperators.Add(TokenType.Symbol_EqualEqual);
            _comparsionOperators.Add(TokenType.Symbol_NotEqual);
            _comparsionOperators.Add(TokenType.Symbol_LessThen);
            _comparsionOperators.Add(TokenType.Symbol_LessThenOrEqual);
            _comparsionOperators.Add(TokenType.Symbol_GreaterThen);
            _comparsionOperators.Add(TokenType.Symbol_GreaterThenOrEqual);

            _termOperators = new List<TokenType>
            {
                TokenType.Symbol_Plus,
                TokenType.Symbol_Minus
            };

            _factorOperators = new List<TokenType>();
            _factorOperators.Add(TokenType.Symbol_Asterik);
            _factorOperators.Add(TokenType.Symbol_Slash);
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
            else if (CheckCurrent(TokenType.Keyword, Keywords.WHILE))
            {
                return ParseWhileStatement();
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
                throw new Exception($"Syntax error or not supported token.");
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

            throw new Exception("");
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

        private Node ParseWhileStatement()
        {
            Consume(); // while
            ConsumeIf(TokenType.Symbol_LeftParentheses); // (

            var condition = ParseExpression();

            ConsumeIf(TokenType.Symbol_RightParentheses); // )

            var statement = ParseStatement();
            return new WhileStatementNode(condition, statement);
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

            if (!_variables.ContainsKey(identifier.Value))
            {
                throw new Exception($"Variable not declared: {identifier.Value}.");
            }

            if (CheckCurrent(TokenType.Symbol_Dot))
            {
                Consume();
                property = ConsumeIf(TokenType.Identifier);
            }

            ConsumeIf(TokenType.Symbol_Equal); // =

            var expr = ParseExpression();

            ConsumeIf(TokenType.Symbol_Semicolon);

            return new AssigmentNode(identifier.Value, property?.Value, expr);
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
            return ParseComparsion();
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
            if (CheckCurrent(TokenType.Symbol_Not))
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

            if (CheckCurrent(TokenType.Identifier))
            {
                Token identifier = _tokens[_position++];
                Token property = null;

                if (!_variables.ContainsKey(identifier.Value))
                {
                    throw new Exception($"Variable not declared: {identifier.Value}.");
                }

                if (CheckCurrent(TokenType.Symbol_Dot))
                {
                    Consume();

                    if (CheckCurrent(TokenType.Identifier))
                    {
                        property = _tokens[_position++];
                    }
                    else
                    {
                        throw new Exception($"LogicType or Property missing ({identifier.Value}).");
                    }
                }

                return new IdentifierNode(identifier.Value, property?.Value);
            }

            if (CheckCurrent(TokenType.Keyword, Keywords.DEVICE))
            {
                return ParseDeviceConfig();
            }

            if (CheckCurrent(TokenType.Symbol_LeftParentheses))
            {
                Consume();

                Node expr = ParseExpression();

                if (!CheckCurrent(TokenType.Symbol_LeftParentheses))
                {
                    throw new Exception();
                }

                Consume();
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
                    throw new Exception("Not supported comparsion operation type: {tt}.");
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
