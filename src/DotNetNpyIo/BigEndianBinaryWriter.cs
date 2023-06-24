namespace DotNetNpyIo
{
    public class BigEndianBinaryWriter : BinaryWriter, IBinaryWriter
    {
        byte[] buffer = new byte[BaseTwoPower.TwentyFour];

        public BigEndianBinaryWriter(Stream stream) : base(stream) { }

        public void Write<T>(T value)
        {
            var tType = typeof(T);
            if (tType == typeof(bool))
                Write((bool)(object)value);
            else if (tType == typeof(byte))
                Write((byte)(object)value);
            else if (tType == typeof(sbyte))
                Write((sbyte)(object)value);

            else if (tType == typeof(short))
                Write((short)(object)value);
            else if (tType == typeof(ushort))
                Write((ushort)(object)value);
            else if (tType == typeof(int))
                Write((int)(object)value);
            else if (tType == typeof(uint))
                Write((uint)(object)value);
            else if (tType == typeof(long))
                Write((long)(object)value);
            else if (tType == typeof(ulong))
                Write((ulong)(object)value);

            else if (tType == typeof(Half))
                Write((Half)(object)value);
            else if (tType == typeof(float))
                Write((float)(object)value);
            else if (tType == typeof(double))
                Write((double)(object)value);
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

        public unsafe void Write(ulong[] values)
        {
            var byteCount = values.Length * 8;
            fixed (ulong* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ulong bigEValue = EndianUtilities.Swap(p[i]);
                    var bytes = (byte*)&bigEValue;
                    buffer[i * 8 + 0] = bytes[0];
                    buffer[i * 8 + 1] = bytes[1];
                    buffer[i * 8 + 2] = bytes[2];
                    buffer[i * 8 + 3] = bytes[3];
                    buffer[i * 8 + 4] = bytes[4];
                    buffer[i * 8 + 5] = bytes[5];
                    buffer[i * 8 + 6] = bytes[6];
                    buffer[i * 8 + 7] = bytes[7];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(uint[] values)
        {
            var byteCount = values.Length * 4;
            fixed (uint* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    uint bigEValue = EndianUtilities.Swap(p[i]);
                    var bytes = (byte*)&bigEValue;
                    buffer[i * 4 + 0] = bytes[0];
                    buffer[i * 4 + 1] = bytes[1];
                    buffer[i * 4 + 2] = bytes[2];
                    buffer[i * 4 + 3] = bytes[3];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(ushort[] values)
        {
            var byteCount = values.Length * 2;
            fixed (ushort* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ushort bigEValue = EndianUtilities.Swap(p[i]);
                    var bytes = (byte*)&bigEValue;
                    buffer[i * 2 + 0] = bytes[0];
                    buffer[i * 2 + 1] = bytes[1];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(short[] values)
        {
            var byteCount = values.Length * 2;
            fixed (short* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    short bigEValue = EndianUtilities.Swap(p[i]);
                    var bytes = (byte*)&bigEValue;
                    buffer[i * 2 + 0] = bytes[0];
                    buffer[i * 2 + 1] = bytes[1];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(int[] values)
        {
            var byteCount = values.Length * 4;
            fixed (int* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    int bigEValue = EndianUtilities.Swap(p[i]);
                    var bytes = (byte*)&bigEValue;
                    buffer[i * 4 + 0] = bytes[0];
                    buffer[i * 4 + 1] = bytes[1];
                    buffer[i * 4 + 2] = bytes[2];
                    buffer[i * 4 + 3] = bytes[3];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(long[] values)
        {
            var byteCount = values.Length * 8;
            fixed (long* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    long bigEValue = EndianUtilities.Swap(p[i]);
                    var bytes = (byte*)&bigEValue;
                    buffer[i * 8 + 0] = bytes[0];
                    buffer[i * 8 + 1] = bytes[1];
                    buffer[i * 8 + 2] = bytes[2];
                    buffer[i * 8 + 3] = bytes[3];
                    buffer[i * 8 + 4] = bytes[4];
                    buffer[i * 8 + 5] = bytes[5];
                    buffer[i * 8 + 6] = bytes[6];
                    buffer[i * 8 + 7] = bytes[7];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(string[] values)
        {
            foreach (var val in values)
                base.Write(val);
        }

        public unsafe void Write(Half[] values)
        {
            var byteCount = values.Length * 2;
            fixed (Half* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    short fconv = *(short*)&p[i];
                    fconv = EndianUtilities.Swap(fconv);
                    // fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);                // Endianess conversion
                    byte* bytes = (byte*)&fconv;
                    buffer[i * 2 + 0] = bytes[0];
                    buffer[i * 2 + 1] = bytes[1];
                }
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(float[] values)
        {
            var byteCount = values.Length * 4;
            fixed (float* p = values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    int fconv = *(int*)&p[i];
                    fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);                // Endianess conversion
                    byte* bytes = (byte*)&fconv;
                    buffer[i * 4 + 0] = bytes[0];
                    buffer[i * 4 + 1] = bytes[1];
                    buffer[i * 4 + 2] = bytes[2];
                    buffer[i * 4 + 3] = bytes[3];
                }
            }
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

        public unsafe void Write(double[] values)
        {
            var byteCount = values.Length * 8;
            fixed (double* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                {
                    var r = *(long*)&p[i];
                    var bigEValue = EndianUtilities.Swap(r);
                    byte* bytes = (byte*)&bigEValue;
                    buffer[i * 8 + 0] = bytes[0];
                    buffer[i * 8 + 1] = bytes[1];
                    buffer[i * 8 + 2] = bytes[2];
                    buffer[i * 8 + 3] = bytes[3];
                    buffer[i * 8 + 4] = bytes[4];
                    buffer[i * 8 + 5] = bytes[5];
                    buffer[i * 8 + 6] = bytes[6];
                    buffer[i * 8 + 7] = bytes[7];
                }
            }
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
            int fconv;
            int fmant;
            int t;
            fconv = *(int*)&value;
            if (fconv != 0)
            {
                fmant = (0x007fffff & fconv) | 0x00800000;
                t = ((0x7f800000 & fconv) >> 23) - 126;
                while ((t & 0x3) != 0) { ++t; fmant >>= 1; }
                fconv = (int)(0x80000000 & fconv) | (((t >> 2) + 64) << 24) | fmant;
            }
            fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);                // Endianess conversion
            var to = fconv;
            base.Write(to);
        }

        public unsafe void WriteIbm(float[] values)
        {
            int n = values.Length;
            int fconv;
            int fmant;
            int i;
            int t;
            fixed (float* pbuffer = values)
            {
                for (i = 0; i < n; ++i)
                {
                    int iByte = i * 4;
                    fconv = *(int*)&pbuffer[i];
                    if (fconv != 0)
                    {
                        fmant = (0x007fffff & fconv) | 0x00800000;
                        t = ((0x7f800000 & fconv) >> 23) - 126;
                        while ((t & 0x3) != 0) { ++t; fmant >>= 1; }
                        fconv = (int)(0x80000000 & fconv) | (((t >> 2) + 64) << 24) | fmant;
                    }
                    fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);         // Endianess conversion
                    var bytes = (byte*)&fconv;
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