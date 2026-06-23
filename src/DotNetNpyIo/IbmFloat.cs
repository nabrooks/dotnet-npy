namespace DotNetNpyIo
{
    /// <summary>
    /// Single source of truth for IBM/360 hexadecimal floating-point conversions. These were
    /// previously copy-pasted (with subtle variations) into the readers, writers and the various
    /// Ibm* converters. Centralising them removes ~19 duplicated loops and, crucially, applies the
    /// fmant==0 guard in one place so the normalisation loop can never spin forever.
    /// </summary>
    /// <remarks>
    /// All methods operate on bits in native <see cref="int"/> layout — i.e. the caller is
    /// responsible for any byte-order reversal (use <c>System.Buffers.Binary.BinaryPrimitives.ReverseEndianness</c>)
    /// before/after, exactly as the original inline code did.
    /// </remarks>
    internal static class IbmFloat
    {
        /// <summary>
        /// Converts 32-bit IBM/360 hex-float bits to IEEE-754 single bits.
        /// </summary>
        public static int IbmBitsToIeeeBits(int fconv)
        {
            unchecked
            {
                if (fconv == 0)
                    return 0;

                int fmant = 0x00ffffff & fconv;

                // Guard: if no mantissa bits intersect 0x00ffffff the normalisation loop below
                // would shift 0 forever (e.g. some inputs round-tripped from exact powers of two).
                if (fmant == 0)
                    return 0;

                int t = ((0x7f000000 & fconv) >> 22) - 130;
                while ((fmant & 0x00800000) == 0) { --t; fmant <<= 1; }

                if (t > 254)
                    return (int)(0x80000000 & fconv) | 0x7f7fffff;
                if (t <= 0)
                    return 0;
                return (int)(0x80000000 & fconv) | (t << 23) | (0x007fffff & fmant);
            }
        }

        /// <summary>
        /// Converts 32-bit IEEE-754 single bits to IBM/360 hex-float bits.
        /// </summary>
        public static int IeeeBitsToIbmBits(int fconv)
        {
            unchecked
            {
                if (fconv == 0)
                    return 0;

                int fmant = (0x007fffff & fconv) | 0x00800000;
                int t = ((0x7f800000 & fconv) >> 23) - 126;
                while ((t & 0x3) != 0) { ++t; fmant >>= 1; }

                return (int)(0x80000000 & fconv) | (((t >> 2) + 64) << 24) | fmant;
            }
        }
    }
}
