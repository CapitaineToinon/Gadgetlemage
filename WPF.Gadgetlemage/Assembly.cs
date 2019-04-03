using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WPF.Gadgetlemage
{
    public static class Assembly
    {
        private static Regex asmLineRx = new Regex(@"^[\w\d]+:\s+((?:[\w\d][\w\d] ?)+)");

        private static byte[] loadDefuseOutput(string lines)
        {
            List<byte> bytes = new List<byte>();
            foreach (string line in Regex.Split(lines, "[\r\n]+"))
            {
                Match match = asmLineRx.Match(line);
                string hexes = match.Groups[1].Value;
                foreach (Match hex in Regex.Matches(hexes, @"\S+"))
                    bytes.Add(Byte.Parse(hex.Value, System.Globalization.NumberStyles.AllowHexSpecifier));
            }
            return bytes.ToArray();
        }

        public static byte[] PTDE = loadDefuseOutput(Properties.Resources.PTDE);
        public static byte[] REMASTERED = loadDefuseOutput(Properties.Resources.REMASTERED);
    }
}
