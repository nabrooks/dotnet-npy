using System.Text;

namespace DotNetNpyIo
{
    public static class IbmConverter
    {
        #region String

        private static readonly Encoding _unicode = Encoding.Unicode;
        private static readonly Encoding _ebcdic = Encoding.GetEncoding("IBM037");

        /// <summary>
        /// Returns a Unicode string converted from a byte array of EBCDIC encoded characters
        /// </summary>
        public static string ToString(byte[] value)
        {
            return ToString(value, 0);
        }

        /// <summary>
        /// Returns a Unicode string converted from a byte array of EBCDIC encoded characters 
        /// starting at the specified position
        /// </summary>
        /// <param name="startingIndex">
        /// Zero-based index of starting position in value array
        /// </param>
        public static string ToString(byte[] value, int startingIndex)
        {
            if (ReferenceEquals(null, value))
                throw new ArgumentNullException("value");
            return ToString(value, startingIndex, value.Length - startingIndex);
        }

        /// <summary>
        /// Returns a Unicode string converted from a byte array of EBCDIC encoded characters 
        /// starting at the specified position of the given length
        /// </summary>
        /// <param name="startingIndex">
        /// Zero-based index of starting position in value array
        /// </param>
        /// <param name="length">
        /// Number of characters to convert
        /// </param>
        public static string ToString(byte[] value, int startingIndex, int length)
        {
            var unicodeBytes = Encoding.Convert(_ebcdic, _unicode, value, startingIndex, length);
            return _unicode.GetString(unicodeBytes);
        }

        /// <summary>
        /// Returns a byte array of EBCDIC encoded characters converted from a Unicode string
        /// </summary>
        public static byte[] GetBytes(string value)
        {
            return GetBytes(value, 0);
        }

        /// <summary>
        /// Returns a byte array of EBCDIC encoded characters converted from a Unicode substring 
        /// starting at the specified position
        /// </summary>
        /// <param name="startingIndex">
        /// Zero-based starting index of substring
        /// </param>
        public static byte[] GetBytes(string value, int startingIndex)
        {
            return GetBytes(value, startingIndex, value.Length - startingIndex);
        }

        /// <summary>
        /// Returns a byte array of EBCDIC encoded characters converted from a Unicode substring 
        /// starting at the specified position with the given length
        /// </summary>
        /// <param name="startingIndex">
        /// Zero-based starting index of substring
        /// </param>
        /// <param name="length">
        /// Number of characters to convert
        /// </param>
        public static byte[] GetBytes(string value, int startingIndex, int length)
        {
            if (ReferenceEquals(null, value))
                throw new ArgumentNullException(nameof(value));
            var unicodeBytes = _unicode.GetBytes(value.ToCharArray(startingIndex, length));
            return Encoding.Convert(_unicode, _ebcdic, unicodeBytes);
        }

        #endregion

        #region Int16

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes encoding a big endian 16-bit signed integer
        /// </summary>
        public static Int16 ToInt16(byte[] value)
        {
            return ToInt16(value, 0);
        }

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes in a specified position encoding a big endian 16-bit signed integer
        /// </summary>
        public static short ToInt16(byte[] value, int startIndex)
        {
            return BitConverter.ToInt16(new[] { value[startIndex + 1], value[startIndex] }, 0);
        }

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes in a specified position encoding a big endian 16-bit signed integer
        /// </summary>
        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            return BitConverter.ToUInt16(new byte[] { value[startIndex + 1], value[startIndex] }, 0);
        }

        /// <summary>
        /// Returns two bytes encoding a big endian 16-bit signed integer given a 16-bit signed integer
        /// </summary>
        public static byte[] GetBytes(short value)
        {
            var b = BitConverter.GetBytes(value);
            Array.Reverse(b);
            return b;
        }

        #endregion

