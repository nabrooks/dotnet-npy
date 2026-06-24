using System.Buffers.Binary;

namespace DotNetNpyIo
{
    public class BigEndianBinaryWriter : BinaryWriter, IBinaryWriter
    {
        byte[] buffer = new byte[BaseTwoPower.TwentyFour];

        public BigEndianBinaryWriter(Stream stream) : base(stream) { }

        public void Write<T>(T value)
        {
            // NOTE: dispatch directly to the non-generic BinaryWriter overloads via 'base'
            // (with the appropriate endian swap) rather than to this type's Write(short) etc.
            // overloads. Calling the unqualified Write(...) here re-binds to this generic
            // method (Write<T>) and recurses infinitely -> StackOverflow.
            var tType = typeof(T);
            if (tType == typeof(bool))
                base.Write((bool)(object)value);
            else if (tType == typeof(byte))
                base.Write((byte)(object)value);
            else if (tType == typeof(sbyte))
                base.Write((sbyte)(object)value);

            else if (tType == typeof(short))
                base.Write(EndianUtilities.Swap((short)(object)value));
            else if (tType == typeof(ushort))
                base.Write(EndianUtilities.Swap((ushort)(object)value));
            else if (tType == typeof(int))
                base.Write(EndianUtilities.Swap((int)(object)value));
            else if (tType == typeof(uint))
                base.Write(EndianUtilities.Swap((uint)(object)value));
            else if (tType == typeof(long))
                base.Write(EndianUtilities.Swap((long)(object)value));
            else if (tType == typeof(ulong))
                base.Write(EndianUtilities.Swap((ulong)(object)value));

            else if (tType == typeof(Half))
                base.Write(EndianUtilities.Swap(BitConverter.HalfToInt16Bits((Half)(object)value)));
            else if (tType == typeof(float))
                base.Write(EndianUtilities.Swap((float)(object)value));
            else if (tType == typeof(double))
                base.Write(EndianUtilities.Swap((double)(object)value));
            else
                throw new NotSupportedException($"Type {typeof(T)} not supported");
        }

        public void Write<T>(T[] values, int count)
        {
            if (count > values.Length)
                throw new ArgumentException($"There arent {count} elements in the array to write to file: array length {values.Length}");

            var tType = typeof(T);
            int byteCount = count;
            var typeSize = 1;
            var typeSizeHalf = 0;

            if (tType == typeof(bool) || tType == typeof(byte) || tType == typeof(sbyte))
            {
                byteCount = count * sizeof(bool);
                Buffer.BlockCopy(values, 0, buffer, 0, byteCount);
                BaseStream.Write(buffer, 0, byteCount);
                return;
            }
            else if (tType == typeof(short) || tType == typeof(ushort) || tType == typeof(char) || tType == typeof(Half))
            {
                byteCount = count * sizeof(short);
                typeSize = sizeof(short);
                typeSizeHalf = sizeof(short) / 2;
            }
            else if (tType == typeof(int) || tType == typeof(uint) || tType == typeof(float))
            {
                byteCount = count * sizeof(int);
                typeSize = sizeof(int);
                typeSizeHalf = sizeof(int) / 2;
            }
            else if (tType == typeof(long) || tType == typeof(ulong) || tType == typeof(double))
            {
                byteCount = count * sizeof(long);
                typeSize = sizeof(long);
                typeSizeHalf = sizeof(long) / 2;
            }
            else if (tType == typeof(decimal))
            {
                byteCount = count * sizeof(decimal);
                typeSize = sizeof(decimal);
                typeSizeHalf = sizeof(decimal) / 2;
            }
            else throw new NotFiniteNumberException($"type {typeof(T)} not supported for binary writing.");

            Buffer.BlockCopy(values, 0, buffer, 0, byteCount);
            // swap bytes here depending on type size
            for (int i = 0; i < byteCount; i += typeSize)
            {
                for (int j = 0; j < typeSizeHalf; j++)
                {
                    byte tmp = buffer[i + j];
                    buffer[i + j] = buffer[(i + typeSize) - 1 - j];
                    buffer[(i + typeSize) - 1 - j] = tmp;
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public override void Write(ulong value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(uint value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(ushort value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(short value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(int value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(long value) => base.Write(EndianUtilities.Swap(value));

        public unsafe void Write(Half value)
        {
            var byteCount = 2;
            Half* p = &value;
            short fconv = *(short*)&p[0];
            fconv = EndianUtilities.Swap(fconv);
            // fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);                // Endianess conversion
            byte* bytes = (byte*)&fconv;
            buffer[0] = bytes[0];
            buffer[1] = bytes[1];
            BaseStream.Write(buffer, 0, byteCount);
        }

        public override void Write(float value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(double value) => base.Write(EndianUtilities.Swap(value));

        public override void Write(decimal value) => base.Write(EndianUtilities.Swap(value));

        public void Write<T>(T[] values)
        {
            this.Write(values, values.Length);
        }

        public void Write(ulong[] values)
        {
            int byteCount = values.Length * 8;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteUInt64BigEndian(span.Slice(i * 8), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(uint[] values)
        {
            int byteCount = values.Length * 4;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteUInt32BigEndian(span.Slice(i * 4), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(ushort[] values)
        {
            int byteCount = values.Length * 2;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(i * 2), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(short[] values)
        {
            int byteCount = values.Length * 2;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteInt16BigEndian(span.Slice(i * 2), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(int[] values)
        {
            int byteCount = values.Length * 4;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteInt32BigEndian(span.Slice(i * 4), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(long[] values)
        {
            int byteCount = values.Length * 8;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteInt64BigEndian(span.Slice(i * 8), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(string[] values)
        {
            foreach (var val in values)
                base.Write(val);
        }

        public void Write(Half[] values)
        {
            int byteCount = values.Length * 2;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteHalfBigEndian(span.Slice(i * 2), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(float[] values)
        {
            int byteCount = values.Length * 4;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteSingleBigEndian(span.Slice(i * 4), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(sbyte[] values)
        {
            var byteCount = values.Length;
            fixed (sbyte* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write(double[] values)
        {
            int byteCount = values.Length * 8;
            var span = buffer.AsSpan(0, byteCount);
            for (int i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteDoubleBigEndian(span.Slice(i * 8), values[i]);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(decimal[] values)
        {
            var byteCount = values.Length * sizeof(decimal);
            fixed (decimal* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                {
                    buffer[i] = ((byte*)p)[i];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(bool[] values)
        {
            var byteCount = values.Length;
            fixed (bool* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void WriteIbm(float value)
        {
            // IEEE -> IBM bits, then reverse to big-endian on the wire.
            int fconv = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(IbmFloat.IeeeBitsToIbmBits(*(int*)&value));
            base.Write(fconv);
        }

        public unsafe void WriteIbm(float[] values)
        {
            int n = values.Length;
            fixed (float* pbuffer = values)
            {
                for (int i = 0; i < n; ++i)
                {
                    int fconv = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(IbmFloat.IeeeBitsToIbmBits(*(int*)&pbuffer[i]));
                    var bytes = (byte*)&fconv;
                    int iByte = i * 4;
                    buffer[iByte + 0] = bytes[0];
                    buffer[iByte + 1] = bytes[1];
                    buffer[iByte + 2] = bytes[2];
                    buffer[iByte + 3] = bytes[3];
                }
            }
            BaseStream.Write(buffer, 0, n * 4);
        }
    }
}