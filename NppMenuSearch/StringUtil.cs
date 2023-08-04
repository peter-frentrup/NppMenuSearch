using System;
using System.Collections.Generic;
using System.Text;

namespace NppMenuSearch
{
    static class StringUtil
    {
        public static IEnumerable<string> SplitAt(this string str, char separator)
        {
            int partPos = 0;
            while (partPos < str.Length)
            {
                int separatorPos = partPos;
                while (separatorPos < str.Length)
                {
                    if (str[separatorPos] == separator)
                        break;

                    ++separatorPos;
                }

                string part = str.Substring(partPos, separatorPos - partPos);

                yield return part;
                partPos = separatorPos + 1;
            }
        }

        public static string Riffle(this IEnumerable<string> items, string separator)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;

            foreach (string s in items)
            {
                if (first)
                    first = false;
                else
                    sb.Append(separator);
                sb.Append(s);
            }

            return sb.ToString();
        }

        public static IEnumerable<string> SplitCamelCaseWords(this string str)
        {
            int pos = 0;
            while (pos < str.Length)
            {
                int next = pos + 1;
                while (next < str.Length)
                {
                    if (str[next] >= 'A' && str[next] <= 'Z')
                        break;
                    ++next;
                }

                if (next == pos + 1)
                {
                    while (next < str.Length)
                    {
                        if (!(str[next] >= 'A' && str[next] <= 'Z'))
                            break;
                        ++next;
                    }
                }

                yield return str.Substring(pos, next - pos);
                pos = next;
            }
        }

        public static bool IsAscii(this char ch)
        {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
        }

        public static bool IsDigit(this char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        public static bool IsAsciiOrDigit(this char ch)
        {
            return ch.IsAscii() || ch.IsDigit();
        }

        public static string Before(this string s, string sub)
        {
            int i = s.IndexOf(sub);
            if (i < 0)
                return s;
            return s.Substring(0, i);
        }

        public static string After(this string s, string sub)
        {
            int i = s.IndexOf(sub);
            if (i < 0)
                return "";
            return s.Substring(i + sub.Length);
        }

        public static bool EqualsCaseless(this string s, string s2)
        {
            if (s == null)
                return s2 == null;

            if (s2 == null)
                return false;

            return s.Equals(s2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string TrySubstring(this string s, int start)
        {
            return s.TrySubstring(start, s.Length - start);
        }

        public static string TrySubstring(this string s, int start, int length)
        {
            if (length <= 0 || s == null)
                return "";

            int end = start + length;
            if (start < 0)
                start = 0;
            if (end > s.Length)
                end = s.Length;

            if (end <= start)
                return "";

            return s.Substring(start, end - start);
        }

        public static string Repeat(this string s, int count)
        {
            StringBuilder sb = new StringBuilder();
            while (count-- > 0)
                sb.Append(s);
            return sb.ToString();
        }

        public static string RemovePreviousWord(this string s, int pos)
        {
            int n = ((pos <= s.Length) ? pos : s.Length) - 1;
            while (n >= 0 && Char.IsWhiteSpace(s[n])) { --n; } // skipping the trailing spaces
            while (n >= 0 && !Char.IsWhiteSpace(s[n])) { --n; } // skipping the last word
            while (n >= 0 && Char.IsWhiteSpace(s[n])) { --n; } // skipping the spaces before the last word
            return s.Substring(0, n + 1) + s.Substring(pos);
        }
    }
}
