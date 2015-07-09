using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SpotDown_V2.Classes
{
    /// <summary>
    /// Please give credit to me if you use this or any part of it.
    /// HF: http://www.hackforums.net/member.php?action=profile&uid=1389752
    /// GitHub: https://github.com/DarkN3ss61
    /// Website: http://jlynx.net/
    /// Twitter: https://twitter.com/jLynx_DarkN3ss
    /// </summary>
    public static class FormatLeftTime
    {
        private static string[] TimeUnitsNames = { "Milli", "Sec", "Min", "Hour", "Day", "Month", "Year", "Decade", "Century" };
        private static int[] TimeUnitsValue = { 1000, 60, 60, 24, 30, 12, 10, 10 };//refrernce unit is milli
        public static string Format(long millis)
        {
            string format = "";
            for (int i = 0; i < TimeUnitsValue.Length; i++)
            {
                long y = millis % TimeUnitsValue[i];
                millis = millis / TimeUnitsValue[i];
                if (y == 0) continue;
                format = y + " " + TimeUnitsNames[i] + " , " + format;
            }

            format = format.Trim(',', ' ');
            if (format == "") return "0 Sec";
            else return format;
        }
    }
}
