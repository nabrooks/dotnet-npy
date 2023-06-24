using System.Buffers.Binary;
using System.Text;

namespace DotNetNpyIo
{
    /// <summary>
    ///  THIS IS AN ATTEMPT AT TESTING OUT NEW BINARYPRIMITIVE APIS, IN GENERAL, PERF 
    ///  TESTS BETWEEN OLD APIS AND THIS NEW ONE ARE MARGINALLY DIFFERENT AT BEST AND LIKELY
    ///  ARE NOT STATISTICALLY SIGNIFICANT ENOUGH TO WARRANT LARGE CHANGES TO THIS IMPLEMENTATION
    /// </summary>
    internal class NewBigEndianBinaryReader : BinaryReader, IBinaryReader
    {
        #region Fields not directly related to properties
        /// <summary>
        /// Buffer used for temporary storage before conversion into primitives
        /// </summary>
        byte[] buffer = new byte[BaseTwoPower.TwentyFour];// new byte[16];

        #endregion

        public NewBigEndianBinaryReader(Stream input) : base(input) { }

        public NewBigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }

        public NewBigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public unsafe void ReadMany<T>(ref T[] objects, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (objects.Length < count) throw new ArgumentException("The array buffer must be initialized with length greater than 'count' prior to method invocation.");
            if (count == 0) return;

            var tType = typeof(T);

            T[] result = objects;

            if (tType == typeof(bool))
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadBoolean();
                    result[i] = (T)(object)value;
                }
            else if (tType == typeof(byte))
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadByte();
                    result[i] = (T)(object)value;
                }
            else if (tType == typeof(sbyte))
                for (int i = 0; i < count; i++)
                {
                    var value = base.ReadSByte();
                    result[i] = (T)(object)value;
                }
            else if (tType == typeof(short))
            {
                BaseStream.Read(buffer, 0, count * 2);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadInt16();
            }
            else if (tType == typeof(ushort))
            {
                BaseStream.Read(buffer, 0, count * 2);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadUInt16();
            }

            else if (tType == typeof(int))
            {
                BaseStream.Read(buffer, 0, count * 4);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadInt32();
            }
            else if (tType == typeof(uint))
            {
                BaseStream.Read(buffer, 0, count * 4);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadUInt32();
            }
            else if (tType == typeof(long))
            {
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadInt64();
            }
            else if (tType == typeof(ulong))
            {
                BaseStream.Read(buffer, 0, count * 8);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadUInt64();
            }

            else if (tType == typeof(Half))
            {
                BaseStream.Read(buffer, 0, count * 2);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadHalf();
            }
            else if (tType == typeof(float))
            {
                BaseStream.Read(buffer, 0, count * 4);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadSingle();
            }
            else if (tType == typeof(double))
            {
                BaseStream.Read(buffer, 0, count * 8);
                for (int i = 0; i < count; i++)
                    result[i] = (T)(object)this.ReadDouble();
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
                return (T)(object)ReadUInt16(); ;
            }
            else if (tType == typeof(ushort))
            {
                BaseStream.Read(buffer, 0, 2);
                return (T)(object)ReadUInt16();
            }
            else if (tType == typeof(int))
            {
                BaseStream.Read(buffer, 0, 4);
                return (T)(object)ReadInt32();
            }
            else if (tType == typeof(uint))
            {
                BaseStream.Read(buffer, 0, 4);
                return (T)(object)ReadUInt32();
            }
            else if (tType == typeof(long))
            {
                BaseStream.Read(buffer, 0, 8);
                return (T)(object)ReadInt64();
            }
            else if (tType == typeof(ulong))
            {
                BaseStream.Read(buffer, 0, 8);
                return (T)(object)this.ReadUInt64();
            }
            else if (tType == typeof(Half))
            {
                BaseStream.Read(buffer, 0, 2);
                return (T)(object)this.ReadHalf();
            }
            else if (tType == typeof(float))
            {
                BaseStream.Read(buffer, 0, 4);
                return (T)(object)this.ReadSingle();
            }
            else if (tType == typeof(double))
            {
                BaseStream.Read(buffer, 0, 8);
                return (T)(object)this.ReadDouble();
            }
            else throw new NotSupportedException("type not supported");
        }

        public unsafe T[] ReadMany<T>(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new T[0];

            var tType = typeof(T);
            T[] result = new T[count];
            ReadMany<T>(ref result, count);
            return result;
        }

        public bool[] ReadBooleans(int count)
        {
            bool[] booleans = new bool[count];
            for (int i = 0; i < count; i++)
                booleans[i] = base.ReadBoolean();
            return booleans;
        }

        public override double ReadDouble()
        {
            BaseStream.Read(buffer, 0, 8);
            return BinaryPrimitives.ReadDoubleBigEndian(buffer);
        }

        public double[] ReadDoubles(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new double[0];

            BaseStream.Read(buffer, 0, count * 8);
            Span<byte> bufferSpan = buffer;
            double[] result = new double[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadDoubleBigEndian(bufferSpan.Slice(i * 8), out result[i]);
            return result;
        }

        public double ReadIbmDouble()
        {
            throw new NotImplementedException();
        }

        public double[] ReadIbmDoubles(int count)
        {
            throw new NotImplementedException();
        }

        public unsafe float ReadIbmSingle()
        {
            BaseStream.Read(buffer, 0, 4);
            int fmant;
            int t;
            var fconv = BinaryPrimitives.ReadInt32BigEndian(buffer);
            /// reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)
            //int fconv = ((int*)bptr)[0];
            //fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   

            ///  re interpret float according to ibm360 encoding standard
            if (fconv != 0)
            {
                fmant = 0x00ffffff & fconv;
                t = (int)((0x7f000000 & fconv) >> 22) - 130;
                while ((fmant & 0x00800000) == 0) { --t; fmant <<= 1; }
                if (t > 254) fconv = (int)((0x80000000 & fconv) | 0x7f7fffff);
                else if (t <= 0) fconv = 0;
                else fconv = (int)(0x80000000 & fconv) | (t << 23) | (0x007fffff & fmant);
            }
            return *(float*)&fconv;
        }

        public unsafe float[] ReadIbmSingles(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new float[0];

            BaseStream.Read(buffer, 0, count * 4);
            Span<byte> bufferSpan = buffer;
            float[] copy = new float[count];
            int fmant;
            int t;
            int fconv;
            for (int i = 0; i < count; i++)
            {
                BinaryPrimitives.TryReadInt32BigEndian(bufferSpan.Slice(i * 4), out fconv);
                //fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);   // reordering bytes to accomodate big endian initial encoding. (WAAAY faster than array indexing to reorder)

                ///  re interpret float according to ibm360 encoding standard
                if (fconv != 0)
                {
                    fmant = 0x00ffffff & fconv;
                    t = (int)((0x7f000000 & fconv) >> 22) - 130;
                    while ((fmant & 0x00800000) == 0) { --t; fmant <<= 1; }
                    if (t > 254) fconv = (int)((0x80000000 & fconv) | 0x7f7fffff);
                    else if (t <= 0) fconv = 0;
                    else fconv = (int)(0x80000000 & fconv) | (t << 23) | (0x007fffff & fmant);
                }
                copy[i] = *(float*)&fconv;
            }
            return copy;
        }

        public override short ReadInt16()
        {
            BaseStream.Read(buffer, 0, 2);
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        }

        public short[] ReadInt16s(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new short[0];

            BaseStream.Read(buffer, 0, count * 2);
            Span<byte> bufferSpan = buffer;
            short[] result = new short[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadInt16BigEndian(bufferSpan.Slice(i * 2), out result[i]);
            return result;
        }

        public override int ReadInt32()
        {
            BaseStream.Read(buffer, 0, 4);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public int[] ReadInt32s(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new int[0];

            BaseStream.Read(buffer, 0, count * 4);
            Span<byte> bufferSpan = buffer;
            int[] result = new int[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadInt32BigEndian(bufferSpan.Slice(i * 4), out result[i]);
            return result;
        }

        public override long ReadInt64()
        {
            BaseStream.Read(buffer, 0, 8);
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
        }

        public long[] ReadInt64s(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new long[0];

            BaseStream.Read(buffer, 0, count * 8);
            Span<byte> bufferSpan = buffer;
            long[] result = new long[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadInt64BigEndian(bufferSpan.Slice(i * 8), out result[i]);
            return result;
        }

        public sbyte[] ReadSBytes(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new sbyte[0];

            BaseStream.Read(buffer, 0, count);
            return Array.ConvertAll(buffer, b => unchecked((sbyte)b));
        }

        public override Half ReadHalf()
        {
            BaseStream.Read(buffer, 0, 2);
            return BinaryPrimitives.ReadHalfBigEndian(buffer);
        }

        public Half[] ReadHalfs(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new Half[0];

            BaseStream.Read(buffer, 0, count * 2);
            Span<byte> bufferSpan = buffer;
            Half[] result = new Half[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadHalfBigEndian(bufferSpan.Slice(i * 2), out result[i]);
            return result;
        }

        public override float ReadSingle()
        {
            BaseStream.Read(buffer, 0, 4);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public float[] ReadSingles(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new float[0];

            BaseStream.Read(buffer, 0, count * 4);
            Span<byte> bufferSpan = buffer;
            float[] result = new float[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadSingleBigEndian(bufferSpan.Slice(i * 4), out result[i]);
            return result;
        }

        public override ushort ReadUInt16()
        {
            BaseStream.Read(buffer, 0, 2);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public ushort[] ReadUInt16s(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new ushort[0];

            BaseStream.Read(buffer, 0, count * 2);
            Span<byte> bufferSpan = buffer;
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadUInt16BigEndian(bufferSpan.Slice(i * 2), out result[i]);
            return result;
        }

        public override uint ReadUInt32()
        {
            BaseStream.Read(buffer, 0, 4);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public uint[] ReadUInt32s(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"count: {count}");
            if (count == 0) return new uint[0];

            BaseStream.Read(buffer, 0, count * 4);
            Span<byte> bufferSpan = buffer;
            uint[] result = new uint[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadUInt32BigEndian(bufferSpan.Slice(i * 4), out result[i]);
            return result;
        }

        public override ulong ReadUInt64()
        {
            BaseStream.Read(buffer, 0, 8);
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
        }

        public ulong[] ReadUInt64s(int count)
        {
            BaseStream.Read(buffer, 0, count * 8);
            Span<byte> bufferSpan = buffer;
            ulong[] result = new ulong[count];
            for (int i = 0; i < count; i++)
                BinaryPrimitives.TryReadUInt64BigEndian(bufferSpan.Slice(i * 8), out result[i]);
            return result;
        }


    }


}
