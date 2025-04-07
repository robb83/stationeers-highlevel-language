import 'ast.dart';
import 'keywords.dart';
import 'lexer.dart';
import 'utils.dart';

class Parser {
  List<Token> _tokens;
  int _position = 0;
  Set<String> _variables = {};
  Set<String> _builtInMethods = {Keywords.HCF, Keywords.SLEEP, Keywords.YIELD};

  Set<String> _builtInFunctions = {
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
    Keywords.SRL,
  };

  Set<String> _constants = {
    Keywords.NAN,
    Keywords.PINF,
    Keywords.NINF,
    Keywords.EPSILON,
    Keywords.PI,
    Keywords.DEG2RAD,
    Keywords.RAD2DEG,
  };

  Set<String> _keywords = {
    Keywords.VAR,
    Keywords.WHILE,
    Keywords.IF,
    Keywords.ELIF,
    Keywords.ELSE,
    Keywords.RETURN,
    Keywords.BREAK,
    Keywords.CONTINUE,
    Keywords.DEVICE,
    Keywords.LOOP,
    Keywords.DEF,
    Keywords.FN,
  };

  Set<String> _batchModes = {
    Keywords.AVERAGE,
    Keywords.MAXIMUM,
    Keywords.MINIMUM,
    Keywords.SUM,
  };

  Parser(this._tokens);

  ProgramNode parse() {
    List<Node> statements = [];

    while (_position < _tokens.length) {
      var statement = parseStatement();
      statements.add(statement);
    }

    return ProgramNode(statements);
  }

  Node parseStatement() {
    if (checkCurrentValue(TokenTypes.Identifier, Keywords.VAR)) {
      return parseVariableDecleration();
    } else if (checkCurrentValue(TokenTypes.Identifier, Keywords.LOOP)) {
      return parseLoopStatement();
    } else if (checkCurrentValue(TokenTypes.Identifier, Keywords.WHILE)) {
      return parseConditionalLoopStatement();
    } else if (checkCurrentValue(TokenTypes.Identifier, Keywords.IF)) {
      return parseIfStatement();
    } else if (checkCurrentValue(TokenTypes.Identifier, Keywords.BREAK)) {
      return parseBreakNode();
    } else if (checkCurrentValue(TokenTypes.Identifier, Keywords.CONTINUE)) {
      return parseContinueNode();
    } else if (checkCurrentValues(TokenTypes.Identifier, _builtInMethods)) {
      return parseFunctionCall(true);
    } else if (checkCurrentValues(TokenTypes.Identifier, _keywords) ||
        checkCurrentValues(TokenTypes.Identifier, _constants)) {
      throw new Exception("Syntax error or not supported token.");
    } else if (checkCurrent(TokenTypes.Identifier)) {
      return parseAssigment();
    } else if (checkCurrent(TokenTypes.SymbolLeftBrace)) {
      return parseBlockStatement();
    }

    throw new Exception(
      "Syntax error or not supported token: ${_tokens[_position].type}, ${_tokens[_position].value}.",
    );
  }

  Node parseConditionalLoopStatement() {
    consume(); // while
    consumeIf(TokenTypes.SymbolLeftParentheses); // (

    var condition = parseExpression();

    consumeIf(TokenTypes.SymbolRightParentheses); // )

    var statement = parseStatement();
    return new ConditionalLoopNode(condition, statement);
  }

  Node parseBreakNode() {
    consume(); // continue
    consumeIf(TokenTypes.SymbolSemicolon);

    return new BreakNode();
  }

  Node parseContinueNode() {
    consume(); // continue
    consumeIf(TokenTypes.SymbolSemicolon);

    return new ContinueNode();
  }

  Node parseIfStatement() {
    Node condition;
    Node statement;
    Node? alternate = null;

    consume(); // if or elif
    consumeIf(TokenTypes.SymbolLeftParentheses); // (

    condition = parseExpression();

    consumeIf(TokenTypes.SymbolRightParentheses); // )

    statement = parseStatement();

    if (checkCurrentValue(TokenTypes.Identifier, Keywords.ELIF)) {
      alternate = parseIfStatement();
    } else if (checkCurrentValue(TokenTypes.Identifier, Keywords.ELSE)) {
      consume();
      alternate = parseStatement();
    }

    return new ConditionalStatementNode(condition, statement, alternate);
  }

