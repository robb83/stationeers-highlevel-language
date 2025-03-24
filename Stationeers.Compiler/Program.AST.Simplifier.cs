using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.AccessControl;

namespace Stationeers.Compiler.AST
{
    public class Simplifier
    {
        private ProgramNode _programNode;

        public Simplifier(ProgramNode programNode)
        {
            _programNode = programNode;
        }

        public Node Simplify()
        {
            return Simplify(_programNode);
        }

        private Node Simplify(Node n)
        {
            if (n is ProgramNode pn)
            {
                List<Node> statements = new List<Node>();

                if (pn.Statements != null)
                {
                    for (int i = 0; i < pn.Statements.Count; ++i)
                    {
                        var statement = Simplify(pn.Statements[i]);
                        if (statement != null)
                        {
                            statements.Add(statement);
                        }
                    }
                }

                return new ProgramNode(statements);
            }
            else if (n is BlockNode bn)
            {
                if (bn.Statements != null)
                {
                    List<Node> statements = new List<Node>();

                    for (int i = 0; i < bn.Statements.Count; ++i)
                    {
                        var statement = Simplify(bn.Statements[i]);
                        if (statement != null)
                        {
                            statements.Add(statement);
                        }
                    }

                    if (statements.Count > 0)
                    {
                        return new BlockNode(statements);
                    }
                }

                return null;
            }
            else if (n is LoopNode ln)
            {
                var statement = Simplify(ln.Statement);
                if (statement == null)
                {
                    return null;
                }

                return new LoopNode(statement);
            }
            else if (n is ConditionalLoopNode cln)
            {
                var condition = Simplify(cln.Condition);
                var statement = Simplify(cln.Statement);

                if (condition is NumericNode)
                {
                    if (IsTrue((NumericNode)condition))
                    {
                        return new LoopNode(statement);
                    } 
                    else
                    {
                        return null;
                    }
                }
                
                return new ConditionalLoopNode(condition, statement);
            }
            else if (n is ConditionalStatementNode csn)
            {
                var condition = Simplify(csn.Condition);
                var statement = Simplify(csn.Statement);
                var alternate = ( csn.Alternate != null ? Simplify(csn.Alternate) : (Node)null);

                if (condition is NumericNode)
                {
                    if (IsTrue((NumericNode)condition))
                    {
                        return statement;
                    }
                    else
                    {
                        return alternate;
                    }
                }
                else
                {
                    return new ConditionalStatementNode(condition, statement, alternate);
                }
            }
            else if (n is CallNode cn)
            {
                var identifier = cn.Identifier;
                List<Node> arguments = null;

                if (cn.Arguments != null && cn.Arguments.Count > 0)
                {
                    arguments = new List<Node>();
                    for ( int i = 0; i < cn.Arguments.Count; ++i)
                    {
                        arguments.Add(Simplify(cn.Arguments[i]));
                    }
                }

                return new CallNode(identifier, arguments);
            }
            else if (n is VariableDeclerationNode vdn)
            {
                var identifier = vdn.Identifier;
                var expression = Simplify(vdn.Expression);

                return new VariableDeclerationNode(identifier, expression);
            }
            else if (n is AssigmentNode an)
            {
                var identifier = Copy(an.Identifier);
                var expression = Simplify(an.Expression);

                return new AssigmentNode(identifier, expression);
            }
            else if (n is BinaryOpNode bon)
            {
                var left = Simplify(bon.Left);
                var right = Simplify(bon.Right);
                var op = bon.Operator;

                if (left is NumericNode && right is NumericNode)
                {
                    return Eval(left, right, op);
                }

                if (op == OperatorType.OpMul)
                {
                    if (left is NumericNode && IsOne((NumericNode)left))
                    {
                        return right;
                    }

                    if (left is NumericNode && IsZero((NumericNode)left))
                    {
                        return new NumericNode("0");
                    }

                    if (right is NumericNode && IsOne((NumericNode)right))
                    {
                        return left;
                    }

                    if (right is NumericNode && IsZero((NumericNode)right))
                    {
                        return new NumericNode("0");
                    }
                }

                if (op == OperatorType.OpAdd)
                {
                    if (left is NumericNode && IsZero((NumericNode)left))
                    {
                        return right;
                    }
                    
                    if (right is NumericNode && IsZero((NumericNode)right))
                    {
                        return left;
                    }
                }

                if (op == OperatorType.OpSub)
                {
                    // TODO: same for identifiers
                    if (right is NumericNode r && left is NumericNode l && IsEqual(l, r))
                    {
                        return new NumericNode("0");
                    }
                        
                    if (right is NumericNode && IsZero((NumericNode)right))
                    {
                        return left;
                    }
                }

                return new BinaryOpNode(left, op, right);
            }
            else if (n is ComparisonNode con)
            {
                var left = Simplify(con.Left);
                var right = Simplify(con.Right);
                var op = con.Operator;

                if (left is NumericNode && right is NumericNode)
                {
                    return Eval(left, right, op);
                }

                return new ComparisonNode(left, con.Operator, right);
            }
            else if (n is UnaryOpNode uon)
            {
                var expr = Simplify(uon.Expression);
                return new UnaryOpNode(expr, uon.Operator);
            }
            else if (n is DeviceConfigNode dcn)
            {
                return new DeviceConfigNode(dcn.Type, dcn.Port, dcn.Name, dcn.BatchMode);
            }
            else if (n is ContinueNode)
            {
                return new ContinueNode();
            }
            else if (n is BreakNode)
            {
                return new BreakNode();
            }
            else if (n is NumericNode nn)
            {
                return new NumericNode(nn.Value);
            }
            else if (n is IdentifierNode idn)
            {
                return Copy(idn);
            }
            else
            {
                throw new Exception("Not Supported node.");
            }
        }

