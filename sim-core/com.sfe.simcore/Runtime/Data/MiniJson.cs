using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SFE.SimCore.Data
{
    /// <summary>
    /// Kleiner JSON-Leser ohne Fremdabhängigkeit, damit der Core seine Leveldateien in Unity,
    /// in dotnet-Tests und genauso wie die Python-Referenz lesen kann.
    /// Ergebnis: Dictionary&lt;string, object&gt;, List&lt;object&gt;, double, string, bool oder null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            int i = 0;
            object value = ParseValue(json, ref i);
            SkipWhitespace(json, ref i);
            if (i != json.Length) throw Error(json, i, "Unerwartete Zeichen nach dem Ende");
            return value;
        }

        public static Dictionary<string, object> ParseObject(string json) =>
            Parse(json) as Dictionary<string, object> ?? throw new FormatException("JSON-Objekt erwartet.");

        // ---- Zugriffshelfer ----

        public static Dictionary<string, object> Obj(this Dictionary<string, object> o, string key) =>
            (Dictionary<string, object>)Get(o, key);

        public static List<object> Arr(this Dictionary<string, object> o, string key) => (List<object>)Get(o, key);

        public static double Num(this Dictionary<string, object> o, string key) => Convert.ToDouble(Get(o, key), CultureInfo.InvariantCulture);

        public static int Int(this Dictionary<string, object> o, string key) => (int)Math.Round(o.Num(key));

        public static string Str(this Dictionary<string, object> o, string key) => (string)Get(o, key);

        public static bool Has(this Dictionary<string, object> o, string key) => o.ContainsKey(key) && o[key] != null;

        public static string[] StrArray(this Dictionary<string, object> o, string key)
        {
            if (!o.Has(key)) return new string[0];
            var list = o.Arr(key);
            var result = new string[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = (string)list[i];
            return result;
        }

        public static double[] NumArray(List<object> list)
        {
            var result = new double[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = Convert.ToDouble(list[i], CultureInfo.InvariantCulture);
            return result;
        }

        private static object Get(Dictionary<string, object> o, string key)
        {
            if (!o.TryGetValue(key, out object v)) throw new KeyNotFoundException($"JSON-Feld '{key}' fehlt.");
            return v;
        }

        // ---- Parser ----

        private static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) throw Error(s, i, "Wert erwartet");
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObjectBody(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return ParseString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default:
                    if (c == '-' || char.IsDigit(c)) return ParseNumber(s, ref i);
                    throw Error(s, i, $"Unerwartetes Zeichen '{c}'");
            }
        }

        private static Dictionary<string, object> ParseObjectBody(string s, ref int i)
        {
            var result = new Dictionary<string, object>();
            i++; // {
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return result; }
            while (true)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(s, i, "Feldname erwartet");
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(s, i, "':' erwartet");
                i++;
                result[key] = ParseValue(s, ref i);
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return result; }
                throw Error(s, i, "',' oder '}' erwartet");
            }
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var result = new List<object>();
            i++; // [
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return result; }
            while (true)
            {
                result.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return result; }
                throw Error(s, i, "',' oder ']' erwartet");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // "
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(s, i, "Unvollständiges \\u");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw Error(s, i, $"Unbekannte Escape-Sequenz '\\{e}'");
                }
            }
            throw Error(s, i, "String nicht abgeschlossen");
        }

        private static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error(s, i, $"'{word}' erwartet");
            i += word.Length;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static FormatException Error(string s, int i, string message) =>
            new FormatException($"JSON-Fehler an Position {i}: {message}.");
    }
}
