using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http.Utils
{
    /// <summary>
    /// BoyerMoore算法，百度百科来的
    /// https://baike.baidu.com/item/Boyer-%20Moore%E7%AE%97%E6%B3%95/16548374?fr=aladdin
    /// </summary>
    public class BoyerMoore
    {
        private static int MAX_CHAR = 256;
        public static int Max(int a, int b) { return (a > b) ? a : b; }

        private static void PreBmBc(byte[] pattern, int m, int[] bmBc)
        {
            int i;

            for (i = 0; i < MAX_CHAR; i++)
                bmBc[i] = m;
            for (i = 0; i < m - 1; i++)
                bmBc[pattern[i]] = m - 1 - i;
        }

        private static void suffix(byte[] pattern, int m, int[] suff)  //改进计算suffix方法
        {
            int f = 0, g, i;

            suff[m - 1] = m;
            g = m - 1;
            for (i = m - 2; i >= 0; --i)
            {
                if (i > g && suff[i + m - 1 - f] < i - g)
                    suff[i] = suff[i + m - 1 - f];
                else
                {
                    if (i < g)
                        g = i;
                    f = i;
                    while (g >= 0 && pattern[g] == pattern[g + m - 1 - f])
                        --g;
                    suff[i] = f - g;
                }
            }
        }

        private static void PreBmGs(byte[] pattern, int m, int[] bmGs)
        {
            int i, j;
            int[] suff = new int[m];

            // 计算后缀数组
            suffix(pattern, m, suff);

            // 先全部赋值为m，包含Case3
            for (i = 0; i < m; i++)
                bmGs[i] = m;

            // Case2
            j = 0;
            for (i = m - 1; i >= 0; i--)
            {
                if (i + 1 == suff[i])
                {
                    for (; j < m - 1 - i; j++)
                    {
                        if (m == bmGs[j])
                            bmGs[j] = m - 1 - i;
                    }
                }
            }

            // Case1
            for (i = 0; i <= m - 2; i++)
                bmGs[m - 1 - suff[i]] = m - 1 - i;

        }

        public static void PrepareBoyerMoore(byte[] pattern, out int[] bmBc, out int[] bmGs )
        {
            int m = pattern.Length;
            bmBc = new int[MAX_CHAR];
            bmGs = new int[m];

            PreBmBc(pattern, m, bmBc);
            PreBmGs(pattern, m, bmGs);
        }

        public static int Search(byte[] pattern, byte[] text, out int lastJ, out int nextSize)
        {
            int i, j;
            int m = pattern.Length;
            int n = text.Length;
            int[] bmBc;
            int[] bmGs;
            PrepareBoyerMoore(pattern, out bmBc, out bmGs);
            nextSize = 0;
            lastJ = -1;
            j = 0;
            while (j <= n - m)
            {
                for (i = m - 1; i >= 0 && pattern[i] == text[i + j]; i--) ;
                if (i < 0)
                {
                    return j;
                }
                else
                {
                    lastJ = j;
                    nextSize = Max(bmBc[text[i + j]] - m + 1 + i, bmGs[i]);
                    j += nextSize;
                }
            }
            return -1;
        }
    }
}
