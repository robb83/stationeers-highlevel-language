import 'ast.dart';
import 'keywords.dart';
import 'utils.dart';

class Generator {
  static const MIN_REGISTER = 0;
  static const MAX_REGISTER = 15;
  static const STACK_SIZE = 512;

  Set<int> _reservedRegisters = {};
  Map<String, int> _variables = {};
  Map<String, DeviceConfigNode> _devices = {};
  Map<String, int> _labelCounter = {};
  StringBuffer _output = StringBuffer();
  List<String> _continuePoints = [];
  List<String> _breakPoints = [];
  ProgramNode _program;

  Generator(this._program);

  StringBuffer generate() {
    _output.clear();
    int r = reserveRegister();
    generateMipsCode(_program, r);
    freeRegister(r);
    return _output;
  }

  void generateMipsCode(Node node, int r) {
    switch (node) {
      case ProgramNode program:
        for (var s in program.statements) {
          generateMipsCode(s, r);
        }
        break;
      case BlockNode block:
        for (var s in block.statements) {
          generateMipsCode(s, r);
        }
        break;
      case ConditionalStatementNode csn:
        {
          if (csn.alternate != null) {
            var (constant, isTrue) = isConstantExpressionAndTrue(csn.condition);
            if (!constant) {
              var label1 = generateLabel("if_else");
              var label2 = generateLabel("if_end");

              generateOpositeJump(csn.condition, r, label1);
              generateMipsCode(csn.statement, r);
              writeLine("j " + label2);
              writeLine(label1 + ":");
              generateMipsCode(csn.alternate!, r);
              writeLine(label2 + ":");
            } else if (isTrue) {
              generateMipsCode(csn.statement, r);
            } else {
              generateMipsCode(csn.alternate!, r);
            }
          } else {
            var (constant, isTrue) = isConstantExpressionAndTrue(csn.condition);
            if (!constant) {
              var label = generateLabel("if");
              generateOpositeJump(csn.condition, r, label);
              generateMipsCode(csn.statement, r);
              writeLine(label + ":");
            } else if (isTrue) {
              generateMipsCode(csn.statement, r);
            }
          }

          break;
        }
      case ConditionalLoopNode cln:
        {
          var (constant, isTrue) = isConstantExpressionAndTrue(cln.condition);

          if (!constant) {
            var label1 = generateLabel("while_start");
            var label2 = generateLabel("while_end");

            _continuePoints.add(label1);
            _breakPoints.add(label2);

            writeLine(label1 + ":");
            generateOpositeJump(cln.condition, r, label2);
            generateMipsCode(cln.statement, r);
            writeLine("j " + label1);
            writeLine(label2 + ":");

            _continuePoints.removeLast();
            _breakPoints.removeLast();
          } else if (isTrue) {
            var label1 = generateLabel("while_start");
            var label2 = generateLabel("while_end");

            _continuePoints.add(label1);
            _breakPoints.add(label2);

            writeLine(label1 + ":");
            generateMipsCode(cln.statement, r);
            writeLine("j " + label1);
            writeLine(label2 + ":");

            _continuePoints.removeLast();
            _breakPoints.removeLast();
          }

          break;
        }
      case LoopNode ln:
        {
          var label1 = generateLabel("loop_start");
          var label2 = generateLabel("loop_end");

          _continuePoints.add(label1);
          _breakPoints.add(label2);

          writeLine(label1 + ":");
          generateMipsCode(ln.statement, r);
          writeLine("j " + label1);
          writeLine(label2 + ":");

          _continuePoints.removeLast();
          _breakPoints.removeLast();

          break;
        }
      case BreakNode bn:
        {
          var jp = _breakPoints.last;
          writeLine("j " + jp);
          break;
        }
      case ContinueNode cn:
        {
          var jp = _continuePoints.last;
          writeLine("j " + jp);
          break;
        }
      case BinaryOpNode bop:
        {
          int rt = r;
          String a1, a2;
          bool rt_used;

          (rt_used, a1) = getNumericValueOrRegister(bop.left, rt);
          if (rt_used) {
            rt = reserveRegister();
          }

          (rt_used, a2) = getNumericValueOrRegister(bop.right, rt);
          if (rt_used && rt != r) {
            freeRegister(rt);
          }

          switch (bop.operator) {
            case ArithmeticOperatorType.OpMul:
              writeLine("mul r${r} ${a1} ${a2}");
              break;
            case ArithmeticOperatorType.OpDiv:
              writeLine("div r${r} ${a1} ${a2}");
              break;
            case ArithmeticOperatorType.OpAdd:
              writeLine("add r${r} ${a1} ${a2}");
              break;
            case ArithmeticOperatorType.OpSub:
              writeLine("sub r${r} ${a1} ${a2}");
              break;
          }

          break;
        }
      case ComparisonNode cop:
        {
          int rt = r;
          String a1, a2;
          bool rt_used;

          (rt_used, a1) = getNumericValueOrRegister(cop.left, rt);
          if (rt_used) {
            rt = reserveRegister();
          }

          (rt_used, a2) = getNumericValueOrRegister(cop.right, rt);
          if (rt_used && rt != r) {
            freeRegister(rt);
          }

          switch (cop.operator) {
            case ComparisonOperatorType.OpEqual:
              writeLine("seq r${r} ${a1} ${a2}");
              break;
            case ComparisonOperatorType.OpNotEqual:
              writeLine("sne r${r} ${a1} ${a2}");
              break;
            case ComparisonOperatorType.OpLess:
              writeLine("slt r${r} ${a1} ${a2}");
              break;
            case ComparisonOperatorType.OpLessOrEqual:
              writeLine("sle r${r} ${a1} ${a2}");
              break;
            case ComparisonOperatorType.OpGreater:
              writeLine("sgt r${r} ${a1} ${a2}");
              break;
            case ComparisonOperatorType.OpGreaterOrEqual:
              writeLine("sge r${r} ${a1} ${a2}");
              break;
          }

          break;
        }
      case TernaryOpNode ton:
        {
          var (constant, isTrue) = isConstantExpressionAndNotZero(
            ton.condition,
          );
          if (constant) {
            if (isTrue) {
              var (r_used, a1) = getNumericValueOrRegister(ton.left, r);
              if (!r_used) {
                writeLine("move r${r} ${a1}");
              }
            } else {
              var (r_used, a2) = getNumericValueOrRegister(ton.right, r);
              if (!r_used) {
                writeLine("move r${r} ${a2}");
              }
            }
          } else {
            var rt = r;
            bool rt_used;
            String a1, a2, a3;
            var reserved = [];

            (rt_used, a1) = getNumericValueOrRegister(ton.condition, rt);
            if (rt_used) {
              rt = reserveRegister();
              reserved.add(rt);
            }

            (rt_used, a2) = getNumericValueOrRegister(ton.left, rt);
            if (rt_used) {
              rt = reserveRegister();
              reserved.add(rt);
            }

            (rt_used, a3) = getNumericValueOrRegister(ton.right, rt);
            if (rt_used) {
              rt = reserveRegister();
              reserved.add(rt);
            }

            for (int i = 0; i < reserved.length; ++i) {
              freeRegister(reserved[i]);
            }

            writeLine("select r${r} ${a1} ${a2} ${a3}");
          }

          break;
        }
      case LogicalNode ln:
        {
          int ra = reserveRegister();
          var (_, a1) = getNumericValueOrRegister(ln.left, ra);

          int rb = reserveRegister();
          var (_, a2) = getNumericValueOrRegister(ln.right, rb);

          freeRegister(ra);
          freeRegister(rb);

          switch (ln.operator) {
            case LogicalOperatorType.OpAnd:
              writeLine("sge r${ra} ${a1} 1");
              writeLine("sge r${rb} ${a2} 1");
              writeLine("and r${r} r${ra} r${rb}");
              break;
            case LogicalOperatorType.OpOr:
              writeLine("sge r${ra} ${a1} 1");
              writeLine("sge r${rb} ${a2} 1");
              writeLine("or r${r} r${ra} r${rb}");
              break;
          }

          break;
        }
      case NumericNode nn:
        {
          writeLine("move r${r} ${nn.value}");
          break;
        }
      case HashNode hashn:
        {
          writeLine("move r${r} ${Utils.hashAsInt32(hashn.value)}");
          break;
        }
      case ConstantNode constn:
        {
          writeLine("move r${r} ${constn.value}");
          break;
        }
      case IdentifierNode idn:
        {
          var dcn = _devices[idn.identifier];
          if (dcn != null) {
            if (idn.property == null || idn.property!.isEmpty) {
              throw new Exception("Missing logicType or slotLogicType.");
            }

            if (idn.index != null) {
              String indexValue;

              if (idn.index is NumericNode) {
                indexValue = (idn.index as NumericNode).value;
              } else if (idn.index is IdentifierNode) {
                var ri = getVariable((idn.index as IdentifierNode).identifier);
                indexValue = "r${ri}";
              } else {
                throw new Exception("Not supported index expression.");
              }

              if (dcn.port != null) {
                writeLine("ls r${r} ${dcn.port} ${indexValue} ${idn.property}");
              } else if (dcn.name != null && dcn.batchMode != null) {
                writeLine(
                  "lbns r${r} ${Utils.hashAsInt32(dcn.type)} ${Utils.hashAsInt32(dcn.name!)} ${indexValue} ${idn.property} ${dcn.batchMode}",
                );
              } else if (dcn.batchMode != null) {
                writeLine(
                  "lbs r${r} ${Utils.hashAsInt32(dcn.type)} ${indexValue} ${idn.property} ${dcn.batchMode}",
                );
              } else {
                throw new Exception("Not supported Device Configuration.");
              }
            } else {
              if (dcn.port != null) {
                writeLine("l r${r} ${dcn.port} ${idn.property}");
              } else if (dcn.name != null && dcn.batchMode != null) {
                writeLine(
                  "lbn r${r} ${Utils.hashAsInt32(dcn.type)} ${Utils.hashAsInt32(dcn.name!)} ${idn.property} ${dcn.batchMode}",
                );
              } else if (dcn.batchMode != null) {
                writeLine(
                  "lb r${r} ${Utils.hashAsInt32(dcn.type)} ${idn.property} ${dcn.batchMode}",
                );
              } else {
                throw new Exception("Not supported Device Configuration.");
              }
            }
          } else {
            var rv = getVariable(idn.identifier);
            writeLine("move r${r} r${rv}");
          }

          break;
        }
      case VariableDeclerationNode vdn:
        {
          if (_devices.containsKey(vdn.identifier) ||
              hasVariable(vdn.identifier)) {
            throw new Exception(
              "Variable already declared: ${vdn.identifier}.",
            );
          }

          if (vdn.expression is DeviceConfigNode) {
            _devices[vdn.identifier] = vdn.expression as DeviceConfigNode;
          } else {
            int rv = registerVariable(vdn.identifier);
            generateMipsCode(vdn.expression, rv);
          }

          break;
        }
      case AssigmentNode an:
        {
          var identifier = an.identifier;
          var dcn = _devices[identifier.identifier];

          if (dcn != null) {
            if (identifier.property == null || identifier.property!.isEmpty) {
              throw new Exception("Missing logicType or slotLogicType.");
            }

            var (r_used, a1) = getNumericValueOrRegister(an.expression, r);

            if (identifier.index != null) {
              String indexValue;

              if (identifier.index is NumericNode) {
                indexValue = (identifier.index as NumericNode).value;
              } else if (identifier.index is IdentifierNode) {
                var ri = getVariable(
                  (identifier.index as IdentifierNode).identifier,
                );
                indexValue = "r${ri}";
              } else {
                throw new Exception("Not supported index expression.");
              }

              if (dcn.port != null) {
                writeLine(
                  "ss ${dcn.port} ${indexValue} ${identifier.property} ${a1}",
                );
              } else if (dcn.name != null && dcn.batchMode != null) {
                throw new Exception("Not supported operation.");
                // writeLine($"sbns {GenerateHashValue(dcn.Type)} {GenerateHashValue(dcn.Name)} {indexValue} {an.Property} r0");
              } else if (dcn.batchMode != null) {
                writeLine(
                  "sbs ${Utils.hashAsInt32(dcn.type)} ${indexValue} ${identifier.property} ${a1}",
                );
              } else {
                throw new Exception("Not supported Device Configuration.");
              }
            } else {
              if (dcn.port != null) {
                writeLine("s ${dcn.port} ${identifier.property} ${a1}");
              } else if (dcn.name != null && dcn.batchMode != null) {
                writeLine(
                  "sbn ${Utils.hashAsInt32(dcn.type)} ${Utils.hashAsInt32(dcn.name!)} ${identifier.property} ${a1}",
                );
              } else if (dcn.batchMode != null) {
                writeLine(
                  "sb ${Utils.hashAsInt32(dcn.type)} ${identifier.property} ${a1}",
                );
              } else {
                throw new Exception("Not supported Device Configuration.");
              }
            }
          } else {
            var rv = getVariable(identifier.identifier);
            var (rv_used, a1) = getNumericValueOrRegister(an.expression, rv);
            if (!rv_used) {
              writeLine("move r${rv} ${a1}");
            }
          }
          break;
        }
      case CallNode cn:
        {
          if (Keywords.SLEEP == cn.identifier) {
            if (cn.arguments.length != 1) {
              throw new Exception("");
            }

            var (result, a1) = getNumericValueOrRegister(cn.arguments[0], r);
            writeLine("sleep ${a1}");
          } else if (Keywords.YIELD == cn.identifier) {
            if (cn.arguments.length != 0) {
              throw new Exception("");
            }

            writeLine("yield");
          } else if (Keywords.HCF == cn.identifier) {
            if (cn.arguments.length != 0) {
              throw new Exception("");
            }

            writeLine("hcf");
          } else {
            List<int> reservedRegisters = [];
            String callArguments = "";

            for (int a = 0; a < cn.arguments.length; ++a) {
              int ra = reserveRegister();
              var (ra_used, a1) = getNumericValueOrRegister(
                cn.arguments[a],
                ra,
              );
              if (ra_used) {
                reservedRegisters.add(ra);
              }

              callArguments += " " + a1;
            }

            writeLine("${cn.identifier} r${r}" + callArguments);

            for (var p in reservedRegisters) {
              freeRegister(p);
            }
          }

          break;
        }
      default:
        throw new Exception("Not supported Node: ${node.runtimeType}");
    }
  }

