﻿namespace PdfXenon.Standard
{
    public class TokenXRefEntry : TokenObject
    {
        public TokenXRefEntry(long position, int id, int gen, int offset, bool used)
            : base(position)
        {
            Id = id;
            Gen = gen;
            Offset = offset;
            Used = used;
        }

        public override string ToString()
        {
            return $"XRef ({Position}): Id:{Id} Gen:{Gen} Offset:{Offset} Used:{Used}";
        }

        public int Id { get; private set; }
        public int Gen { get; private set; }
        public int Offset { get; private set; }
        public bool Used { get; private set; }
    }
}