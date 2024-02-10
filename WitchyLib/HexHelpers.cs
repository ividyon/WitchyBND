using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPlus;

namespace WitchyLib;

public static class HexHelpers
{
    #region Hex Helpers

        public static byte[] FriendlyHexToByteArray(this string hex)
        {
            if (!IsValidHexString(hex))
            {
                throw new FriendlyException("A hex string in a CustomData's value could not be parsed as hex. Valid hex characters are: 0-9 A-F a-f");
            }

            if (hex.Length == 0)
            {
                hex = "00000000";
                Console.Error.WriteLine("Warning: Hex string was empty, adding 00000000...");
            }

            if (hex.Length % 2 != 0)
            {
                hex += "0";
                Console.Error.WriteLine("Warning: Hex string was not divisible by 2, adding 0...");
            }

            if (hex.Length / 2 % 4 != 0)
            {
                while (hex.Length / 2 % 4 != 0)
                {
                    hex += "00";
                }
                Console.Error.WriteLine("Warning: Hex string was not divisible by 4 for Custom type of CustomData, added 00 until it was.");
            }

            try
            {
                return hex.HexToByteArray();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An issue occurred in parsing a hex string into a byte array.");
                Console.Error.WriteLine(ex.Message.PromptPlusEscape());
                Console.Error.WriteLine(ex.StackTrace.PromptPlusEscape());
                throw;
            }
        }

        public static bool IsValidHexString(IEnumerable<char> hexString)
        {
            return hexString.Select((char currentCharacter) =>
                (currentCharacter >= '0' && currentCharacter <= '9')
             || (currentCharacter >= 'a' && currentCharacter <= 'f')
             || (currentCharacter >= 'A' && currentCharacter <= 'F')
             ).All((bool isHexCharacter) => isHexCharacter);
        }

        public static string ToHexString(this byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString().ToUpper();
        }

        public static byte[] HexToByteArray(this string hex)
        {
            int charLength = hex.Length;
            byte[] bytes = new byte[charLength / 2];
            for (int i = 0; i < charLength; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        #endregion
}