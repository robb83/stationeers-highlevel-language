using Stationeers.Compiler.AST;
using System.Globalization;
using System.IO.Hashing;
using System.Text;
using System;
using System.Collections.Generic;

namespace Stationeers.Compiler
{
    public static class Utils
    {
        private static Dictionary<String, double> _constantValues;

        static Utils()
        {
            _constantValues = new Dictionary<string, double>
            {
                { Keywords.EPSILON, Double.Epsilon },
                { Keywords.NINF, Double.NegativeInfinity },
                { Keywords.PINF, Double.PositiveInfinity },
                { Keywords.NAN, Double.NaN },
                { Keywords.DEG2RAD, 0.0174532923847437 },
                { Keywords.RAD2DEG, 57.2957801818848 },
                { Keywords.PI, 3.14159265358979 }
            };
        }

        public static bool IsConstantExpression(String str)
        {
            return _constantValues.TryGetValue(str, out double value);
        }

        public static bool IsValueNode(Node n)
        {
            if (n is NumericNode nn)
            {
                return true;
            }
            else if (n is ConstantNode constn)
            {
                if (_constantValues.TryGetValue(constn.Value, out double value))
                {
                    return true;
                }

                return false;
            }
            else if (n is HashNode hashn)
            {
                return true;
            }

            return false;
        }

        public static Double GetValue(Node n)
        {
            if (n is NumericNode nn)
            {
                if (String.IsNullOrEmpty(nn.Value))
                {
                    throw new Exception($"Invalid number literal ({nn.Value}).");
                }

                if (nn.Value[0] == '%')
                {
                    return Convert.ToInt64(nn.Value.Substring(1).Replace("_", ""), 2);
                } 
                else if (nn.Value[0] == '$')
                {
                    return Convert.ToInt64(nn.Value.Substring(1), 16);
                }

                return Double.Parse(nn.Value, CultureInfo.InvariantCulture);
            }
            else if (n is ConstantNode constn)
            {
                if (_constantValues.TryGetValue(constn.Value, out double value))
                {
                    return value;
                }

                throw new Exception($"Not supported constant expression: {constn.Value}.");
            } 
            else if (n is HashNode hashn)
            {
                return HashAsDouble(hashn.Value);
            }

            throw new Exception($"Not supported number node: {n?.GetType()?.Name}.");
        }

        public static bool IsEqual(Node left, Node right)
        {
            double a = GetValue(left);
            double b = GetValue(right);

            return a == b;
        }

        public static bool IsZero(Node nn)
        {
            double v = GetValue(nn);
            return Math.Abs(v) < Double.Epsilon;
        }

        public static bool IsOne(Node nn)
        {
            double v = GetValue(nn);
            return v == 1.0;
        }

        public static bool IsTrue(Node n)
        {
            return !(GetValue(n) < 1.0);
        }

        public static double HashAsDouble(String value)
        {
            var hash = BitConverter.ToInt32(Crc32.Hash(Encoding.ASCII.GetBytes(value)), 0);
            return hash;
        }
        
        public static Int32 HashAsInt32(string value)
        {
            var hash = BitConverter.ToInt32(Crc32.Hash(Encoding.ASCII.GetBytes(value)), 0);
            return hash;
        }
    }
}
