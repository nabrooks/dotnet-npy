using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNetNpyIo
{
    internal static class EndianUtilities
    {
        static UInt16HalfMap UshortHalfMap = new UInt16HalfMap();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Swap(ushort val)
        {
            unchecked
            {
                return (ushort)(((val & 0xFF00U) >> 8) | ((val & 0x00FFU) << 8));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Swap(short val)
        {
            unchecked
            {
                return (short)Swap((ushort)val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Swap(uint val)
        {
            return (val << 24) | ((val >> 24) & 0xff) | ((val & 0xff00) << 8) | ((val & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
            //// Swap adjacent 16-bit blocks
            //val = (val >> 16) | (val << 16);
            //// Swap adjacent 8-bit blocks
            //val = ((val & 0xFF00FF00U) >> 8) | ((val & 0x00FF00FFU) << 8);
            //return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Swap(int val)
        {
            return (val << 24) | ((val >> 24) & 0xff) | ((val & 0xff00) << 8) | ((val & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Swap(ulong val)
        {
            // Swap adjacent 32-bit blocks
            val = (val >> 32) | (val << 32);
            // Swap adjacent 16-bit blocks
            val = ((val & 0xFFFF0000FFFF0000U) >> 16) | ((val & 0x0000FFFF0000FFFFU) << 16);
            // Swap adjacent 8-bit blocks
            val = ((val & 0xFF00FF00FF00FF00U) >> 8) | ((val & 0x00FF00FF00FF00FFU) << 8);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Swap(long val)
        {
            unchecked
            {
                return (long)Swap((ulong)val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half Swap(Half val)
        {
            UshortHalfMap.Half = val;
            UshortHalfMap.UInt16 = Swap(UshortHalfMap.UInt16);
            return UshortHalfMap.Half;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Swap(float val)
        {
            // (Inefficient) alternatives are BitConverter.ToSingle(BitConverter.GetBytes(val).Reverse().ToArray(), 0)
            // and BitConverter.ToSingle(BitConverter.GetBytes(Swap(BitConverter.ToInt32(BitConverter.GetBytes(val), 0))), 0)
            UInt32SingleMap map = new UInt32SingleMap() { Single = val };
            map.UInt32 = Swap(map.UInt32);
            return map.Single;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Swap(double val)
        {
            // We *could* use BitConverter.Int64BitsToDouble(Swap(BitConverter.DoubleToInt64Bits(val))), 
            // but that throws if system endianness isn't LittleEndian
            UInt64DoubleMap map = new UInt64DoubleMap() { Double = val };
            map.UInt64 = Swap(map.UInt64);
            return map.Double;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Swap(decimal val)
        {
            //Load four 32 bit integers from the Decimal.GetBits function
            Int32[] bits = decimal.GetBits(val);
            //Create a temporary list to hold the bytes
            List<byte> bytes = new List<byte>();
            //iterate each 32 bit integer
            foreach (Int32 i in bits)
            {
                //add the bytes of the current 32bit integer
                //to the bytes list
                bytes.AddRange(BitConverter.GetBytes(i));
            }
            byte[] bytesArray = bytes.ToArray();
            Array.Reverse(bytesArray);

            //make an array to convert back to int32's
            Int32[] bits2 = new Int32[4];
            for (int i = 0; i <= 15; i += 4)
            {
                //convert every 4 bytes into an int32
                bits[i / 4] = BitConverter.ToInt32(bytesArray, i);
            }
            //Use the decimal's new constructor to
            //create an instance of decimal
            return new decimal(bits);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UInt16HalfMap
        {
            [FieldOffset(0)] public ushort UInt16;
            [FieldOffset(0)] public Half Half;

        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UInt32SingleMap
        {
            [FieldOffset(0)] public uint UInt32;
            [FieldOffset(0)] public float Single;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UInt64DoubleMap
        {
            [FieldOffset(0)] public ulong UInt64;
            [FieldOffset(0)] public double Double;
        }
    }


}
