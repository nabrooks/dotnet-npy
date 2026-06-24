namespace DotNetNpyIo
{
    public class LittleEndianBinaryWriter : BinaryWriter, IBinaryWriter
    {
        byte[] buffer = new byte[BaseTwoPower.TwentyFour];// new byte[16];

        Dictionary<Type, int> typeSizeMap = new Dictionary<Type, int>()
        {
            { typeof(bool), sizeof(bool) },
            { typeof(byte), sizeof(byte) },
            { typeof(sbyte), sizeof(sbyte) },
            { typeof(short), sizeof(short) },
            { typeof(ushort), sizeof(ushort) },
            { typeof(int), sizeof(int) },
            { typeof(uint), sizeof(uint) },
            { typeof(long), sizeof(long) },
            { typeof(ulong), sizeof(ulong) },
            { typeof(Half), sizeof(float)/2 },
            { typeof(float), sizeof(float) },
            { typeof(double), sizeof(double) },
        };

        public LittleEndianBinaryWriter(Stream stream) : base(stream) { }

        public void Write<T>(T value)
        {
            var tType = typeof(T);
            if (tType == typeof(bool))
                base.Write((bool)(object)value);
            else if (tType == typeof(byte))
                base.Write((byte)(object)value);
            else if (tType == typeof(sbyte))
                base.Write((sbyte)(object)value);

            else if (tType == typeof(short))
                base.Write((short)(object)value);
            else if (tType == typeof(ushort))
                base.Write((ushort)(object)value);
            else if (tType == typeof(int))
                base.Write((int)(object)value);
            else if (tType == typeof(uint))
                base.Write((uint)(object)value);
            else if (tType == typeof(long))
                base.Write((long)(object)value);
            else if (tType == typeof(ulong))
                base.Write((ulong)(object)value);

            else if (tType == typeof(Half))
                // Use base.Write (unambiguous) — calling Write(Half) here would re-bind to
                // this generic method and recurse infinitely. Half bits written raw (LE host).
                base.Write(BitConverter.HalfToInt16Bits((Half)(object)value));
            else if (tType == typeof(float))
                base.Write((float)(object)value);
            else if (tType == typeof(double))
                base.Write((double)(object)value);
            else
                throw new NotSupportedException("Type not supported");
        }

        public void Write<T>(T[] values, int count)
        {
            if (count > values.Length)
                throw new ArgumentException($"There arent {count} elements in the array to write to file: array length {values.Length}");

            var tType = typeof(T);
            int byteCount = count * typeSizeMap[tType];

            Buffer.BlockCopy(values, 0, buffer, 0, byteCount);
            BaseStream.Write(buffer, 0, byteCount);
        }

        public void Write<T>(T[] values)
        {
            Write(values, values.Length);
        }

        public unsafe void Write(ulong[] values)
        {
            var byteCount = values.Length * 8;
            fixed (ulong* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(uint[] values)
        {
            var byteCount = values.Length * 4;
            fixed (uint* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(ushort[] values)
        {
            var byteCount = values.Length * 2;
            fixed (ushort* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(short[] values)
        {
            var byteCount = values.Length * 2;
            fixed (short* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(int[] values)
        {
            var byteCount = values.Length * 4;
            fixed (int* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(long[] values)
        {
            var byteCount = values.Length * 8;
            fixed (long* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
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
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(float[] values)
        {
            var byteCount = values.Length * 4;


            //fixed (float* p = values)
            //{
            //for (int i = 0; i < byteCount; i++)
            //buffer[i] = ((byte*)p)[i];
            //}
            Buffer.BlockCopy(values, 0, buffer, 0, byteCount);
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
                    buffer[i] = ((byte*)p)[i];
            }
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void Write(decimal[] values)
        {
            var byteCount = values.Length * sizeof(decimal);
            fixed (decimal* p = values)
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = ((byte*)p)[i];
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

        public unsafe void Write(Half value)
        {
            var byteCount = 2;
            Half* i = &value;
            buffer[0] = ((byte*)i)[0];
            buffer[1] = ((byte*)i)[1];
            BaseStream.Write(buffer, 0, byteCount);
        }

        public unsafe void WriteIbm(float value)
        {
            // IEEE -> IBM bits; little-endian writer emits native byte order (no reversal).
            int fconv = IbmFloat.IeeeBitsToIbmBits(*((int*)&value));
            base.Write(fconv);
        }

        public unsafe void WriteIbm(float[] values)
        {
            int n = values.Length;
            fixed (float* pbuffer = values)
            {
                for (int i = 0; i < n; ++i)
                {
                    int fconv = IbmFloat.IeeeBitsToIbmBits(*(int*)&pbuffer[i]);
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