  Node parseAssigment() {
    Token identifier = _tokens[_position++];
    Token? property = null;
    Node? index = null;

    if (!_variables.contains(identifier.value)) {
      throw new Exception("Variable not declared: ${identifier.value}.");
    }

    if (checkCurrent(TokenTypes.SymbolLeftBracket)) {
      consume(); // [

      if (checkCurrent(TokenTypes.Number)) {
        var number = consume();
        index = new NumericNode(number.value);
      } else if (checkCurrent(TokenTypes.Identifier)) {
        var indeIdentifier = consume();
        index = new IdentifierNode(indeIdentifier.value, null, null);
      } else {
        throw new Exception("Index missing or invalid.");
      }

      consumeIf(TokenTypes.SymbolRightBracket); // ]
    }

    if (checkCurrent(TokenTypes.SymbolDot)) {
      consume(); // .
      property = consumeIf(TokenTypes.Identifier);
    }

    if (index != null && property == null) {
      throw new Exception(
        "Not supported expression: index without slotLogicType.",
      );
    }

    consumeIf(TokenTypes.SymbolEqual); // =

    var expr = parseExpression();

    consumeIf(TokenTypes.SymbolSemicolon);

    return new AssigmentNode(
      new IdentifierNode(identifier.value, index, property?.value),
      expr,
    );
  }

  Node parseBlockStatement() {
    consume(); // {

    List<Node> statements = [];
    while (until(TokenTypes.SymbolRightBrace)) {
      var statement = parseStatement();
      statements.add(statement);
    }

    consumeIf(TokenTypes.SymbolRightBrace); // }
    return new BlockNode(statements);
  }

  Node parseLoopStatement() {
    consume(); // loop
    var statement = parseStatement();
    return new LoopNode(statement);
  }

  Node parseVariableDecleration() {
    consume(); // var

    var identifier = consumeIf(TokenTypes.Identifier);

    if (_variables.contains(identifier.value)) {
      throw new Exception("Variable already declared: ${identifier.value}.");
    }

    _variables.add(identifier.value);

    consumeIf(TokenTypes.SymbolEqual); // =

    var expr = parseExpression();

    consumeIf(TokenTypes.SymbolSemicolon);

    return new VariableDeclerationNode(identifier.value, expr);
  }

  Node parseDeviceConfig() {
    int p = 0;
    List<Token?> arguments = [null, null, null];

    consume(); // Device
    consumeIf(TokenTypes.SymbolLeftParentheses); // (

    while (until(TokenTypes.SymbolRightParentheses)) {
      if (p >= arguments.length) {
        throw new Exception("Too many arguments for DeviceConfiguration.");
      }

      arguments[p++] = consumeIfAny(TokenTypes.Identifier, TokenTypes.String);

      if (!checkCurrent(TokenTypes.SymbolRightParentheses)) {
        consumeIf(TokenTypes.SymbolComma); // ,
      }
    }

    consumeIf(TokenTypes.SymbolRightParentheses); // )

    var type = arguments[0]; // Identifier
    if (type == null) {
      throw new Exception("Invalid Device Configuration.");
    }

    // Device(Type, Port)
    if (arguments[1] != null && isDevicePort(arguments[1]!.value)) {
      return new DeviceConfigNode(type.value, arguments[1]!.value, null, null);
    }

    // Device(Type, "Name", BatchMode)
    if (arguments[1] != null &&
        arguments[1]!.type == TokenTypes.String &&
        arguments[2] != null &&
        isBatchMode(arguments[2]!.value)) {
      return new DeviceConfigNode(
        type.value,
        null,
        arguments[1]!.value,
        arguments[2]!.value,
      );
    }

    // Device(Type, BatchMode)
    if (arguments[1] != null && isBatchMode(arguments[1]!.value)) {
      return new DeviceConfigNode(type.value, null, null, arguments[1]!.value);
    }

    throw new Exception("Invalid Device Configuration.");
  }

  bool isBatchMode(String value) {
    return _batchModes.contains(value);
  }

  bool isDevicePort(String value) {
    final digits = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
    return !(value.length != 2 ||
        value[0] != 'd' ||
        !(value[1] == 'b' || digits.contains(value[1])));
  }

  Node parseFunctionCall(bool statement) {
    List<Node> arguments = [];
    var identifier = consume(); // keyword or identifier
    consumeIf(TokenTypes.SymbolLeftParentheses);

    if (!checkCurrent(TokenTypes.SymbolRightParentheses)) {
      arguments.add(parseExpression());
    }

    while (until(TokenTypes.SymbolRightParentheses)) {
      consumeIf(TokenTypes.SymbolComma);
      arguments.add(parseExpression());
    }

    consumeIf(TokenTypes.SymbolRightParentheses);

    if (statement) {
      consumeIf(TokenTypes.SymbolSemicolon);
    }

    return new CallNode(identifier.value, arguments);
  }

