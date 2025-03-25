using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Hashing;
using System.Reflection.Emit;
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
        Dictionary<String, int> _variables = new Dictionary<string, int>();
        Node _programNode;

        public OutputGenerator(Node programNode)
        {
            _labelCounter = new Dictionary<string, int>();
            _breakPoints = new Stack<string>();
            _continuePoints = new Stack<string>();
            _devices = new Dictionary<string, DeviceConfigNode>();
            _programNode = programNode;
        }

        public void Print()
        {
            int r = ReserveRegister();
            GenerateMipsCode(_programNode, r);
            FreeRegister(r);
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
                                Console.WriteLine("j " + label2);
                                Console.WriteLine(label1 + ":");
                                GenerateMipsCode(csn.Alternate, r);
                                Console.WriteLine(label2 + ":");
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
                                Console.WriteLine(label + ":");
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

                            Console.WriteLine(label1 + ":");
                            GenerateOpositeJump(cln.Condition, r, label2);
                            GenerateMipsCode(cln.Statement, r);
                            Console.WriteLine("j " + label1);
                            Console.WriteLine(label2 + ":");

                            _continuePoints.Pop();
                            _breakPoints.Pop();
                        }
                        else if (isTrue)
                        {
                            var label1 = GenerateLabel("while_start");
                            var label2 = GenerateLabel("while_end");

                            _continuePoints.Push(label1);
                            _breakPoints.Push(label2);

                            Console.WriteLine(label1 + ":");
                            GenerateMipsCode(cln.Statement, r);
                            Console.WriteLine("j " + label1);
                            Console.WriteLine(label2 + ":");

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

                        Console.WriteLine(label1 + ":");
                        GenerateMipsCode(ln.Statement, r);
                        Console.WriteLine("j " + label1);
                        Console.WriteLine(label2 + ":");

                        _continuePoints.Pop();
                        _breakPoints.Pop();

                        break;
                    }
                case BreakNode bn:
                    {
                        var jp = _breakPoints.Peek();
                        Console.WriteLine("j " + jp);
                        break;
                    }
                case ContinueNode cn:
                    {
                        var jp = _continuePoints.Peek();
                        Console.WriteLine("j " + jp);
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
                            FreeRegister(rt);
                        }

                        switch (bop.Operator)
                        {
                            case OperatorType.OpMul:
                                Console.WriteLine($"mul r{r} {a1} {a2}");
                                break;
                            case OperatorType.OpDiv:
                                Console.WriteLine($"div r{r} {a1} {a2}");
                                break;
                            case OperatorType.OpAdd:
                                Console.WriteLine($"add r{r} {a1} {a2}");
                                break;
                            case OperatorType.OpSub:
                                Console.WriteLine($"sub r{r} {a1} {a2}");
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
                            FreeRegister(rt);
                        }

                        switch (cop.Operator)
                        {
                            case ComparsionOperatorType.OpEqual:
                                Console.WriteLine($"seq r{r} {a1} {a2}");
                                break;
                            case ComparsionOperatorType.OpNotEqual:
                                Console.WriteLine($"sne r{r} {a1} {a2}");
                                break;
                            case ComparsionOperatorType.OpLess:
                                Console.WriteLine($"slt r{r} {a1} {a2}");
                                break;
                            case ComparsionOperatorType.OpLessOrEqual:
                                Console.WriteLine($"sle r{r} {a1} {a2}");
                                break;
                            case ComparsionOperatorType.OpGreater:
                                Console.WriteLine($"sgt r{r} {a1} {a2}");
                                break;
                            case ComparsionOperatorType.OpGreaterOrEqual:
                                Console.WriteLine($"sge r{r} {a1} {a2}");
                                break;
                            default:
                                throw new Exception($"Not supported comparsion operator: {cop.Operator}.");
                        }

                        break;
                    }
                case NumericNode nn:
                    {
                        Console.WriteLine($"move r{r} {nn.Value}");
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
                                    Console.WriteLine($"ls r{r} {dcn.Port} {indexValue} {idn.Property}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lbns r{r} {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {indexValue} {idn.Property} {dcn.BatchMode}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lbs r{r} {GenerateHashValue(dcn.Type)} {indexValue} {idn.Property} {dcn.BatchMode}");
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
                                    Console.WriteLine($"l r{r} {dcn.Port} {idn.Property}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lbn r{r} {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {idn.Property} {dcn.BatchMode}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lb r{r} {GenerateHashValue(dcn.Type)} {idn.Property} {dcn.BatchMode}");
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
                            Console.WriteLine($"move r{r} r{rv}");
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
                                    Console.WriteLine($"ss {dcn.Port} {indexValue} {identifier.Property} {a1}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    throw new Exception("Not supported operation.");
                                    // Console.WriteLine($"sbns {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {indexValue} {an.Property} r0");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"sbs {GenerateHashValue(dcn.Type)} {indexValue} {identifier.Property} {a1}");
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
                                    Console.WriteLine($"s {dcn.Port} {identifier.Property} {a1}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"sbn {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {identifier.Property} {a1}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"sb {GenerateHashValue(dcn.Type)} {identifier.Property} {a1}");
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
                                Console.WriteLine($"move r{rv} {a1}");
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
                            Console.WriteLine($"sleep {a1}");
                        }
                        else if (String.Compare(Keywords.YIELD, cn.Identifier, StringComparison.Ordinal) == 0)
                        {
                            if (cn.Arguments != null && cn.Arguments.Count != 0)
                            {
                                throw new Exception("");
                            }

                            Console.WriteLine($"yield");
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

                            Console.WriteLine($"{cn.Identifier} r{r}" + callArguments);

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

        private void IsConstantExpressionAndTrue(Node n, out bool constant, out bool isTrue)
        {
            if (n is NumericNode nn)
            {
                constant = true;
                isTrue = IsTrue(nn.Value);
                return;
            }

            constant = false;
            isTrue = false;
        }

        private bool IsTrue(String numericValue)
        {
            return Double.Parse(numericValue, CultureInfo.InvariantCulture) >= 1.0;
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
                    FreeRegister(rt);
                }

                switch (cn.Operator)
                {
                    case ComparsionOperatorType.OpEqual:
                        Console.WriteLine($"bne r{r} {a1} {a2} {label}");
                        break;
                    case ComparsionOperatorType.OpNotEqual:
                        Console.WriteLine($"beq r{r} {a1} {a2} {label}");
                        break;
                    case ComparsionOperatorType.OpLess:
                        Console.WriteLine($"bge r{r} {a1} {a2} {label}");
                        break;
                    case ComparsionOperatorType.OpLessOrEqual:
                        Console.WriteLine($"bgt r{r} {a1} {a2} {label}");
                        break;
                    case ComparsionOperatorType.OpGreater:
                        Console.WriteLine($"ble r{r} {a1} {a2} {label}");
                        break;
                    case ComparsionOperatorType.OpGreaterOrEqual:
                        Console.WriteLine($"blt r{r} {a1} {a2} {label}");
                        break;
                    default:
                        throw new Exception($"Not supported comparsion operator: {cn.Operator}.");
                }
            }
            else
            {
                GetNumericValueOrRegister(n, r, out string a1);
                Console.WriteLine($"blt {a1} 1 {label}");
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

        private Int32 GenerateHashValue(string value)
        {
            var hash = BitConverter.ToInt32(Crc32.Hash(Encoding.ASCII.GetBytes(value)), 0);
            return hash;
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
    }
}
