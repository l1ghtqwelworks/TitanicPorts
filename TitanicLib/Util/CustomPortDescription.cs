using Open.Nat;
using ShipwreckLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TitanicLib.Objects
{
    public static class CustomPortDescription
    {
        public const char StartChar = '(', EndChar = ')';
        public const string Marking = "Titanic";
        public static readonly string StartKey = "" + StartChar + StartChar + Marking + StartChar, EndKey = "" + EndChar;

        public static bool IsValidCustomName(string description, Protocol protocol)
        {
            try
            {
                return description.StartsWith(StartKey) 
                    && description.EndsWith(EndKey) 
                    && Port.ProtocolParse(description.Substring(StartKey.Length, description.IndexOf("" + EndChar + EndChar, StartKey.Length) - StartKey.Length)) == protocol;
            }
            catch
            {
                return false;
            }
        }

        public static string UnmarkCustom(string description)
        {
            int lastIndex = description.Length - EndKey.Length - 1;
            int descriptionStartIndex = description.LastIndexOf(EndChar, lastIndex) + 1;
            return description.Substring(descriptionStartIndex, lastIndex - descriptionStartIndex + 1);
        }

        public static string MarkCustom(string description, Protocol protocol)
        {
            return StartKey + protocol.ToString() + EndChar + EndChar + description.ToString() + EndKey;
        }

    }
}