  Node parseExpression() {
    return parseTernary();
  }

  Node parseTernary() {
    var node = parseLogical();

    if (checkCurrent(TokenTypes.SymbolQuestionMark)) {
      consume();
      var left = parseLogical();
      consumeIf(TokenTypes.SymbolColon);
      var right = parseLogical();
      return new TernaryOpNode(node, left, right);
    }

    return node;
  }

  Node parseLogical() {
    var node = parseComparison();

    while (_position < _tokens.length) {
      if (checkCurrent(TokenTypes.SymbolPipe) &&
          checkNext(TokenTypes.SymbolPipe)) {
        consume();
        consume();

        node = new LogicalNode(
          node,
          LogicalOperatorType.OpOr,
          parseComparison(),
        );
      } else if (checkCurrent(TokenTypes.SymbolAnd) &&
          checkNext(TokenTypes.SymbolAnd)) {
        consume();
        consume();

        node = new LogicalNode(
          node,
          LogicalOperatorType.OpAnd,
          parseComparison(),
        );
      } else {
        break;
      }
    }

    return node;
  }

  Node parseComparison() {
    var node = parseUnary();

    while (_position < _tokens.length) {
      if (checkCurrent(TokenTypes.SymbolEqual) &&
          checkNext(TokenTypes.SymbolEqual)) {
        consume();
        consume();

        node = new ComparisonNode(
          node,
          ComparisonOperatorType.OpEqual,
          parseUnary(),
        );
      } else if (checkCurrent(TokenTypes.SymbolExclamationMark) &&
          checkNext(TokenTypes.SymbolEqual)) {
        consume();
        consume();

        node = new ComparisonNode(
          node,
          ComparisonOperatorType.OpNotEqual,
          parseUnary(),
        );
      } else if (checkCurrent(TokenTypes.SymbolLessThen)) {
        consume();

        if (checkCurrent(TokenTypes.SymbolEqual)) {
          consume();

          node = new ComparisonNode(
            node,
            ComparisonOperatorType.OpLessOrEqual,
            parseUnary(),
          );
        } else {
          node = new ComparisonNode(
            node,
            ComparisonOperatorType.OpLess,
            parseUnary(),
          );
        }
      } else if (checkCurrent(TokenTypes.SymbolGreaterThen)) {
        consume();

        if (checkCurrent(TokenTypes.SymbolEqual)) {
          consume();
          node = new ComparisonNode(
            node,
            ComparisonOperatorType.OpGreaterOrEqual,
            parseUnary(),
          );
        } else {
          node = new ComparisonNode(
            node,
            ComparisonOperatorType.OpGreater,
            parseUnary(),
          );
        }
      } else {
        break;
      }
    }

    return node;
  }

  Node parseUnary() {
    if (checkCurrent(TokenTypes.SymbolExclamationMark)) {
      consume();
      var op = UnaryOperationType.OpNot;
      return new UnaryOpNode(parseUnary(), op);
    }

    return parseTerm();
  }

  Node parseTerm() {
    var node = parseFactor();

    while (_position < _tokens.length) {
      if (checkCurrent(TokenTypes.SymbolPlus)) {
        consume();

        node = new BinaryOpNode(
          node,
          ArithmeticOperatorType.OpAdd,
          parseFactor(),
        );
      } else if (checkCurrent(TokenTypes.SymbolMinus)) {
        consume();

        node = new BinaryOpNode(
          node,
          ArithmeticOperatorType.OpSub,
          parseFactor(),
        );
      } else {
        break;
      }
    }

    return node;
  }

  Node parseFactor() {
    var node = parsePrimary();

    while (_position < _tokens.length) {
      if (checkCurrent(TokenTypes.SymbolAsterik)) {
        consume();

        node = new BinaryOpNode(
          node,
          ArithmeticOperatorType.OpMul,
          parsePrimary(),
        );
      } else if (checkCurrent(TokenTypes.SymbolSlash)) {
        consume();

        node = new BinaryOpNode(
          node,
          ArithmeticOperatorType.OpDiv,
          parsePrimary(),
        );
      } else {
        break;
      }
    }

    return node;
  }

