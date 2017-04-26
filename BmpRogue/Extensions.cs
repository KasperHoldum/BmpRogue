using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitmapRogue
{
    /// <summary>
    /// Extends the functionality of existing classes.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Reverses the string. This method is taken from http://weblogs.sqlteam.com/mladenp/archive/2006/03/19/9350.aspx
        /// </summary>
        /// <param name="x">string to reverse.</param>
        /// <returns>The reversed string.</returns>
        public static string Reverse(this string x)
        {
            char[] charArray = new char[x.Length];
            int len = x.Length - 1;
            for (int i = 0; i <= len; i++)
                charArray[i] = x[len - i];
            return new string(charArray);
        }
    }
}
