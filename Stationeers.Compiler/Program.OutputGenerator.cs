using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Hashing;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using Stationeers.Compiler.AST;

namespace Stationeers.Compiler
{
    public class OutputGenerator
    {
        const int MIN_REGISTER = 0;
        const int MAX_REGISTER = 15;
        const int STACK_SIZE = 512;

        Dictionary<String, int> _labelCounter;
        Stack<String> _breakPoints;
        Stack<String> _continuePoints;
        Dictionary<String, DeviceConfigNode> _devices;
        bool[] _registers = new bool[18];
        Dictionary<String, int> _variables;
        Node _programNode;
        StringBuilder _output;

        public OutputGenerator(Node programNode)
        {
            _labelCounter = new Dictionary<string, int>();
            _breakPoints = new Stack<string>();
            _continuePoints = new Stack<string>();
            _devices = new Dictionary<string, DeviceConfigNode>();
            _programNode = programNode;
            _variables = new Dictionary<string, int>();
            _output = new StringBuilder();
        }

        public String Print()
        {
            int r = ReserveRegister();
            GenerateMipsCode(_programNode, r);
            FreeRegister(r);
            return _output.ToString();
        }

        private void GenerateMipsCode(Node node, int r)
        {
            switch (node)
            {
                case ProgramNode program:
                    if (program.Statements != null)
                    {
                        foreach (var s in program.Statements)
                        {
                            GenerateMipsCode(s, r);
                        }
                    }
                    break;
                case BlockNode block:
                    if (block.Statements != null)
                    {
                        foreach (var s in block.Statements)
                        {
                            GenerateMipsCode(s, r);
                        }
                    }
                    break;
                case ConditionalStatementNode csn:
                    {
                        if (csn.Alternate != null)
                        {
                            IsConstantExpressionAndTrue(csn.Condition, out bool constant, out bool isTrue);

                            if (!constant)
                            {
                                var label1 = GenerateLabel("if_else");
                                var label2 = GenerateLabel("if_end");

                                GenerateOpositeJump(csn.Condition, r, label1);
                                GenerateMipsCode(csn.Statement, r);
                                WriteLine("j " + label2);
                                WriteLine(label1 + ":");
                                GenerateMipsCode(csn.Alternate, r);
                                WriteLine(label2 + ":");
                            } 
                            else if (isTrue)
                            {
                                GenerateMipsCode(csn.Statement, r);
                            }
                            else
                            {
                                GenerateMipsCode(csn.Alternate, r);
                            }
                        }
                        else
                        {
                            IsConstantExpressionAndTrue(csn.Condition, out bool constant, out bool isTrue);

                            if (!constant)
                            {
                                var label = GenerateLabel("if");
                                GenerateOpositeJump(csn.Condition, r, label);
                                GenerateMipsCode(csn.Statement, r);
                                WriteLine(label + ":");
                            } 
                            else if (isTrue)
                            {
                                GenerateMipsCode(csn.Statement, r);
                            }
                        }

                        break;
                    }
                case ConditionalLoopNode cln:
                    {
                        IsConstantExpressionAndTrue(cln.Condition, out bool constant, out bool isTrue);

                        if (!constant)
                        {
                            var label1 = GenerateLabel("while_start");
                            var label2 = GenerateLabel("while_end");

                            _continuePoints.Push(label1);
                            _breakPoints.Push(label2);

                            WriteLine(label1 + ":");
                            GenerateOpositeJump(cln.Condition, r, label2);
                            GenerateMipsCode(cln.Statement, r);
                            WriteLine("j " + label1);
                            WriteLine(label2 + ":");

                            _continuePoints.Pop();
                            _breakPoints.Pop();
                        }
                        else if (isTrue)
                        {
                            var label1 = GenerateLabel("while_start");
                            var label2 = GenerateLabel("while_end");

                            _continuePoints.Push(label1);
                            _breakPoints.Push(label2);

                            WriteLine(label1 + ":");
                            GenerateMipsCode(cln.Statement, r);
                            WriteLine("j " + label1);
                            WriteLine(label2 + ":");

                            _continuePoints.Pop();
                            _breakPoints.Pop();
                        }

                        break;
                    }
                case LoopNode ln:
                    {
                        var label1 = GenerateLabel("loop_start");
                        var label2 = GenerateLabel("loop_end");

                        _continuePoints.Push(label1);
                        _breakPoints.Push(label2);

                        WriteLine(label1 + ":");
                        GenerateMipsCode(ln.Statement, r);
                        WriteLine("j " + label1);
                        WriteLine(label2 + ":");

                        _continuePoints.Pop();
                        _breakPoints.Pop();

                        break;
                    }
                case BreakNode bn:
                    {
                        var jp = _breakPoints.Peek();
                        WriteLine("j " + jp);
                        break;
                    }
                case ContinueNode cn:
                    {
                        var jp = _continuePoints.Peek();
                        WriteLine("j " + jp);
                        break;
                    }
                case BinaryOpNode bop:
                    {
                        int rt = r;

                        if (GetNumericValueOrRegister(bop.Left, rt, out string a1))
                        {
                            rt = ReserveRegister();
                        }

                        if (GetNumericValueOrRegister(bop.Right, rt, out string a2))
                        {
                            if (rt != r)
                            {
                                FreeRegister(rt);
                            }
                        }

                        switch (bop.Operator)
                        {
                            case ArithmeticOperatorType.OpMul:
                                WriteLine($"mul r{r} {a1} {a2}");
                                break;
                            case ArithmeticOperatorType.OpDiv:
                                WriteLine($"div r{r} {a1} {a2}");
                                break;
                            case ArithmeticOperatorType.OpAdd:
                                WriteLine($"add r{r} {a1} {a2}");
                                break;
                            case ArithmeticOperatorType.OpSub:
                                WriteLine($"sub r{r} {a1} {a2}");
                                break;
                            default:
                                throw new Exception($"Not supported Operator: {bop.Operator}.");
                        }

                        break;
                    }
                case ComparisonNode cop:
                    {
                        int rt = r;

                        if (GetNumericValueOrRegister(cop.Left, rt, out string a1))
                        {
                            rt = ReserveRegister();
                        }

                        if (GetNumericValueOrRegister(cop.Right, rt, out string a2))
                        {
                            if (rt != r)
                            {
                                FreeRegister(rt);
                            }
                        }

                        switch (cop.Operator)
                        {
                            case ComparisonOperatorType.OpEqual:
                                WriteLine($"seq r{r} {a1} {a2}");
                                break;
                            case ComparisonOperatorType.OpNotEqual:
                                WriteLine($"sne r{r} {a1} {a2}");
                                break;
                            case ComparisonOperatorType.OpLess:
                                WriteLine($"slt r{r} {a1} {a2}");
                                break;
                            case ComparisonOperatorType.OpLessOrEqual:
                                WriteLine($"sle r{r} {a1} {a2}");
                                break;
                            case ComparisonOperatorType.OpGreater:
                                WriteLine($"sgt r{r} {a1} {a2}");
                                break;
                            case ComparisonOperatorType.OpGreaterOrEqual:
                                WriteLine($"sge r{r} {a1} {a2}");
                                break;
                            default:
                                throw new Exception($"Not supported comparison operator: {cop.Operator}.");
                        }

                        break;
                    }
                case TernaryOpNode ton:
                    {
                        IsConstantExpressionAndNotZero(ton.Condition, out bool constant, out bool isTrue);

                        if (constant)
                        {
                            if (isTrue)
                            {
                                if (!GetNumericValueOrRegister(ton.Left, r, out string a1))
                                {
                                    WriteLine($"move r{r} {a1}");
                                }
                            }
                            else
                            {
                                if (!GetNumericValueOrRegister(ton.Right, r, out string a2))
                                {
                                    WriteLine($"move r{r} {a2}");
                                }
                            }
                        }
                        else
                        {
                            var rt = r;
                            int count = 0;
                            var reserved = new int[3];

                            if (GetNumericValueOrRegister(ton.Condition, rt, out string a1))
                            {
                                rt = ReserveRegister();
                                reserved[count++] = rt;
                            }

                            if (GetNumericValueOrRegister(ton.Left, rt, out string a2))
                            {
                                rt = ReserveRegister();
                                reserved[count++] = rt;
                            }

                            if (GetNumericValueOrRegister(ton.Right, rt, out string a3))
                            {
                                rt = ReserveRegister();
                                reserved[count++] = rt;
                            }

                            for (int i = 0; i < count; ++i)
                            {
                                FreeRegister(reserved[i]);
                            }

                            WriteLine($"select r{r} {a1} {a2} {a3}");
                        }

                        break;
                    }
                case LogicalNode ln:
                    {
                        int ra = ReserveRegister();
                        GetNumericValueOrRegister(ln.Left, ra, out string a1);

                        int rb = ReserveRegister();
                        GetNumericValueOrRegister(ln.Right, rb, out string a2);

                        FreeRegister(ra);
                        FreeRegister(rb);

                        switch (ln.Operator)
                        {
                            case LogicalOperatorType.OpAnd:
                                WriteLine($"sge r{ra} {a1} 1");
                                WriteLine($"sge r{rb} {a2} 1");
                                WriteLine($"and r{r} r{ra} r{rb}");
                                break;
                            case LogicalOperatorType.OpOr:
                                WriteLine($"sge r{ra} {a1} 1");
                                WriteLine($"sge r{rb} {a2} 1");
                                WriteLine($"or r{r} r{ra} r{rb}");
                                break;
                            default:
                                throw new Exception($"Not supported logical operator: {ln.Operator}.");
                        }

                        break;
                    }
                case NumericNode nn:
                    {
                        WriteLine($"move r{r} {nn.Value}");
                        break;
                    }
                case HashNode hashn:
                    {
                        WriteLine($"move r{r} {Utils.HashAsInt32(hashn.Value)}");
                        break;
                    }
                case ConstantNode constn:
                    {
                        WriteLine($"move r{r} {constn.Value}");
                        break;
                    }
                case IdentifierNode idn:
                    {
                        if (_devices.TryGetValue(idn.Identifier, out DeviceConfigNode dcn))
                        {
                            if (String.IsNullOrEmpty(idn.Property))
                            {
                                throw new Exception("Missing logicType or slotLogicType.");
                            }

                            if (idn.Index != null)
                            {
                                String indexValue = null;

                                if (idn.Index is NumericNode nn)
                                {
                                    indexValue = nn.Value;
                                }
                                else if (idn.Index is IdentifierNode iidn)
                                {
                                    var ri = GetVariable(iidn.Identifier);
                                    indexValue = "r" + ri;
                                }
                                else
                                {
                                    throw new Exception("Not supported index expression.");
                                }

                                if (dcn.Port != null)
                                {
                                    WriteLine($"ls r{r} {dcn.Port} {indexValue} {idn.Property}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    WriteLine($"lbns r{r} {Utils.HashAsInt32(dcn.Type)} {Utils.HashAsInt32(dcn.Name)} {indexValue} {idn.Property} {dcn.BatchMode}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    WriteLine($"lbs r{r} {Utils.HashAsInt32(dcn.Type)} {indexValue} {idn.Property} {dcn.BatchMode}");
                                }
                                else
                                {
                                    throw new Exception("Not supported Device Configuration.");
                                }
                            }
                            else
                            {
                                if (dcn.Port != null)
                                {
                                    WriteLine($"l r{r} {dcn.Port} {idn.Property}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    WriteLine($"lbn r{r} {Utils.HashAsInt32(dcn.Type)} {Utils.HashAsInt32(dcn.Name)} {idn.Property} {dcn.BatchMode}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    WriteLine($"lb r{r} {Utils.HashAsInt32(dcn.Type)} {idn.Property} {dcn.BatchMode}");
                                }
                                else
                                {
                                    throw new Exception("Not supported Device Configuration.");
                                }
                            }
                        }
                        else
                        {
                            var rv = GetVariable(idn.Identifier);
                            WriteLine($"move r{r} r{rv}");
                        }

                        break;
                    }
                case VariableDeclerationNode vdn:
                    {
                        if (_devices.ContainsKey(vdn.Identifier) || HasVariable(vdn.Identifier))
                        {
                            throw new Exception($"Variable already declared: {vdn.Identifier}.");
                        }

                        if (vdn.Expression is DeviceConfigNode dcn)
                        {
                            _devices[vdn.Identifier] = dcn;
                        }
                        else
                        {
                            int rv = RegisterVariable(vdn.Identifier);
                            GenerateMipsCode(vdn.Expression, rv);
                        }

                        break;
                    }
                case AssigmentNode an:
                    {
                        var identifier = an.Identifier;

                        if (_devices.TryGetValue(identifier.Identifier, out DeviceConfigNode dcn))
                        {
                            if (String.IsNullOrEmpty(identifier.Property))
                            {
                                throw new Exception("Missing logicType or slotLogicType.");
                            }

                            GetNumericValueOrRegister(an.Expression, r, out string a1);

                            if (identifier.Index != null)
                            {
                                String indexValue = null;

                                if (identifier.Index is NumericNode nn)
                                {
                                    indexValue = nn.Value;
                                }
                                else if (identifier.Index is IdentifierNode iidn)
                                {
                                    var ri = GetVariable(iidn.Identifier);
                                    indexValue = "r" + ri;
                                }
                                else
                                {
                                    throw new Exception("Not supported index expression.");
                                }

                                if (dcn.Port != null)
                                {
                                    WriteLine($"ss {dcn.Port} {indexValue} {identifier.Property} {a1}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    throw new Exception("Not supported operation.");
                                    // WriteLine($"sbns {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {indexValue} {an.Property} r0");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    WriteLine($"sbs {Utils.HashAsInt32(dcn.Type)} {indexValue} {identifier.Property} {a1}");
                                }
                                else
                                {
                                    throw new Exception("Not supported Device Configuration.");
                                }
                            }
                            else
                            {
                                if (dcn.Port != null)
                                {
                                    WriteLine($"s {dcn.Port} {identifier.Property} {a1}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    WriteLine($"sbn {Utils.HashAsInt32(dcn.Type)} {Utils.HashAsInt32(dcn.Name)} {identifier.Property} {a1}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    WriteLine($"sb {Utils.HashAsInt32(dcn.Type)} {identifier.Property} {a1}");
                                }
                                else
                                {
                                    throw new Exception("Not supported Device Configuration.");
                                }
                            }
                        }
                        else
                        {
                            var rv = GetVariable(identifier.Identifier);
                            if (!GetNumericValueOrRegister(an.Expression, rv, out string a1))
                            {
                                WriteLine($"move r{rv} {a1}");
                            }
                        }
                        break;
                    }
                case CallNode cn:
                    {
                        if (String.Compare(Keywords.SLEEP, cn.Identifier, StringComparison.Ordinal) == 0)
                        {
                            if (cn.Arguments == null || cn.Arguments.Count != 1)
                            {
                                throw new Exception("");
                            }

                            GetNumericValueOrRegister(cn.Arguments[0], r, out string a1);
                            WriteLine($"sleep {a1}");
                        }
                        else if (String.Compare(Keywords.YIELD, cn.Identifier, StringComparison.Ordinal) == 0)
                        {
                            if (cn.Arguments != null && cn.Arguments.Count != 0)
                            {
                                throw new Exception("");
                            }

                            WriteLine($"yield");
                        }
                        else if (String.Compare(Keywords.HCF, cn.Identifier, StringComparison.Ordinal) == 0)
                        {
                            if (cn.Arguments != null && cn.Arguments.Count != 0)
                            {
                                throw new Exception("");
                            }

                            WriteLine($"hcf");
                        }
                        else
                        {
                            List<int> reservedRegisters = new List<int>();
                            String callArguments = "";

                            if (cn.Arguments != null && cn.Arguments.Count > 0)
                            {
                                for (int a = 0; a < cn.Arguments.Count; ++a)
                                {
                                    int ra = ReserveRegister();
                                    if (GetNumericValueOrRegister(cn.Arguments[a], ra, out string a1))
                                    {
                                        reservedRegisters.Add(ra);
                                    }

                                    callArguments += " " + a1;
                                }
                            }

                            WriteLine($"{cn.Identifier} r{r}" + callArguments);

                            foreach (var p in reservedRegisters)
                            {
                                FreeRegister(p);
                            }
                        }

                        break;
                    }
                default:
                    throw new Exception($"Not supported Node: {node?.GetType()?.Name}");
            }
        }

        private void IsConstantExpressionAndNotZero(Node n, out bool constant, out bool isTrue)
        {
            if (Utils.IsValueNode(n))
            {
                constant = true;
                isTrue = Utils.IsNotZero(n);
                return;
            }

            constant = false;
            isTrue = false;
        }

        private void IsConstantExpressionAndTrue(Node n, out bool constant, out bool isTrue)
        {
            if (Utils.IsValueNode(n))
            {
                constant = true;
                isTrue = Utils.IsTrue(n);
                return;
            }

            constant = false;
            isTrue = false;
        }

        private void GenerateOpositeJump(Node n, int r, String label)
        {
            if (n is ComparisonNode cn)
            {
                int rt = r;

                if (GetNumericValueOrRegister(cn.Left, rt, out string a1))
                {
                    rt = ReserveRegister();
                }

                if (GetNumericValueOrRegister(cn.Right, rt, out string a2))
                {
                    if (rt != r)
                    {
                        FreeRegister(rt);
                    }
                }

                switch (cn.Operator)
                {
                    case ComparisonOperatorType.OpEqual:
                        WriteLine($"bne r{r} {a1} {a2} {label}");
                        break;
                    case ComparisonOperatorType.OpNotEqual:
                        WriteLine($"beq r{r} {a1} {a2} {label}");
                        break;
                    case ComparisonOperatorType.OpLess:
                        WriteLine($"bge r{r} {a1} {a2} {label}");
                        break;
                    case ComparisonOperatorType.OpLessOrEqual:
                        WriteLine($"bgt r{r} {a1} {a2} {label}");
                        break;
                    case ComparisonOperatorType.OpGreater:
                        WriteLine($"ble r{r} {a1} {a2} {label}");
                        break;
                    case ComparisonOperatorType.OpGreaterOrEqual:
                        WriteLine($"blt r{r} {a1} {a2} {label}");
                        break;
                    default:
                        throw new Exception($"Not supported comparison operator: {cn.Operator}.");
                }
            }
            else if (n is LogicalNode ln)
            {
                int rt = r;

                if (GetNumericValueOrRegister(ln.Left, rt, out string a1))
                {
                    rt = ReserveRegister();
                }

                if (GetNumericValueOrRegister(ln.Right, rt, out string a2))
                {
                    if (rt != r)
                    {
                        FreeRegister(rt);
                    }
                }

                switch (ln.Operator)
                {
                    case LogicalOperatorType.OpAnd:
                        WriteLine($"blt {a1} 1 {label}");
                        WriteLine($"blt {a2} 1 {label}");
                        break;
                    case LogicalOperatorType.OpOr:
                        WriteLine($"brge {a1} 1 3");
                        WriteLine($"brge {a2} 1 2");
                        WriteLine($"j {label}");
                        break;
                    default:
                        throw new Exception($"Not supported logical operator: {ln.Operator}.");
                }
            }
            else
            {
                GetNumericValueOrRegister(n, r, out string a1);
                WriteLine($"blt {a1} 1 {label}");
            }
        }

        private bool GetNumericValueOrRegister(Node n, int r, out String result)
        {
            // constant expression
            if (n is NumericNode nn)
            {
                result = nn.Value;
                return false; 
            }
            else if (n is HashNode hashn)
            {
                result = Utils.HashAsInt32(hashn.Value).ToString();
                return false;
            }
            else if (n is ConstantNode constn)
            {
                result = constn.Value;
                return false;
            }
            // access directly to variable
            else if (n is IdentifierNode idn && !_devices.TryGetValue(idn.Identifier, out DeviceConfigNode dcn))
            {
                int rv = GetVariable(idn.Identifier);
                result = "r" + rv;
                return false;
            }

            GenerateMipsCode(n, r);
            result = "r" + r;
            return true;
        }

        private String GenerateLabel(String prefix)
        {
            _labelCounter.TryGetValue(prefix, out int counter);
            ++counter;
            _labelCounter[prefix] = counter;

            return $"{prefix}{counter:000}";
        }

        private int RegisterVariable(string variable)
        {
            for (int i = MAX_REGISTER; i >= MIN_REGISTER; --i)
            {
                if (_registers[i]) continue;
                _registers[i] = true;

                _variables[variable] = i;
                return i;
            }

            throw new Exception("No more free register.");
        }

        private int GetVariable(string variable)
        {
            if (_variables.TryGetValue(variable, out int i))
            {
                return i;
            }

            throw new Exception($"Variable not declared: {variable}.");
        }

        private bool HasVariable(string variable)
        {
            return _variables.ContainsKey(variable);
        }

        private int ReserveRegister()
        {
            for (int i = MIN_REGISTER; i < MAX_REGISTER; ++i)
            {
                if (_registers[i]) continue;
                _registers[i] = true;
                return i;
            }

            throw new Exception("No more free register.");
        }

        private void FreeRegister(int r)
        {
            if (_registers[r])
            {
                _registers[r] = false;
            }
            else
            {
                throw new Exception($"Register already freed ({r}).");
            }
        }

        private void WriteLine(String line)
        {
            _output.AppendLine(line);
        }
    }
}
