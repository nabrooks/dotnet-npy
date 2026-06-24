using System.Buffers.Binary;
using System.Text;

namespace DotNetNpyIo
{
    public class BigEndianBinaryReader : BinaryReader, IBinaryReader
    {
        #region Fields not directly related to properties
        /// <summary>
        /// Buffer used for temporary storage before conversion into primitives
        /// </summary>
        byte[] buffer = new byte[BaseTwoPower.TwentyFour];// new byte[16];
        #endregion

        public BigEndianBinaryReader(Stream input) : base(input) { }

        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }

        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public unsafe void ReadMany<T>(ref T[] objects, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (objects.Length < count) throw new ArgumentException("The array buffer must be initialized wiht length greater than 'count' prior to method invocation.");

            var tType = typeof(T);

            if (tType == typeof(bool))
            {
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadBoolean();
                    objects[i] = (T)(object)value;
                }
            }
            else if (tType == typeof(byte))
            {
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadByte();
                    objects[i] = (T)(object)value;
                }
            }
            else if (tType == typeof(sbyte))
            {
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadSByte();
                    objects[i] = (T)(object)value;
                }
            }
            else if (tType == typeof(short))
            {
                BaseStream.Read(buffer, 0, count * 2);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort fconv = ((ushort*)pBuffer)[i];
                        fconv = EndianUtilities.Swap(fconv);
                        objects[i] = (T)(object)*((short*)&fconv);
                    }
                }
            }
            else if (tType == typeof(ushort))
            {
                BaseStream.Read(buffer, 0, count * 2);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort fconv = ((ushort*)pBuffer)[i];
                        fconv = EndianUtilities.Swap(fconv);
                        objects[i] = (T)(object)*&fconv;
                    }
                }
            }
            else if (tType == typeof(int))
            {
                BaseStream.Read(buffer, 0, count * 4);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int fconv = ((int*)bptr)[i];
                        objects[i] = (T)(object)((fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8));   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                    }
                }
            }
            else if (tType == typeof(uint))
            {
                BaseStream.Read(buffer, 0, count * 4);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint fconv = ((uint*)bptr)[i];
                        objects[i] = (T)(object)((fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8));   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                    }
                }
            }
            else if (tType == typeof(long))
            {
                BaseStream.Read(buffer, 0, count * 8);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        long fconv = ((long*)pBuffer)[i];
                        objects[i] = (T)(object)EndianUtilities.Swap(fconv);
                    }
                }
            }
            else if (tType == typeof(ulong))
            {
                BaseStream.Read(buffer, 0, count * 8);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var value = ((ulong*)pBuffer)[i];
                        objects[i] = (T)(object)EndianUtilities.Swap(value);
                        //value = (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 | (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 | (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 | (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
                    }
                }
            }
            else if (tType == typeof(Half))
            {
                BaseStream.Read(buffer, 0, count * 2);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort fconv = ((ushort*)bptr)[i];
                        fconv = EndianUtilities.Swap(fconv);

                        var floatValue = *((float*)&fconv);
                        var value = (Half)floatValue;
                        //var value = *((Half*)&fconv);
                        objects[i] = (T)(object)value;
                    }
                }
            }
            else if (tType == typeof(float))
            {
                BaseStream.Read(buffer, 0, count * 4);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int fconv = ((int*)bptr)[i];
                        fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                        objects[i] = (T)(object)*((float*)&fconv);
                    }
                }
            }
            else if (tType == typeof(double))
            {
                BaseStream.Read(buffer, 0, count * 8);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ulong fconv = ((ulong*)bptr)[i];
                        fconv = EndianUtilities.Swap(fconv);
                        var value = *((double*)&fconv);
                        objects[i] = (T)(object)value;
                    }
                }
            }
            else throw new NotSupportedException("type not supported");
        }

        public unsafe T Read<T>()
        {
            var tType = typeof(T);

            if (tType == typeof(bool))
            {
                var value = base.ReadBoolean();
                return (T)(object)value;
            }
            else if (tType == typeof(byte))
            {
                var value = base.ReadByte();
                return (T)(object)value;
            }
            else if (tType == typeof(sbyte))
            {
                var value = base.ReadSByte();
                return (T)(object)value;
            }

            else if (tType == typeof(short))
            {
                BaseStream.Read(buffer, 0, 2);
                fixed (byte* pBuffer = buffer)
                {
                    ushort fconv = ((ushort*)pBuffer)[0];
                    fconv = EndianUtilities.Swap(fconv);
                    return (T)(object)*((short*)&fconv);
                }
            }
            else if (tType == typeof(ushort))
            {
                BaseStream.Read(buffer, 0, 2);
                fixed (byte* pBuffer = buffer)
                {
                    ushort fconv = ((ushort*)pBuffer)[0];
                    fconv = EndianUtilities.Swap(fconv);
                    return (T)(object)*&fconv;
                }
            }

            else if (tType == typeof(int))
            {
                BaseStream.Read(buffer, 0, 4);
                fixed (byte* bptr = buffer)
                {
                    int fconv = ((int*)bptr)[0];
                    return (T)(object)((fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8));   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                }
            }
            else if (tType == typeof(uint))
            {
                BaseStream.Read(buffer, 0, 4);
                fixed (byte* bptr = buffer)
                {
                    uint fconv = ((uint*)bptr)[0];
                    return (T)(object)((fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8));   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                }
            }
            else if (tType == typeof(long))
            {
                BaseStream.Read(buffer, 0, 8);
                fixed (byte* pBuffer = buffer)
                {
                    long fconv = ((long*)pBuffer)[0];
                    return (T)(object)EndianUtilities.Swap(fconv);
                }
            }
            else if (tType == typeof(ulong))
            {
                BaseStream.Read(buffer, 0, 8);
                fixed (byte* pBuffer = buffer)
                {
                    var value = ((ulong*)pBuffer)[0];
                    return (T)(object)EndianUtilities.Swap(value);
                    //value = (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 | (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 | (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 | (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
                }
            }

            else if (tType == typeof(Half))
            {
                BaseStream.Read(buffer, 0, 2);
                fixed (byte* bptr = buffer)
                {

                    ushort fconv = ((ushort*)bptr)[0];
                    fconv = EndianUtilities.Swap(fconv);
                    var floatValue = *((float*)&fconv);
                    //var value = (Half)floatValue;
                    var value = *((Half*)&fconv);
                    return (T)(object)value;
                }
            }
            else if (tType == typeof(float))
            {
                BaseStream.Read(buffer, 0, 4);
                fixed (byte* bptr = buffer)
                {

                    int fconv = ((int*)bptr)[0];
                    fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                    return (T)(object)*((float*)&fconv);
                }
            }
            else if (tType == typeof(double))
            {
                BaseStream.Read(buffer, 0, 8);
                fixed (byte* bptr = buffer)
                {

                    ulong fconv = ((ulong*)bptr)[0];
                    fconv = EndianUtilities.Swap(fconv);
                    var value = *((double*)&fconv);
                    return (T)(object)value;
                }
            }
            else throw new NotSupportedException("type not supported");
        }

        public unsafe T[] ReadMany<T>(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new T[0];

            var tType = typeof(T);
            T[] result = new T[count];

            if (tType == typeof(bool))
            {
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadBoolean();
                    result[i] = (T)(object)value;
                }
            }
            else if (tType == typeof(byte))
            {
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadByte();
                    result[i] = (T)(object)value;
                }
            }
            else if (tType == typeof(sbyte))
            {
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadSByte();
                    result[i] = (T)(object)value;
                }
            }

            else if (tType == typeof(short))
            {
                BaseStream.Read(buffer, 0, count * 2);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort fconv = ((ushort*)pBuffer)[i];
                        fconv = EndianUtilities.Swap(fconv);
                        result[i] = (T)(object)*((short*)&fconv);
                    }
                    return result;
                }
            }
            else if (tType == typeof(ushort))
            {
                BaseStream.Read(buffer, 0, count * 2);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort fconv = ((ushort*)pBuffer)[i];
                        fconv = EndianUtilities.Swap(fconv);
                        result[i] = (T)(object)*&fconv;
                    }
                    return result;
                }
            }

            else if (tType == typeof(int))
            {
                BaseStream.Read(buffer, 0, count * 4);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int fconv = ((int*)bptr)[i];
                        result[i] = (T)(object)((fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8));   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                    }
                    return result;
                }
            }
            else if (tType == typeof(uint))
            {
                BaseStream.Read(buffer, 0, count * 4);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint fconv = ((uint*)bptr)[i];
                        result[i] = (T)(object)((fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8));   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                    }
                    return result;
                }
            }
            else if (tType == typeof(long))
            {
                BaseStream.Read(buffer, 0, count * 8);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        long fconv = ((long*)pBuffer)[i];
                        result[i] = (T)(object)EndianUtilities.Swap(fconv);
                    }
                }
            }
            else if (tType == typeof(ulong))
            {
                BaseStream.Read(buffer, 0, count * 8);
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var value = ((ulong*)pBuffer)[i];
                        result[i] = (T)(object)EndianUtilities.Swap(value);
                        //value = (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 | (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 | (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 | (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
                    }
                }
            }

            else if (tType == typeof(Half))
            {
                BaseStream.Read(buffer, 0, count * 2);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort fconv = ((ushort*)bptr)[i];
                        fconv = EndianUtilities.Swap(fconv);

                        var floatValue = *((float*)&fconv);
                        var value = (Half)floatValue;
                        //var value = *((Half*)&fconv);
                        result[i] = (T)(object)value;
                    }
                }
            }
            else if (tType == typeof(float))
            {
                BaseStream.Read(buffer, 0, count * 4);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int fconv = ((int*)bptr)[i];
                        fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                        result[i] = (T)(object)*((float*)&fconv);
                    }
                }
            }
            else if (tType == typeof(double))
            {
                BaseStream.Read(buffer, 0, count * 8);
                fixed (byte* bptr = buffer)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ulong fconv = ((ulong*)bptr)[i];
                        fconv = EndianUtilities.Swap(fconv);
                        var value = *((double*)&fconv);
                        result[i] = (T)(object)value;
                    }
                }
            }
            else throw new NotSupportedException("type not supported");

            return result;
        }

        public bool[] ReadBooleans(int count)
        {
            bool[] booleans = new bool[count];
            for (int i = 0; i < count; i++)
                booleans[i] = base.ReadBoolean();
            return booleans;
        }

        public unsafe override double ReadDouble()
        {
            BaseStream.Read(buffer, 0, 8);
            fixed (byte* bptr = buffer)
            {
                ulong fconv = ((ulong*)bptr)[0];
                fconv = EndianUtilities.Swap(fconv);
                return *((double*)&fconv);
            }
        }

        public double[] ReadDoubles(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new double[0];

            BaseStream.ReadExactly(buffer, 0, count * 8);
            var span = buffer.AsSpan(0, count * 8);
            double[] copy = new double[count];
            for (int i = 0; i < count; i++)
                copy[i] = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(i * 8));
            return copy;
        }

        public double ReadIbmDouble()
        {
            throw new NotImplementedException();
        }

        public unsafe double[] ReadIbmDoubles(int count)
        {
            throw new NotImplementedException();
        }

        public unsafe float ReadIbmSingle()
        {
            BaseStream.Read(buffer, 0, 4);
            fixed (byte* bptr = buffer)
            {
                int fconv = IbmFloat.IbmBitsToIeeeBits(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(((int*)bptr)[0]));
                return *(float*)&fconv;
            }
        }

        public unsafe float[] ReadIbmSingles(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new float[0];

            BaseStream.Read(buffer, 0, count * 4);
            fixed (byte* bptr = buffer)
            {
                float[] copy = new float[count];
                for (int i = 0; i < count; i++)
                {
                    int fconv = IbmFloat.IbmBitsToIeeeBits(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(((int*)bptr)[i]));
                    copy[i] = *(float*)&fconv;
                }
                return copy;
            }
        }

        public unsafe override short ReadInt16()
        {
            BaseStream.Read(buffer, 0, 2);
            fixed (byte* bptr = buffer)
            {
                short fconv = ((short*)bptr)[0];
                return EndianUtilities.Swap(fconv);
            }
        }

        public short[] ReadInt16s(int count)
        {
            BaseStream.ReadExactly(buffer, 0, count * 2);
            var span = buffer.AsSpan(0, count * 2);
            short[] result = new short[count];
            for (int i = 0; i < count; i++)
                result[i] = BinaryPrimitives.ReadInt16BigEndian(span.Slice(i * 2));
            return result;
        }

        public unsafe override int ReadInt32()
        {
            BaseStream.Read(buffer, 0, 4);
            fixed (byte* pBuffer = buffer)
            {
                int fconv = ((int*)pBuffer)[0];
                fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                return fconv;
            }
        }

        public int[] ReadInt32s(int count)
        {
            BaseStream.ReadExactly(buffer, 0, count * 4);
            var span = buffer.AsSpan(0, count * 4);
            int[] copy = new int[count];
            for (int i = 0; i < count; i++)
                copy[i] = BinaryPrimitives.ReadInt32BigEndian(span.Slice(i * 4));
            return copy;
        }

        public unsafe override long ReadInt64()
        {
            BaseStream.Read(buffer, 0, 8);
            fixed (byte* pBuffer = buffer)
            {
                long fconv = ((long*)pBuffer)[0];
                return EndianUtilities.Swap(fconv);
            }
        }

        public long[] ReadInt64s(int count)
        {
            BaseStream.ReadExactly(buffer, 0, count * 8);
            var span = buffer.AsSpan(0, count * 8);
            long[] result = new long[count];
            for (int i = 0; i < count; i++)
                result[i] = BinaryPrimitives.ReadInt64BigEndian(span.Slice(i * 8));
            return result;
        }

        public unsafe sbyte[] ReadSBytes(int count)
        {
            sbyte[] result = new sbyte[count];
            BaseStream.Read(buffer, 0, count);
            fixed (byte* pBuffer = buffer)
            {
                for (int i = 0; i < count; i++)
                {
                    sbyte fconv = ((sbyte*)pBuffer)[i];
                    result[i] = fconv;
                }
                return result;
            }
        }

        public unsafe Half ReadHalf()
        {
            BaseStream.Read(buffer, 0, 2);
            fixed (byte* bptr = buffer)
            {
                ushort fconv = ((ushort*)bptr)[0];
                fconv = EndianUtilities.Swap(fconv);
                return *((Half*)&fconv);
            }
        }

        public unsafe Half[] ReadHalfs(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new Half[0];

            BaseStream.Read(buffer, 0, count * 2);
            fixed (byte* bptr = buffer)
            {
                Half[] copy = new Half[count];
                for (int i = 0; i < count; i++)
                {
                    ushort fconv = ((ushort*)bptr)[i];
                    fconv = EndianUtilities.Swap(fconv);
                    copy[i] = *((Half*)&fconv);
                }
                return copy;
            }
        }

        public unsafe override float ReadSingle()
        {
            BaseStream.Read(buffer, 0, 4);
            fixed (byte* bptr = buffer)
            {
                int fconv = ((int*)bptr)[0];
                fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
                return *((float*)&fconv);
            }
        }

        public float[] ReadSingles(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new float[0];

            BaseStream.ReadExactly(buffer, 0, count * 4);
            var span = buffer.AsSpan(0, count * 4);
            float[] copy = new float[count];
            for (int i = 0; i < count; i++)
                copy[i] = BinaryPrimitives.ReadSingleBigEndian(span.Slice(i * 4));
            return copy;
        }

        public unsafe override ushort ReadUInt16()
        {
            BaseStream.Read(buffer, 0, 2);
            fixed (byte* bptr = buffer)
            {
                ushort fconv = ((ushort*)bptr)[0];
                return EndianUtilities.Swap(fconv);
            }
        }

        public ushort[] ReadUInt16s(int count)
        {
            BaseStream.ReadExactly(buffer, 0, count * 2);
            var span = buffer.AsSpan(0, count * 2);
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
                result[i] = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(i * 2));
            return result;
        }

        public unsafe override uint ReadUInt32()
        {
            BaseStream.Read(buffer, 0, 4);
            fixed (byte* bptr = buffer)
            {
                uint fconv = ((uint*)bptr)[0];
                return EndianUtilities.Swap(fconv);
            }
        }

        public uint[] ReadUInt32s(int count)
        {
            BaseStream.ReadExactly(buffer, 0, count * 4);
            var span = buffer.AsSpan(0, count * 4);
            uint[] result = new uint[count];
            for (int i = 0; i < count; i++)
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(i * 4));
            return result;
        }

        public unsafe override ulong ReadUInt64()
        {
            BaseStream.Read(buffer, 0, 8);
            fixed (byte* pBuffer = buffer)
            {
                var value = ((ulong*)pBuffer)[0];
                return EndianUtilities.Swap(value);
            }
        }

        public ulong[] ReadUInt64s(int count)
        {
            BaseStream.ReadExactly(buffer, 0, count * 8);
            var span = buffer.AsSpan(0, count * 8);
            ulong[] result = new ulong[count];
            for (int i = 0; i < count; i++)
                result[i] = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(i * 8));
            return result;
        }
    }


}
