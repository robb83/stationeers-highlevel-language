  abstract class Node
  {
  }

  enum LogicalOperatorType
  {
      OpAnd,
      OpOr
  }

  enum ComparisonOperatorType
  {
      OpEqual,
      OpNotEqual,
      OpLess,
      OpLessOrEqual,
      OpGreater,
      OpGreaterOrEqual
  }

  enum ArithmeticOperatorType
  {
      OpAdd,
      OpSub,
      OpMul,
      OpDiv
  }

  enum UnaryOperationType
  {
      OpNot
  }

  class ProgramNode extends Node
  {
      List<Node> statements;

      ProgramNode(this.statements);
  }

  class BlockNode extends Node
  {
      List<Node> statements;

      BlockNode(this.statements);
  }

  class VariableDeclerationNode extends Node
  {
      String identifier;
      Node expression;

      VariableDeclerationNode(this.identifier, this.expression);
  }

  class AssigmentNode extends Node
  {
      IdentifierNode identifier;
      Node expression;

      AssigmentNode(this.identifier, this.expression);
  }

  class LoopNode extends Node
  {
      Node statement;

      LoopNode(this.statement);
  }

  class ConditionalLoopNode extends Node
  {
      Node condition;
      Node statement;

      ConditionalLoopNode(this.condition, this.statement);
  }

  class ConditionalStatementNode extends Node
  {
      Node condition;
      Node statement;
      Node? alternate;

      ConditionalStatementNode(this.condition, this.statement, this.alternate);
  }

  class BreakNode extends Node
  {
  }

  class ContinueNode extends Node
  {
  }

  class CallNode extends Node
  {
      String identifier;
      List<Node> arguments;

      CallNode(this.identifier, this.arguments);
  }

  class DeviceConfigNode extends Node
  {
      String type;
      String? port;
      String? name;
      String? batchMode;

      DeviceConfigNode(this.type, this.port, this.name, this.batchMode);
  }

  class StringNode extends Node
  {
      String value;

      StringNode(this.value);
  }

  class NumericNode extends Node
  {
      String value;

      NumericNode(this.value);
  }

  class ConstantNode extends Node
  {
      String value;

      ConstantNode(this.value);
  }

  class HashNode extends Node
  {
      String value;

      HashNode(this.value);
  }

  class IdentifierNode extends Node
  {
      String identifier;
      String? property;
      Node? index;

      IdentifierNode(this.identifier, this.index, this.property);
  }

  class UnaryOpNode extends Node
  {
      Node expression;
      UnaryOperationType operator;

      UnaryOpNode(this.expression, this.operator);
  }

  class BinaryOpNode extends Node
  {
      Node left, right;
      ArithmeticOperatorType operator;

      BinaryOpNode(this.left, this.operator, this.right);
  }

  class TernaryOpNode extends Node
  {
      Node condition, left, right;

      TernaryOpNode(this.condition, this.left, this.right);
  }

  class ComparisonNode extends Node
  {
      Node left, right;
      ComparisonOperatorType operator;

      ComparisonNode(this.left, this.operator, this.right);
  }

  class LogicalNode extends Node
  {
      Node left, right;
      LogicalOperatorType operator;

      LogicalNode(this.left, this.operator, this.right);
  }