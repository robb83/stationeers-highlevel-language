using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stationeers.Compiler.Program;

namespace Stationeers.Compiler
{
    public class OutputGenerator
    {
        const int MIN_REGISTER = 2;
        const int MAX_REGISTER = 15;
        const int STACK_SIZE = 512;

        Dictionary<String, int> labelCounter;
        Stack<String> breakPoints;
        Stack<String> continuePoints;
        Dictionary<String, DeviceConfigNode> devices;
        bool[] registers = new bool[18];
        Dictionary<String, int> variables = new Dictionary<string, int>();
        Node programNode;

        public OutputGenerator(Node programNode)
        {
            this.labelCounter = new Dictionary<string, int>();
            this.breakPoints = new Stack<string>();
            this.continuePoints = new Stack<string>();
            this.devices = new Dictionary<string, DeviceConfigNode>();
            this.programNode = programNode;
        }

        public void Print()
        {
            GenerateMipsCode(programNode);
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
                case WhileStatementNode wsn:
                    {
                        var label1 = GenerateLabel("while_start");
                        var label2 = GenerateLabel("while_end");

                        continuePoints.Push(label1);
                        breakPoints.Push(label2);

                        Console.WriteLine(label1 + ":");
                        GenerateMipsCode(wsn.Condition);
                        Console.WriteLine("blt r0 1 " + label2);
                        GenerateMipsCode(wsn.Statement);
                        Console.WriteLine("j " + label1);
                        Console.WriteLine(label2 + ":");

                        continuePoints.Pop();
                        breakPoints.Pop();

                        break;
                    }
                case BreakNode bn:
                    {
                        var jp = breakPoints.Peek();
                        Console.WriteLine("j " + jp);
                        break;
                    }
                case ContinueNode cn:
                    {
                        var jp = continuePoints.Peek();
                        Console.WriteLine("j " + jp);
                        break;
                    }
                case BinaryOpNode bop:
                    {
                        GenerateMipsCode(bop.Left);
                        Console.WriteLine("move r1 r0");
                        GenerateMipsCode(bop.Right);
                        switch (bop.Operator)
                        {
                            case OperatorType.OpMul:
                                Console.WriteLine("mul r0 r1 r0");
                                break;
                            case OperatorType.OpDiv:
                                Console.WriteLine("div r0 r1 r0");
                                break;
                            case OperatorType.OpAdd:
                                Console.WriteLine("add r0 r1 r0");
                                break;
                            case OperatorType.OpSub:
                                Console.WriteLine("sub r0 r1 r0");
                                break;
                            default:
                                throw new Exception($"Not supported Operator: {bop.Operator}.");
                        }
                        break;
                    }
                case ComparisonNode cop:
                    {
                        GenerateMipsCode(cop.Left);
                        Console.WriteLine("move r1 r0");
                        GenerateMipsCode(cop.Right);
                        switch (cop.Operator)
                        {
                            case ComparsionOperatorType.OpEqual:
                                Console.WriteLine("seq r0 r1 r0");
                                break;
                            case ComparsionOperatorType.OpNotEqual:
                                Console.WriteLine("sne r0 r1 r0");
                                break;
                            case ComparsionOperatorType.OpLess:
                                Console.WriteLine("slt r0 r1 r0");
                                break;
                            case ComparsionOperatorType.OpLessOrEqual:
                                Console.WriteLine("sle r0 r1 r0");
                                break;
                            case ComparsionOperatorType.OpGreater:
                                Console.WriteLine("sgt r0 r1 r0");
                                break;
                            case ComparsionOperatorType.OpGreaterOrEqual:
                                Console.WriteLine("sge r0 r1 r0");
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
                        if (devices.TryGetValue(idn.Identifier, out DeviceConfigNode dcn))
                        {
                            if (String.IsNullOrEmpty(idn.Property))
                            {
                                throw new Exception("Missing logicType.");
                            }

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
                        else
                        {
                            var r = GetVariable(idn.Identifier);
                            Console.WriteLine($"move r0 r{r}");
                        }

                        break;
                    }
                case VariableDeclerationNode vdn:
                    {
                        if (devices.ContainsKey(vdn.Identifier) || HasVariable(vdn.Identifier))
                        {
                            throw new Exception($"Variable already declared: {vdn.Identifier}.");
                        }

                        if (vdn.Expression is DeviceConfigNode dcn)
                        {
                            devices[vdn.Identifier] = dcn;
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
                        if (devices.TryGetValue(an.Identifier, out DeviceConfigNode dcn))
                        {
                            if (String.IsNullOrEmpty(an.Property))
                            {
                                throw new Exception("Missing logicType.");
                            }

                            GenerateMipsCode(an.Expression);

                            if (dcn.Port != null)
                            {
                                Console.WriteLine($"s {dcn.Port} {an.Property} r0");
                            }
                            else if (dcn.Name != null && dcn.BatchMode != null)
                            {
                                Console.WriteLine($"sbn {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {an.Property} r0");
                            }
                            else if (dcn.BatchMode != null)
                            {
                                Console.WriteLine($"sb {GenerateHashValue(dcn.Type)} {an.Property} r0");
                            }
                            else
                            {
                                throw new Exception("Not supported Device Configuration.");
                            }
                        }
                        else
                        {
                            GenerateMipsCode(an.Expression);
                            var r = GetVariable(an.Identifier);
                            Console.WriteLine($"move r{r} r0");
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
            labelCounter.TryGetValue(prefix, out int counter);
            ++counter;
            labelCounter[prefix] = counter;

            return $"{prefix}{counter:000}";
        }

        private int RegisterVariable(string variable)
        {
            for (int i = MAX_REGISTER; i >= MIN_REGISTER; --i)
            {
                if (registers[i]) continue;
                registers[i] = true;

                variables[variable] = i;
                return i;
            }

            throw new Exception("No more free register.");
        }

        private int GetVariable(string variable)
        {
            if (variables.TryGetValue(variable, out int i))
            {
                return i;
            }

            throw new Exception($"Variable not declared: {variable}.");
        }

        private bool HasVariable(string variable)
        {
            return variables.ContainsKey(variable);
        }

        private int ReserveRegister()
        {
            for (int i = MIN_REGISTER; i < MAX_REGISTER; ++i)
            {
                if (registers[i]) continue;
                registers[i] = true;
                return i;
            }

            throw new Exception("No more free register.");
        }

        private void FreeRegister(int r)
        {
            if (registers[r])
            {
                registers[r] = false;
            }
            else
            {
                throw new Exception($"Register already freed ({r}).");
            }
        }
    }
}
