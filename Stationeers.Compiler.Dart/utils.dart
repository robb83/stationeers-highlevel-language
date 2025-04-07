import 'ast.dart';
import 'keywords.dart';
import 'package:archive/archive.dart';
import 'dart:convert';

class Utils {
  static const EPSILON = 4.94065645841247E-324;
  static Map<String, double> _constantValues = {
    Keywords.EPSILON: 4.94065645841247E-324,
    Keywords.NINF: double.negativeInfinity,
    Keywords.PINF: double.infinity,
    Keywords.NAN: double.nan,
    Keywords.DEG2RAD: 0.0174532923847437,
    Keywords.RAD2DEG: 57.2957801818848,
    Keywords.PI: 3.14159265358979,
  };

  static bool isConstantExpression(String str) {
    return _constantValues.containsKey(str);
  }

  static bool isValueNode(Node n) {
    if (n is NumericNode) {
      return true;
    } else if (n is ConstantNode) {
      return _constantValues.containsKey(n.value);
    } else if (n is HashNode) {
      return true;
    }

    return false;
  }

  static double getValue(Node n) {
    if (n is NumericNode) {
      if (n.value.isEmpty) {
        throw new Exception("Invalid number literal (${n.value}).");
      }

      int offset = 0;
      bool negative = false;

      if (n.value[offset] == '+') {
        ++offset;
      } else if (n.value[offset] == '-') {
        ++offset;
        negative = true;
      }

      if (offset < n.value.length) {
        if (n.value[offset] == '%') {
          var value = int.parse(
            n.value.substring(offset + 1).replaceAll("_", ""),
            radix: 2,
          );
          return (negative ? -value : value).toDouble();
        } else if (n.value[offset] == '\$') {
          var value = int.parse(n.value.substring(offset + 1), radix: 16);
          return (negative ? -value : value).toDouble();
        }

        return double.parse(n.value);
      }
    } else if (n is ConstantNode) {
      return _constantValues[n.value]!;
    } else if (n is HashNode) {
      return hashAsDouble(n.value);
    }

    throw new Exception("Not supported number node: ${n}.");
  }

  static bool isEqual(Node left, Node right) {
    double a = getValue(left);
    double b = getValue(right);

    return a == b;
  }

  static bool isNotZero(Node nn) {
    double v = getValue(nn);
    return v != 0.0;
  }

  static bool isZero(Node nn) {
    double v = getValue(nn);
    return v.abs() < EPSILON;
  }

  static bool isOne(Node nn) {
    double v = getValue(nn);
    return v == 1.0;
  }

  static bool isTrue(Node n) {
    return !(getValue(n) < 1.0);
  }

  static double hashAsDouble(String value) {
    return getCrc32(utf8.encode(value)).toSigned(32).toDouble();
  }

  static int hashAsInt32(String value) {
    return getCrc32(utf8.encode(value)).toSigned(32);
  }
}
