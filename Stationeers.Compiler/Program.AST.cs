using System;
using System.Collections.Generic;

namespace Stationeers.Compiler
{
    public abstract class Node
    {
        public abstract void Print(int depth = 0);
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

        public override void Print(int depth = 0)
        {
            if (Statements != null)
            {
                foreach (var s in Statements)
                {
                    s.Print(depth + 1);
                }
            }
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

        public override void Print(int depth = 0)
        {
            if (Statements != null)
            {
                foreach (var s in Statements)
                {
                    s.Print(depth + 1);
                }
            }
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

        public override void Print(int depth = 0)
        {
            Console.WriteLine(new string(' ', depth * 2) + Identifier + " = ");
            Expression.Print(depth + 1);
        }
    }

    public class AssigmentNode : Node
    {
        public String Identifier;
        public String Property;
        public Node Index;
        public Node Expression;

        public AssigmentNode(string identifier, Node index, string property, Node expression)
        {
            Identifier = identifier;
            Index = index;
            Property = property;
            Expression = expression;
        }

        public override void Print(int depth = 0)
        {
            if (Property != null)
            {
                Console.WriteLine(new string(' ', depth * 2) + Identifier + "." + Property + " = ");
            }
            else
            {
                Console.WriteLine(new string(' ', depth * 2) + Identifier + " = ");
            }

            Expression.Print(depth + 1);
        }
    }

    public class WhileStatementNode : Node
    {
        public Node Condition;
        public Node Statement;

        public WhileStatementNode(Node condition, Node statement)
        {
            Condition = condition;
            Statement = statement;
        }

        public override void Print(int depth = 0)
        {
            Console.WriteLine(new string(' ', depth * 2) + "while");
            Condition.Print(depth + 1);
            Statement.Print(depth + 1);
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

        public override void Print(int depth = 0)
        {
            Console.WriteLine(new string(' ', depth * 2) + "while");
            Condition.Print(depth + 1);
            Statement.Print(depth + 1);
        }
    }

    public class BreakNode : Node
    {
        public override void Print(int depth = 0)
        {

        }
    }

    public class ContinueNode : Node
    {
        public override void Print(int depth = 0)
        {

        }
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

        public override void Print(int depth = 0)
        {

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

        public override void Print(int depth = 0)
        {

        }
    }

    class StringNode : Node
    {
        public String Value;

        public StringNode(string value)
        {
            Value = value;
        }

        public override void Print(int depth = 0)
        {

        }
    }

    class NumericNode : Node
    {
        public String Value;

        public NumericNode(string value)
        {
            Value = value;
        }

        public override void Print(int depth = 0)
        {

        }
    }

    class IdentifierNode : Node
    {
        public String Identifier;
        public String Property;
        public Node Index;

        public IdentifierNode(string identifier)
        {
            Identifier = identifier;
        }

        public IdentifierNode(string identifier, Node index, string property)
        {
            Identifier = identifier;
            Index = index;
            Property = property;
        }

        public override void Print(int depth = 0)
        {

        }
    }

    class UnaryOpNode : Node
    {
        public Node Expression;
        public UnaryOperationType Operator;

        public UnaryOpNode(Node expression, UnaryOperationType op)
        {
            Expression = expression;
            Operator = op;
        }

        public override void Print(int depth = 0)
        {

        }
    }

    class BinaryOpNode : Node
    {
        public Node Left, Right;
        public OperatorType Operator;

        public BinaryOpNode(Node left, OperatorType op, Node right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override void Print(int depth = 0)
        {

        }
    }

    class ComparisonNode : Node
    {
        public Node Left, Right;
        public ComparsionOperatorType Operator;

        public ComparisonNode(Node left, ComparsionOperatorType op, Node right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override void Print(int depth = 0)
        {

        }
    }
}
