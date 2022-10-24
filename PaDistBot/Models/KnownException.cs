using System;

namespace PaDistBot.Models
{
    public class KnownException : Exception
    {
        public KnownException(string s) : base(s)
        {

        }
    }
}