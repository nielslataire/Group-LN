using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCore.Helpers
{
    public class AlphanumComparator : IComparer<string>
    {
        public int Compare(string x, string y)
        {

            // [1] Validate the arguments.
            string s1 = x;
            if (s1 == null)
                return 0;

            string s2 = y;
            if (s2 == null)
                return 0;

            int len1 = s1.Length;
            int len2 = s2.Length;
            int marker1 = 0;
            int marker2 = 0;

            // [2] Loop over both Strings.
            while (marker1 < len1 & marker2 < len2)
            {

                // [3] Get Chars.
                char ch1 = s1[marker1];
                char ch2 = s2[marker2];

                char[] space1 = new char[len1 + 1];
                int loc1 = 0;
                char[] space2 = new char[len2 + 1];
                int loc2 = 0;

                // [4] Collect digits for String one.
                do
                {
                    space1[loc1] = ch1;
                    loc1 += 1;
                    marker1 += 1;
                    if (marker1 < len1)
                        ch1 = s1[marker1];
                    else
                        break;
                }
                while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

                // [5] Collect digits for String two.
                do
                {
                    space2[loc2] = ch2;
                    loc2 += 1;
                    marker2 += 1;
                    if (marker2 < len2)
                        ch2 = s2[marker2];
                    else
                        break;
                }
                while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

                // [6] Convert to Strings.
                var str1 = new string(space1);
                var str2 = new string(space2);

                // [7] Parse Strings into Integers.
                int result;
                if (char.IsDigit(space1[0]) & char.IsDigit(space2[0]))
                {
                    var thisNumericChunk = int.Parse(str1);
                    var thatNumericChunk = int.Parse(str2);
                    result = thisNumericChunk.CompareTo(thatNumericChunk);
                }
                else
                    result = str1.CompareTo(str2);

                // [8] Return result if not equal.
                if (result != 0)
                    return result;
            }

            // [9] Compare lengths.
            return len1 - len2;
        }
    }
}