  void generateOpositeJump(Node n, int r, String label) {
    if (n is ComparisonNode) {
      int rt = r;
      bool rt_used;
      String a1, a2;

      (rt_used, a1) = getNumericValueOrRegister(n.left, rt);
      if (rt_used) {
        rt = reserveRegister();
      }

      (rt_used, a2) = getNumericValueOrRegister(n.right, rt);
      if (rt_used && rt != r) {
        freeRegister(rt);
      }

      switch (n.operator) {
        case ComparisonOperatorType.OpEqual:
          writeLine("bne r${r} ${a1} ${a2} ${label}");
          break;
        case ComparisonOperatorType.OpNotEqual:
          writeLine("beq r${r} ${a1} ${a2} ${label}");
          break;
        case ComparisonOperatorType.OpLess:
          writeLine("bge r${r} ${a1} ${a2} ${label}");
          break;
        case ComparisonOperatorType.OpLessOrEqual:
          writeLine("bgt r${r} ${a1} ${a2} ${label}");
          break;
        case ComparisonOperatorType.OpGreater:
          writeLine("ble r${r} ${a1} ${a2} ${label}");
          break;
        case ComparisonOperatorType.OpGreaterOrEqual:
          writeLine("blt r${r} ${a1} ${a2} ${label}");
          break;
      }
    } else if (n is LogicalNode) {
      int rt = r;
      bool rt_used;
      String a1, a2;

      (rt_used, a1) = getNumericValueOrRegister(n.left, rt);
      if (rt_used) {
        rt = reserveRegister();
      }

      (rt_used, a2) = getNumericValueOrRegister(n.right, rt);
      if (rt_used && rt != r) {
        freeRegister(rt);
      }

      switch (n.operator) {
        case LogicalOperatorType.OpAnd:
          writeLine("blt ${a1} 1 ${label}");
          writeLine("blt ${a2} 1 ${label}");
          break;
        case LogicalOperatorType.OpOr:
          writeLine("brge ${a1} 1 3");
          writeLine("brge ${a2} 1 2");
          writeLine("j ${label}");
          break;
      }
    } else {
      var (_, a1) = getNumericValueOrRegister(n, r);
      writeLine("blt ${a1} 1 ${label}");
    }
  }

