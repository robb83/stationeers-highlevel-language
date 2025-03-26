using System;
using System.Collections.Generic;

namespace Stationeers.Compiler.AST
{
    public abstract class Node
    {
    }

    public enum LogicalOperatorType
    {
        OpAnd,
        OpOr
    }

    public enum ComparsionOperatorType
    {
        OpEqual,
        OpNotEqual,
        OpLess,
        OpLessOrEqual,
        OpGreater,
        OpGreaterOrEqual
    }

    public enum OperatorType
    {
        OpAdd,
        OpSub,
        OpMul,
        OpDiv
    }

    public enum UnaryOperationType
    {
        OpNot
    }

    public class ProgramNode : Node
    {
        public List<Node> Statements;

        public ProgramNode(List<Node> statements)
        {
            Statements = statements;
        }

        public bool IsEmpty()
        {
            return Statements != null && Statements.Count > 0;
        }
    }

    public class BlockNode : Node
    {
        public List<Node> Statements;

        public BlockNode(List<Node> statements)
        {
            Statements = statements;
        }

        public bool IsEmpty()
        {
            return Statements != null && Statements.Count > 0;
        }
    }

    public class VariableDeclerationNode : Node
    {
        public String Identifier;
        public Node Expression;

        public VariableDeclerationNode(string identifier, Node expression)
        {
            Identifier = identifier;
            Expression = expression;
        }
    }

    public class AssigmentNode : Node
    {
        public IdentifierNode Identifier;
        public Node Expression;

        public AssigmentNode(IdentifierNode identifier, Node expression)
        {
            Identifier = identifier;
            Expression = expression;
        }
    }

    public class LoopNode : Node
    {
        public Node Statement;

        public LoopNode(Node statement)
        {
            Statement = statement;
        }
    }

    public class ConditionalLoopNode : Node
    {
        public Node Condition;
        public Node Statement;

        public ConditionalLoopNode(Node condition, Node statement)
        {
            Condition = condition;
            Statement = statement;
        }
    }

    public class ConditionalStatementNode : Node
    {
        public Node Condition;
        public Node Statement;
        public Node Alternate;

        public ConditionalStatementNode(Node condition, Node statement, Node alternate)
        {
            Condition = condition;
            Statement = statement;
            Alternate = alternate;
        }
    }

    public class BreakNode : Node
    {
    }

    public class ContinueNode : Node
    {
    }

    public class CallNode : Node
    {
        public String Identifier;
        public List<Node> Arguments;

        public CallNode(String identifier, List<Node> argumetns)
        {
            Identifier = identifier;
            Arguments = argumetns;
        }
    }

    public class DeviceConfigNode : Node
    {
        public String Type;
        public String Port;
        public String Name;
        public String BatchMode;

        public DeviceConfigNode(String type, String port, String name, String batchMode)
        {
            Type = type;
            Port = port;
            Name = name;
            BatchMode = batchMode;
        }
    }

    public class StringNode : Node
    {
        public String Value;

        public StringNode(string value)
        {
            Value = value;
        }
    }

    public class NumericNode : Node
    {
        public String Value;

        public NumericNode(string value)
        {
            Value = value;
        }
    }

    public class IdentifierNode : Node
    {
        public String Identifier;
        public String Property;
        public Node Index;

        public IdentifierNode(string identifier, Node index = null, string property = null)
        {
            Identifier = identifier;
            Index = index;
            Property = property;
        }
    }

    public class UnaryOpNode : Node
    {
        public Node Expression;
        public UnaryOperationType Operator;

        public UnaryOpNode(Node expression, UnaryOperationType op)
        {
            Expression = expression;
            Operator = op;
        }
    }

    public class BinaryOpNode : Node
    {
        public Node Left, Right;
        public OperatorType Operator;

        public BinaryOpNode(Node left, OperatorType op, Node right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    public class ComparisonNode : Node
    {
        public Node Left, Right;
        public ComparsionOperatorType Operator;

        public ComparisonNode(Node left, ComparsionOperatorType op, Node right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    public class LogicalNode : Node
    {
        public Node Left, Right;
        public LogicalOperatorType Operator;

        public LogicalNode(Node left, LogicalOperatorType op, Node right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}
