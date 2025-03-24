using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Text;
using Stationeers.Compiler.AST;

namespace Stationeers.Compiler
{
    public class OutputGenerator
    {
        const int MIN_REGISTER = 1;
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
            GenerateMipsCode(_programNode);
        }

        private void GenerateMipsCode(Node node)
        {
            switch (node)
            {
                case ProgramNode program:
                    if (program.Statements != null)
                    {
                        foreach (var s in program.Statements)
                        {
                            GenerateMipsCode(s);
                        }
                    }
                    break;
                case BlockNode block:
                    if (block.Statements != null)
                    {
                        foreach (var s in block.Statements)
                        {
                            GenerateMipsCode(s);
                        }
                    }
                    break;
                case ConditionalStatementNode csn:
                    {
                        if (csn.Alternate != null)
                        {
                            var label1 = GenerateLabel("if_else");
                            var label2 = GenerateLabel("if_end");

                            GenerateMipsCode(csn.Condition);
                            Console.WriteLine("blt r0 1 " + label1);
                            GenerateMipsCode(csn.Statement);
                            Console.WriteLine("j " + label2);
                            Console.WriteLine(label1 + ":");
                            GenerateMipsCode(csn.Alternate);
                            Console.WriteLine(label2 + ":");
                        }
                        else
                        {
                            var label = GenerateLabel("if");
                            GenerateMipsCode(csn.Condition);
                            Console.WriteLine("blt r0 1 " + label);
                            GenerateMipsCode(csn.Statement);
                            Console.WriteLine(label + ":");
                        }

                        break;
                    }
                case ConditionalLoopNode cln:
                    {
                        var label1 = GenerateLabel("while_start");
                        var label2 = GenerateLabel("while_end");

                        _continuePoints.Push(label1);
                        _breakPoints.Push(label2);

                        Console.WriteLine(label1 + ":");
                        GenerateMipsCode(cln.Condition);
                        Console.WriteLine("blt r0 1 " + label2);
                        GenerateMipsCode(cln.Statement);
                        Console.WriteLine("j " + label1);
                        Console.WriteLine(label2 + ":");

                        _continuePoints.Pop();
                        _breakPoints.Pop();

                        break;
                    }
                case LoopNode ln:
                    {
                        var label1 = GenerateLabel("loop_start");
                        var label2 = GenerateLabel("loop_end");

                        _continuePoints.Push(label1);
                        _breakPoints.Push(label2);

                        Console.WriteLine(label1 + ":");
                        GenerateMipsCode(ln.Statement);
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
                        int r = ReserveRegister();

                        GenerateMipsCode(bop.Left);
                        Console.WriteLine($"move r{r} r0");
                        GenerateMipsCode(bop.Right);

                        switch (bop.Operator)
                        {
                            case OperatorType.OpMul:
                                Console.WriteLine($"mul r0 r{r} r0");
                                break;
                            case OperatorType.OpDiv:
                                Console.WriteLine($"div r0 r{r} r0");
                                break;
                            case OperatorType.OpAdd:
                                Console.WriteLine($"add r0 r{r} r0");
                                break;
                            case OperatorType.OpSub:
                                Console.WriteLine($"sub r0 r{r} r0");
                                break;
                            default:
                                throw new Exception($"Not supported Operator: {bop.Operator}.");
                        }
                        break;
                    }
                case ComparisonNode cop:
                    {
                        int r = ReserveRegister();

                        GenerateMipsCode(cop.Left);
                        Console.WriteLine($"move r{r} r0");
                        GenerateMipsCode(cop.Right);
                        switch (cop.Operator)
                        {
                            case ComparsionOperatorType.OpEqual:
                                Console.WriteLine($"seq r0 r{r} r0");
                                break;
                            case ComparsionOperatorType.OpNotEqual:
                                Console.WriteLine($"sne r0 r{r} r0");
                                break;
                            case ComparsionOperatorType.OpLess:
                                Console.WriteLine($"slt r0 r{r} r0");
                                break;
                            case ComparsionOperatorType.OpLessOrEqual:
                                Console.WriteLine($"sle r0 r{r} r0");
                                break;
                            case ComparsionOperatorType.OpGreater:
                                Console.WriteLine($"sgt r0 r{r} r0");
                                break;
                            case ComparsionOperatorType.OpGreaterOrEqual:
                                Console.WriteLine($"sge r0 r{r} r0");
                                break;
                            default:
                                throw new Exception($"Not supported comparsion operator: {cop.Operator}.");
                        }
                        break;
                    }
                case NumericNode nn:
                    {
                        Console.WriteLine($"move r0 {nn.Value}");
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
                                    var r = GetVariable(iidn.Identifier);
                                    indexValue = "r" + r;
                                }
                                else
                                {
                                    throw new Exception("Not supported index expression.");
                                }

                                if (dcn.Port != null)
                                {
                                    Console.WriteLine($"ls r0 {dcn.Port} {indexValue} {idn.Property}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lbns r0 {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {indexValue} {idn.Property} {dcn.BatchMode}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lbs r0 {GenerateHashValue(dcn.Type)} {indexValue} {idn.Property} {dcn.BatchMode}");
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
                                    Console.WriteLine($"l r0 {dcn.Port} {idn.Property}");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lbn r0 {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {idn.Property} {dcn.BatchMode}");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"lb r0 {GenerateHashValue(dcn.Type)} {idn.Property} {dcn.BatchMode}");
                                }
                                else
                                {
                                    throw new Exception("Not supported Device Configuration.");
                                }
                            }
                        }
                        else
                        {
                            var r = GetVariable(idn.Identifier);
                            Console.WriteLine($"move r0 r{r}");
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
                            GenerateMipsCode(vdn.Expression);
                            var r = RegisterVariable(vdn.Identifier);
                            Console.WriteLine($"move r{r} r0");
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

                            GenerateMipsCode(an.Expression);

                            if (identifier.Index != null)
                            {
                                String indexValue = null;

                                if (identifier.Index is NumericNode nn)
                                {
                                    indexValue = nn.Value;
                                }
                                else if (identifier.Index is IdentifierNode iidn)
                                {
                                    var r = GetVariable(iidn.Identifier);
                                    indexValue = "r" + r;
                                }
                                else
                                {
                                    throw new Exception("Not supported index expression.");
                                }

                                if (dcn.Port != null)
                                {
                                    Console.WriteLine($"ss {dcn.Port} {indexValue} {identifier.Property} r0");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    throw new Exception("Not supported operation.");
                                    // Console.WriteLine($"sbns {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {indexValue} {an.Property} r0");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"sbs {GenerateHashValue(dcn.Type)} {indexValue} {identifier.Property} r0");
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
                                    Console.WriteLine($"s {dcn.Port} {identifier.Property} r0");
                                }
                                else if (dcn.Name != null && dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"sbn {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {identifier.Property} r0");
                                }
                                else if (dcn.BatchMode != null)
                                {
                                    Console.WriteLine($"sb {GenerateHashValue(dcn.Type)} {identifier.Property} r0");
                                }
                                else
                                {
                                    throw new Exception("Not supported Device Configuration.");
                                }
                            }
                        }
                        else
                        {
                            GenerateMipsCode(an.Expression);
                            var r = GetVariable(identifier.Identifier);
                            Console.WriteLine($"move r{r} r0");
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

                            GenerateMipsCode(cn.Arguments[0]);
                            Console.WriteLine($"sleep r0");
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
                                callArguments += " r0";
                                for (int a = 0; a < cn.Arguments.Count; ++a)
                                {
                                    GenerateMipsCode(cn.Arguments[a]);

                                    if (a + 1 < cn.Arguments.Count)
                                    {
                                        int r = ReserveRegister();
                                        reservedRegisters.Add(r);
                                        callArguments += " r" + r;

                                        Console.WriteLine($"move r{r} r0");
                                    }
                                }
                            }

                            Console.WriteLine($"{cn.Identifier} r0" + callArguments);

                            foreach (var p in reservedRegisters)
                            {
                                FreeRegister(p);
                            }
                        }

                        break;
                    }
            }
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
