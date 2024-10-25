using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class StringExtension
    {
        public static byte[] ASCII(this string s)
        {
            return Encoding.ASCII.GetBytes(s);
        }
        public static string Words(this string s)
        {
            var words = "";
            foreach (var c in s)
            {
                if (char.IsUpper(c) && words.Length > 0)
                {
                    words += ' ';
                }
                words += c;
            }
            return words;
        }
        public static byte[] UTF8(this string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
        public static string ASCII(this byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }
        public static string UTF8(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        public static string ToBase64(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static long ToLong(this string s, int startIndex)
        {
            long a = 0;
            for (int i = startIndex; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ',' || c == '.') continue;
                if (!char.IsDigit(c)) break;

                a = (a << 1) + (a << 3) + (c & 15);
            }
            return a;
        }
        public static long ToLong(this string s) => ToLong(s, 0);
    }
}
