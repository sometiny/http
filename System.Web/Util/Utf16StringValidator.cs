namespace System.Web.Util
{
    internal class Utf16StringValidator
    {
        internal static string ValidateString(string input, bool skipUtf16Validation)
        {
            if (skipUtf16Validation || string.IsNullOrEmpty(input))
            {
                return input;
            }
            int num = -1;
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsSurrogate(input[i]))
                {
                    num = i;
                    break;
                }
            }
            if (num < 0)
            {
                return input;
            }
            char[] chArray = input.ToCharArray();
            for (int j = num; j < chArray.Length; j++)
            {
                char c = chArray[j];
                if (char.IsLowSurrogate(c))
                {
                    chArray[j] = (char)0xfffd;
                }
                else if (char.IsHighSurrogate(c))
                {
                    if (((j + 1) < chArray.Length) && char.IsLowSurrogate(chArray[j + 1]))
                    {
                        j++;
                    }
                    else
                    {
                        chArray[j] = (char)0xfffd;
                    }
                }
            }
            return new string(chArray);
        }

    }
}
