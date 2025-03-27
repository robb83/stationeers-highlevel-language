using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Hashing;
using System.Text;

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

                if (Utils.IsValueNode(condition))
                {
                    if (Utils.IsTrue(condition))
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

                if (Utils.IsValueNode(condition))
                {
                    if (Utils.IsTrue(condition))
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

                if (Utils.IsValueNode(left) && Utils.IsValueNode(right))
                {
                    return Eval(left, right, op);
                }

                if (op == OperatorType.OpMul)
                {
                    if (left is NumericNode && Utils.IsOne(left))
                    {
                        return right;
                    }

                    if (left is NumericNode && Utils.IsZero(left))
                    {
                        return new NumericNode("0");
                    }

                    if (right is NumericNode && Utils.IsOne(right))
                    {
                        return left;
                    }

                    if (right is NumericNode && Utils.IsZero(right))
                    {
                        return new NumericNode("0");
                    }
                }

                if (op == OperatorType.OpAdd)
                {
                    if (left is NumericNode && Utils.IsZero(left))
                    {
                        return right;
                    }
                    
                    if (right is NumericNode && Utils.IsZero(right))
                    {
                        return left;
                    }
                }

                if (op == OperatorType.OpSub)
                {
                    // TODO: same for identifiers
                    if (right is NumericNode r && left is NumericNode l && Utils.IsEqual(l, r))
                    {
                        return new NumericNode("0");
                    }
                        
                    if (right is NumericNode && Utils.IsZero(right))
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

                if (Utils.IsValueNode(left) && Utils.IsValueNode(right))
                {
                    return Eval(left, right, op);
                }

                return new ComparisonNode(left, con.Operator, right);
            }
            else if (n is LogicalNode logn)
            {
                var left = Simplify(logn.Left);
                var right = Simplify(logn.Right);
                var op = logn.Operator;

                if (Utils.IsValueNode(left) && Utils.IsValueNode(right))
                {
                    return Eval(left, right, op);
                }

                return new LogicalNode(left, logn.Operator, right);
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
            else if (n is ConstantNode constn)
            {
                return new ConstantNode(constn.Value);
            }
            else if (n is HashNode hashn)
            {
                return new HashNode(hashn.Value);
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

        private NumericNode Eval(Node left, Node right, LogicalOperatorType lop)
        {
            bool result = false;

            switch (lop)
            {
                case LogicalOperatorType.OpAnd:
                    result = Utils.IsTrue(left) && Utils.IsTrue(right);
                    break;
                case LogicalOperatorType.OpOr:
                    result = Utils.IsTrue(left) || Utils.IsTrue(right);
                    break;
                default:
                    throw new Exception("Not supported operation.");
            }

            return new NumericNode(result ? "1" : "0");
        }

        private NumericNode Eval(Node left, Node right, OperatorType op)
        {
            double result;
            switch (op)
            {
                case OperatorType.OpAdd:
                    result = Utils.GetValue(left) + Utils.GetValue(right);
                    break;
                case OperatorType.OpSub:
                    result = Utils.GetValue(left) - Utils.GetValue(right);
                    break;
                case OperatorType.OpMul:
                    result = Utils.GetValue(left) * Utils.GetValue(right);
                    break;
                case OperatorType.OpDiv:
                    result = Utils.GetValue(left) / Utils.GetValue(right);
                    break;
                default:
                    throw new Exception("Not supported operation.");
            }

            return new NumericNode(result.ToString(CultureInfo.InvariantCulture));
        }

        private NumericNode Eval(Node left, Node right, ComparsionOperatorType op)
        {
            bool result;
            switch (op)
            {
                case ComparsionOperatorType.OpLess:
                    result = Utils.GetValue(left) < Utils.GetValue(right);
                    break;
                case ComparsionOperatorType.OpLessOrEqual:
                    result = Utils.GetValue(left) <= Utils.GetValue(right);
                    break;
                case ComparsionOperatorType.OpEqual:
                    result = Utils.GetValue(left) == Utils.GetValue(right);
                    break;
                case ComparsionOperatorType.OpNotEqual:
                    result = Utils.GetValue(left) != Utils.GetValue(right);
                    break;
                case ComparsionOperatorType.OpGreater:
                    result = Utils.GetValue(left) > Utils.GetValue(right);
                    break;
                case ComparsionOperatorType.OpGreaterOrEqual:
                    result = Utils.GetValue(left) >= Utils.GetValue(right);
                    break;
                default:
                    throw new Exception("Not supported operation.");
            }

            return new NumericNode( result ? "1" : "0" );
        }
    }
}
