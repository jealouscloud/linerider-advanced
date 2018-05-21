using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace linerider.IO.json
{
    // a C# implementation of lz-string decode i wrote to be fairly quick
    // https://github.com/pieroxy/lz-string
    public class LZString
    {
        static string keyStrBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        static Dictionary<string, Dictionary<char, int>> baseReverseDic = new Dictionary<string, Dictionary<char, int>>();
        static int[] base64Dictionary = null;

        private static int getBaseValue(string alphabet, char character)
        {
            if (!baseReverseDic.ContainsKey(alphabet))
            {
                baseReverseDic[alphabet] = new Dictionary<char, int>();
                for (int i = 0; i < alphabet.Length; i++)
                {
                    baseReverseDic[alphabet][alphabet[i]] = i;
                }
            }
            return baseReverseDic[alphabet][character];
        }
        private static int getBase64Value(char character)
        {
            if (base64Dictionary == null)
            {
                base64Dictionary = new int[256];
                for (int i = 0; i < keyStrBase64.Length; i++)
                {
                    base64Dictionary[keyStrBase64[i]] = i;
                }
            }

            return base64Dictionary[character];
        }

        public static string decompress(string compressed)
        {
            if (compressed == null) return "";
            if (compressed == "") return null;
            return _decompress(compressed.Length, 32768, compressed);
        }


        private struct dec_data
        {
            public int val, position, index;
            public string input;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int new_bits(int maxpower, ref dec_data data)
        {
            int bits = 0;
            int power = 1;
            int resb = 0;
            while (power != maxpower)
            {
                resb = data.val & data.position;
                data.position >>= 1;
                if (data.position == 0)
                {
                    data.position = 32768;
                    data.val = NextValue(ref data);
                }
                bits |= (resb > 0 ? 1 : 0) * power;
                power <<= 1;
            }
            return bits;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureSize(ref char[][] array, int size)
        {
            if (size >= array.Length)
                Array.Resize(ref array, size * 4);
        }
        public static string decompressBase64(string compressed)
        {
            return decompress(getstring(Convert.FromBase64String(compressed)));
        }
        public static string getstring(byte[] compressed)
        {
            if (compressed == null) return "";
            else
            {
                int[] buf = new int[compressed.Length / 2];
                for (int i = 0, TotalLen = buf.Length; i < TotalLen; i++)
                {
                    buf[i] = ((int)compressed[i * 2]) * 256 + ((int)compressed[i * 2 + 1]);
                }
                char[] result = new char[buf.Length];
                for (int i = 0; i < buf.Length; i++)
                {
                    result[i] = (char)(buf[i]);
                }
                return new string(result);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NextValue(ref dec_data data)
        {
            return data.input[data.index++];
        }
        private unsafe static string _decompress(int length, int resetValue, string input)
        {
            var powers = stackalloc int[32];
            for (int i = 0; i < 32; i++)
            {
                powers[i] = (int)Math.Pow(2, i);
            }
            char[][] dictionary = new char[length * 4][];
            char[][] result = new char[length * 4][];
            int resultArraySize = 0;
            int charstoreturn = 0;
            int dictSize = 0;
            int next;
            int enlargeIn = 4;
            int numBits = 3;
            int c = 0;
            var data = new dec_data() { position = resetValue, index = 0, input = input };
            data.val = NextValue(ref data);
            EnsureSize(ref dictionary, dictSize + 4);
            EnsureSize(ref result, resultArraySize + 2);
            for (int i = 0; i < 3; i++)
            {
                dictionary[dictSize++] = new char[] { (char)i };
            }
            int bits = 0;
            bits = new_bits(powers[2], ref data);
            switch (next = bits)
            {
                case 0:
                    bits = new_bits(powers[8], ref data);
                    c = (char)(bits);
                    break;
                case 1:
                    bits = new_bits(powers[16], ref data);
                    c = (char)(bits);
                    break;
                case 2:
                    return "";
            };
            char[] w = new char[1] { (char)c };
            //string w = new string(c, 1);
            result[resultArraySize++] = w;
            charstoreturn += w.Length;
            dictionary[dictSize++] = w;
            char[] entry;
            while (true)
            {
                if (data.index > length)
                {
                    return "";
                }

                bits = new_bits(powers[numBits], ref data);
                c = bits;
                switch (bits)
                {
                    case 0:
                        bits = new_bits(powers[8], ref data);
                        dictionary[dictSize++] = new char[] { (char)(bits) };
                        c = (char)(dictSize - 1);
                        enlargeIn--;
                        break;
                    case 1:
                        bits = new_bits(powers[16], ref data);
                        dictionary[dictSize++] = new char[] { (char)(bits) };
                        c = (char)(dictSize - 1);
                        enlargeIn--;
                        break;
                    case 2:
                        StringBuilder ret = new StringBuilder(charstoreturn);
                        foreach (var str in result)
                        {
                            ret.Append(str);
                        }
                        return ret.ToString();
                }
                if (enlargeIn == 0)
                {
                    enlargeIn = powers[numBits];
                    numBits++;
                }
                char[] dictadd;
                if (c < dictSize)
                {
                    entry = dictionary[c];
                    dictadd = add(w, entry[0]);
                }
                else
                {
                    if (c == dictSize)
                    {
                        entry = add(w, w[0]);
                        dictadd = entry;
                    }
                    else
                    {
                        return null;
                    }
                }
                result[resultArraySize++] = entry;
                charstoreturn += entry.Length;
                // Add w+entry[0] to the dictionary.
                dictionary[dictSize++] = dictadd;
                w = entry;
                enlargeIn--;
                if (enlargeIn == 0)
                {
                    enlargeIn = powers[numBits];
                    numBits++;
                }
                EnsureSize(ref dictionary, dictSize + 3);
                EnsureSize(ref result, resultArraySize + 3);
            }
        }
        private static readonly int machinebits = IntPtr.Size * 8;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static char[] add(char[] w, char c)
        {
            var wlen = w.Length;
            char[] wplus0 = new char[wlen + 1];
            fixed (char* ptr = wplus0)
            {
                fixed (char* wptr = w)
                {
                    ptr[wlen] = c;
                    char* target = ptr;
                    char* src = wptr;
                    Buffer.MemoryCopy(wptr, target, wlen * sizeof(char), wlen * sizeof(char));
                }
            }
            return wplus0;
        }
    }
}