  Node parsePrimary() {
    if (checkCurrent(TokenTypes.String)) {
      return new StringNode(_tokens[_position++].value);
    }

    if (checkCurrent(TokenTypes.SymbolMinus) && checkNext(TokenTypes.Number)) {
      _position++; // -

      return new NumericNode("-" + _tokens[_position++].value);
    }

    if (checkCurrent(TokenTypes.SymbolPlus) && checkNext(TokenTypes.Number)) {
      _position++; // +

      return new NumericNode(_tokens[_position++].value);
    }

    if (checkCurrent(TokenTypes.Number)) {
      return new NumericNode(_tokens[_position++].value);
    }

    if (checkCurrentValues(TokenTypes.Identifier, _constants)) {
      if (Utils.isConstantExpression(_tokens[_position].value)) {
        return new ConstantNode(_tokens[_position++].value);
      } else {
        throw new Exception(
          "Not supported constant: ${_tokens[_position].value}.",
        );
      }
    }

    if (checkCurrentValue(TokenTypes.Identifier, Keywords.HASH)) {
      consume();
      consumeIf(TokenTypes.SymbolLeftParentheses);
      var token = consumeIf(TokenTypes.String);
      consumeIf(TokenTypes.SymbolRightParentheses);

      return new HashNode(token.value);
    }

    if (checkCurrentValue(TokenTypes.Identifier, Keywords.DEVICE)) {
      return parseDeviceConfig();
    }

    if (checkCurrentValues(TokenTypes.Identifier, _builtInFunctions)) {
      return parseFunctionCall(false);
    }

    if (checkCurrent(TokenTypes.SymbolLeftParentheses)) {
      consume();
      var expr = parseExpression();
      consumeIf(TokenTypes.SymbolRightParentheses);
      return expr;
    }

    if (checkCurrent(TokenTypes.Identifier)) {
      Token identifier = _tokens[_position++];
      Token? property = null;
      Node? index = null;

      if (!_variables.contains(identifier.value)) {
        throw new Exception("Variable not declared: ${identifier.value}.");
      }

      if (checkCurrent(TokenTypes.SymbolLeftBracket)) {
        consume(); // [

        if (checkCurrent(TokenTypes.Number)) {
          var number = consume();
          index = new NumericNode(number.value);
        } else if (checkCurrent(TokenTypes.Identifier)) {
          var indeIdentifier = consume();
          index = new IdentifierNode(indeIdentifier.value, null, null);
        } else {
          throw new Exception(
            "Index missing or invalid (${identifier.value}).",
          );
        }

        consumeIf(TokenTypes.SymbolRightBracket); // ]
      }

      if (checkCurrent(TokenTypes.SymbolDot)) {
        consume(); // .

        if (checkCurrent(TokenTypes.Identifier)) {
          property = _tokens[_position++];
        } else {
          throw new Exception(
            "LogicType or Property missing (${identifier.value}).",
          );
        }
      }

      if (index != null && property == null) {
        throw new Exception(
          "Not supported expression: index without slotLogicType.",
        );
      }

      return new IdentifierNode(identifier.value, index, property?.value);
    }

    throw new Exception("Not supported expression token.");
  }

  Token consumeIfAny(TokenTypes type1, TokenTypes type2) {
    if (checkCurrent(type1) || checkCurrent(type2)) {
      return _tokens[_position++];
    } else {
      Token? current = (_position < _tokens.length ? _tokens[_position] : null);
      if (current == null) {
        throw new Exception("Unexpected end of file.");
      } else {
        throw new Exception(
          "Unexpected token: ${current.type}, ${current.value} (Expect: ${type1} or ${type2}).",
        );
      }
    }
  }

  Token consumeIf(TokenTypes type) {
    if (checkCurrent(type)) {
      return _tokens[_position++];
    } else {
      Token? current = (_position < _tokens.length ? _tokens[_position] : null);
      if (current == null) {
        throw new Exception("Unexpected end of file.");
      } else {
        throw new Exception(
          "Unexpected token: ${current.type}, ${current.value} (Expect: ${type}).",
        );
      }
    }
  }

  Token consume() {
    return _tokens[_position++];
  }

  bool until(TokenTypes type) {
    return _position < _tokens.length && _tokens[_position].type != type;
  }

  bool checkNext(TokenTypes type) {
    return _position + 1 < _tokens.length &&
        _tokens[_position + 1].type == type;
  }

  bool checkCurrent(TokenTypes type) {
    return _position < _tokens.length && _tokens[_position].type == type;
  }

  bool checkCurrentValue(TokenTypes type, String value) {
    return _position < _tokens.length &&
        _tokens[_position].type == type &&
        value == _tokens[_position].value;
  }

  bool checkCurrentValues(TokenTypes type, Set<String> values) {
    return _position < _tokens.length &&
        _tokens[_position].type == type &&
        values.contains(_tokens[_position].value);
  }
}