  (bool, bool) isConstantExpressionAndNotZero(Node n) {
    if (Utils.isValueNode(n)) {
      return (true, Utils.isNotZero(n));
    }

    return (false, false);
  }

  (bool, bool) isConstantExpressionAndTrue(Node n) {
    if (Utils.isValueNode(n)) {
      return (true, Utils.isTrue(n));
    }

    return (false, false);
  }

  String generateLabel(String prefix) {
    int counter = (_labelCounter[prefix] ?? 0) + 1;
    _labelCounter[prefix] = counter;
    return "${prefix}${counter.toString().padLeft(3, '0')}";
  }

  (bool, String) getNumericValueOrRegister(Node n, int r) {
    // constant expression
    if (n is NumericNode) {
      return (false, n.value);
    } else if (n is HashNode) {
      return (false, Utils.hashAsInt32(n.value).toString());
    } else if (n is ConstantNode) {
      return (false, n.value);
    }
    // access directly to variable
    else if (n is IdentifierNode && !_devices.containsKey(n.identifier)) {
      int rv = getVariable(n.identifier);
      return (false, "r${rv}");
    }

    generateMipsCode(n, r);
    return (true, "r${r}");
  }

  void writeLine(String line) {
    _output.writeln(line);
  }

  int registerVariable(String variable) {
    for (int i = MAX_REGISTER; i >= MIN_REGISTER; --i) {
      if (!_reservedRegisters.contains(i)) {
        _reservedRegisters.add(i);
        _variables[variable] = i;
        return i;
      }
    }

    throw new Exception("No more free register.");
  }

  int getVariable(String variable) {
    if (_variables.containsKey(variable)) {
      return _variables[variable]!;
    }

    throw new Exception("Variable not declared: ${variable}.");
  }

  bool hasVariable(String variable) {
    return _variables.containsKey(variable);
  }

  void freeRegister(int r) {
    if (_reservedRegisters.contains(r)) {
      _reservedRegisters.remove(r);
    } else {
      throw new Exception("Register already freed (${r}).");
    }
  }

  int reserveRegister() {
    for (int i = MIN_REGISTER; i < MAX_REGISTER; ++i) {
      if (!_reservedRegisters.contains(i)) {
        _reservedRegisters.add(i);
        return i;
      }
    }
    throw new Exception("No more free register.");
  }
}