        private IdentifierNode Copy(IdentifierNode idn)
        {
            if (idn == null)
            {
                return null;
            }

            Node index = null;

            if (idn.Index != null)
            {
                if (idn.Index is NumericNode inn)
                {
                    index = new NumericNode(inn.Value);
                }
                else if (idn.Index is IdentifierNode iin)
                {
                    index = Copy(iin);
                }
                else
                {
                    throw new Exception("Not supported index.");
                }
            }

            return new IdentifierNode(idn.Identifier, index, idn.Property);
        }

        private NumericNode Eval(Node left, Node right, OperatorType op)
        {
            //TODO:  Nan, Infinite, ZeroDiveder

            double l = Double.Parse(((NumericNode)left).Value, CultureInfo.InvariantCulture);
            double r = Double.Parse(((NumericNode)right).Value, CultureInfo.InvariantCulture);

            double result;
            switch (op)
            {
                case OperatorType.OpAdd:
                    result = l + r;
                    break;
                case OperatorType.OpSub:
                    result = l - r;
                    break;
                case OperatorType.OpMul:
                    result = l * r;
                    break;
                case OperatorType.OpDiv:
                    result = l / r;
                    break;
                default:
                    throw new Exception("Not supported operation.");
            }

            return new NumericNode(result.ToString(CultureInfo.InvariantCulture));
        }

        private NumericNode Eval(Node left, Node right, ComparsionOperatorType op)
        {
            double l = Double.Parse(((NumericNode)left).Value, CultureInfo.InvariantCulture);
            double r = Double.Parse(((NumericNode)right).Value, CultureInfo.InvariantCulture);

            bool result;
            switch (op)
            {
                case ComparsionOperatorType.OpLess:
                    result = l < r;
                    break;
                case ComparsionOperatorType.OpLessOrEqual:
                    result = l <= r;
                    break;
                case ComparsionOperatorType.OpEqual:
                    result = l == r;
                    break;
                case ComparsionOperatorType.OpNotEqual:
                    result = l != r;
                    break;
                case ComparsionOperatorType.OpGreater:
                    result = l > r;
                    break;
                case ComparsionOperatorType.OpGreaterOrEqual:
                    result = l >= r;
                    break;
                default:
                    throw new Exception("Not supported operation.");
            }

            return new NumericNode( result ? "1" : "0" );
        }

        private bool IsEqual(NumericNode left, NumericNode right)
        {
            return String.Compare(left.Value, right.Value, StringComparison.Ordinal) == 0;
        }

        private bool IsZero(NumericNode nn)
        {
            double n = Double.Parse(nn.Value, CultureInfo.InvariantCulture);
            return !(n != 0.0); // Or Math.Abs(n) < Double.Epsilon ?
        }

        private bool IsOne(NumericNode nn)
        {
            double n = Double.Parse(nn.Value, CultureInfo.InvariantCulture);
            return n == 1.0;
        }

        private bool IsTrue(NumericNode nn)
        {
            return Double.Parse(nn.Value, CultureInfo.InvariantCulture) >= 1;
        }
    }
}
