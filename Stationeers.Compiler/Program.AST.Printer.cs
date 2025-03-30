using System;
using System.Text;

namespace Stationeers.Compiler.AST
{
    public class Printer
    {
        private Node _programNode;
        private StringBuilder _buffer = new StringBuilder();

        public Printer(Node programNode)
        {
            _programNode = programNode;
        }

        public String Print()
        {
            _buffer.Clear();
            Print(_programNode, 0);
            return _buffer.ToString();
        }

        private void Print(Node n, int level)
        {
            if (n is ProgramNode pn)
            {
                if (pn.Statements != null)
                {
                    for (int i = 0; i < pn.Statements.Count; ++i)
                    {
                        Write(level, String.Empty);
                        Print(pn.Statements[i], level);
                        WriteLine();
                    }
                }
            }
            else if (n is BlockNode bn)
            {
                WriteLine("{");

                if (bn.Statements != null)
                {
                    for (int i = 0; i < bn.Statements.Count; ++i)
                    {
                        Write(level + 1, String.Empty);
                        Print(bn.Statements[i], level + 1);
                        WriteLine();
                    }
                }

                Write(level, "}");
            } 
            else if (n is CallNode cn)
            {
                Write(cn.Identifier);
                Write("(");
                if (cn.Arguments != null)
                {
                    for ( int i = 0; i < cn.Arguments.Count; ++i)
                    {
                        if (i > 0)
                        {
                            Write(", ");
                        }

                        Print(cn.Arguments[i], level + 1);
                    }
                }

                Write(")");
            }
            else if (n is LoopNode ln)
            {
                Write(Keywords.LOOP);
                Write(level, String.Empty);
                Print(ln.Statement, level);
            }
            else if (n is ConditionalLoopNode cln)
            {
                Write(Keywords.WHILE);
                Write(" (");
                Print(cln.Condition, level);
                WriteLine(")");
                Write(level, String.Empty);
                Print(cln.Statement, level);
            }
            else if (n is ConditionalStatementNode csn)
            {
                Write(Keywords.IF);
                Write(" (");
                Print(csn.Condition, level);
                WriteLine(")");
                Write(level, String.Empty);
                Print(csn.Statement, level);

                if (csn.Alternate != null)
                {
                    WriteLine(level, Keywords.ELSE);
                    Write(level, String.Empty);
                    Print(csn.Alternate, level);
                }
            }
            else if (n is VariableDeclerationNode vdn)
            {
                Write(Keywords.VAR);
                Write(" ");
                Write(vdn.Identifier);
                Write(" = ");
                Print(vdn.Expression, level + 1);
            }
            else if (n is AssigmentNode an)
            {
                Print(an.Identifier, level);
                Write(" = ");
                Print(an.Expression, level);
            }
            else if (n is BinaryOpNode bon)
            {
                Write("(");
                Print(bon.Left, level);
                Write(" ");
                Write(Convert(bon.Operator));
                Write(" ");
                Print(bon.Right, level);
                Write(")");
            }
            else if (n is ComparisonNode con)
            {
                Write("(");
                Print(con.Left, level);
                Write(" ");
                Write(Convert(con.Operator));
                Write(" ");
                Print(con.Right, level);
                Write(")");
            }
            else if (n is LogicalNode logn)
            {
                Write("(");
                Print(logn.Left, level);
                Write(" ");
                Write(Convert(logn.Operator));
                Write(" ");
                Print(logn.Right, level);
                Write(")");
            }
            else if (n is UnaryOpNode uon)
            {
                Write(Convert(uon.Operator));
                Print(uon.Expression, level);
            }
            else if (n is TernaryOpNode ton)
            {
                Write("(");
                Print(ton.Condition, level);
                Write("?");
                Print(ton.Left, level);
                Write(":");
                Print(ton.Right, level);
                Write(")");
            }
            else if (n is DeviceConfigNode dcn)
            {
                Write(Keywords.DEVICE);
                Write("(");
                Write(dcn.Type);

                if (dcn.Port != null)
                {
                    Write(", ");
                    Write(dcn.Port);
                }
                else if (dcn.Name != null)
                {
                    Write(", \"");
                    Write(dcn.Name);
                    Write("\", ");
                    Write(dcn.BatchMode);
                }
                else
                {
                    Write(", ");
                    Write(dcn.BatchMode);
                }

                Write(")");
            }
            else if (n is ContinueNode)
            {
                Write(Keywords.CONTINUE);
            }
            else if (n is BreakNode)
            {
                Write(Keywords.BREAK);
            }
            else if (n is NumericNode nn)
            {
                Write(nn.Value);
            }
            else if (n is ConstantNode constn)
            {
                Write(constn.Value);
            }
            else if (n is HashNode hashn)
            {
                Write("HASH(\"");
                Write(hashn.Value);
                Write("\")");
            }
            else if (n is IdentifierNode idn)
            {
                Write(idn.Identifier);

                if (idn.Index != null)
                {
                    Write("[");
                    Print(idn.Index, level);
                    Write("]");
                }

                if (idn.Property != null)
                {
                    Write(".");
                    Write(idn.Property);
                }
            }
            else
            {
                throw new Exception($"Not Supported node ({n?.GetType()?.Name}).");
            }
        }

        private void WriteLine()
        {
            _buffer.AppendLine();
        }

        private void WriteLine(String message)
        {
            _buffer.AppendLine(message);
        }

        private void Write(String message)
        {
            _buffer.Append(message);
        }

        private void Write(int level, String message)
        {
            _buffer.Append(new string(' ', level * 2));
            _buffer.Append(message);
        }

        private void WriteLine(int level, String message)
        {
            _buffer.Append(new string(' ', level * 2));
            _buffer.AppendLine(message);
        }

        private String Convert(LogicalOperatorType t)
        {
            switch (t)
            {
                case LogicalOperatorType.OpAnd:
                    return "&&";
                case LogicalOperatorType.OpOr:
                    return "||";
                default:
                    throw new Exception($"Not supported operator ({t}).");
            }
        }

        private String Convert(ComparisonOperatorType t)
        {
            switch (t)
            {
                case ComparisonOperatorType.OpLess:
                    return "<";
                case ComparisonOperatorType.OpLessOrEqual:
                    return "<=";
                case ComparisonOperatorType.OpEqual:
                    return "==";
                case ComparisonOperatorType.OpNotEqual:
                    return "!=";
                case ComparisonOperatorType.OpGreater:
                    return ">";
                case ComparisonOperatorType.OpGreaterOrEqual:
                    return ">=";
                default:
                    throw new Exception($"Not supported operator ({t}).");
            }
        }

        private String Convert(ArithmeticOperatorType t)
        {
            switch (t)
            {
                case ArithmeticOperatorType.OpMul:
                    return "*";
                case ArithmeticOperatorType.OpDiv:
                    return "/";
                case ArithmeticOperatorType.OpSub:
                    return "-";
                case ArithmeticOperatorType.OpAdd:
                    return "+";
                default:
                    throw new Exception($"Not supported operator ({t}).");
            }
        }

        private String Convert(UnaryOperationType t)
        {
            switch (t)
            {
                case UnaryOperationType.OpNot:
                    return "!";
                default:
                    throw new Exception($"Not supported operator ({t}).");
            }
        }

    }
}