        #region Int32

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes encoding a big endian 32-bit signed integer
        /// </summary>
        public static int ToInt32(byte[] value)
        {
            return ToInt32(value, 0);
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position encoding a big endian 32-bit signed integer
        /// </summary>
        public static Int32 ToInt32(byte[] value, int startIndex)
        {
            return BitConverter.ToInt32(new[] { value[startIndex + 3], value[startIndex + 2], value[startIndex + 1], value[startIndex] }, 0);
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position encoding a big endian 32-bit signed integer
        /// </summary>
        public static UInt32 ToUInt32(byte[] value, int startIndex)
        {
            return BitConverter.ToUInt32(new[] { value[startIndex + 3], value[startIndex + 2], value[startIndex + 1], value[startIndex] }, 0);
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position encoding a big endian 32-bit signed integer
        /// </summary>
        public static Int64 ToInt64(byte[] value, int startIndex)
        {
            return BitConverter.ToInt64(new[]
            {
                value[startIndex + 7], value[startIndex + 6], value[startIndex + 5], value[startIndex + 4],
                value[startIndex + 3], value[startIndex + 2], value[startIndex + 1], value[startIndex]
            }, 0);
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position encoding a big endian 32-bit signed integer
        /// </summary>
        public static UInt64 ToUInt64(byte[] value, int startIndex)
        {
            return BitConverter.ToUInt64(new[]
            {
                value[startIndex + 7], value[startIndex + 6], value[startIndex + 5], value[startIndex + 4],
                value[startIndex + 3], value[startIndex + 2], value[startIndex + 1], value[startIndex]
            }, 0);
        }

        public unsafe static byte[] GetBytes(int value)
        {
            byte[] bytes = new byte[4];
            fixed (byte* b = bytes)
            { *((int*)b) = value; }
            byte tmp = bytes[0];
            bytes[0] = bytes[3];
            bytes[3] = tmp;
            tmp = bytes[1];
            bytes[1] = bytes[2];
            bytes[2] = tmp;
            return bytes;
        }

        public static byte[] GetBytes(ushort value)
        {
            var b = BitConverter.GetBytes(value);
            return new[] { b[1], b[0] };
        }

        #endregion

        #region Single

        //private static readonly int IbmBase = 16;
        //private static readonly byte ExponentBias = 64;
        //private static readonly float ThreeByteShift = 16777216;

        public unsafe static byte[] GetBytes(float value)
        {
            return GetBytes(*(int*)&value);
        }

        /// <summary>
        /// Converts a little endian byte array to an IEEE formatted floating point array, then converts to an IBM formatted floating point array.
        /// </summary>
        /// <param name="bytes">Little endian byte array.</param>
        /// <returns>An IBM formatted floating point array.</returns>
        public static unsafe float[] IbmByteArrayToFloatArray(byte[] bytes)
        {
            CodeContract.Requires(bytes != null, "The byte array must not be null.");
            CodeContract.Requires(bytes.Length % 4 == 0, "The length of the byte array must be a multiple of four in order to convert to a float array.");

            float[] floats = new float[bytes.Length / 4];

            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

            ibm_to_float(floats, floats, floats.Length);

            return floats;
        }

        public static unsafe float[] ibm_to_float(float[] array)
        {
            var result = new float[array.Length];
            ibm_to_float(array, result, array.Length);
            return result;
        }

        public static unsafe void ibm_to_float(float[] array, int ns)
        {
            ibm_to_float(array, array, ns);
        }

        public static unsafe void ibm_to_float(float[] from, float[] to, int ns)
        {
            fixed (float* frm = from)
            {
                fixed (float* t = to)
                {
                    ibm_to_float((int*)frm, (int*)t, ns);
                }
            }
        }

        public static unsafe void float_to_ibm(float[] from, float[] to, int ns)
        {
            fixed (float* frm = from)
            {
                fixed (float* t = to)
                {
                    float_to_ibm((int*)frm, (int*)t, ns);
                }
            }
        }

        private static unsafe void ibm_to_float(int* from, int* to, int n)
        {
            // IBM data is big-endian; reverse to native layout, then convert to IEEE bits.
            for (int i = 0; i < n; ++i)
                to[i] = IbmFloat.IbmBitsToIeeeBits(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(from[i]));
        }

        private static unsafe void float_to_ibm(int* from, int* to, int n)
        {
            // Convert IEEE bits to IBM (native layout), then reverse to big-endian on the wire.
            for (int i = 0; i < n; ++i)
                to[i] = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(IbmFloat.IeeeBitsToIbmBits(from[i]));
        }
        #endregion
    }
}
