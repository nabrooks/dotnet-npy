using System.Runtime.InteropServices;

namespace DotNetNpyIo
{
    public class IbmBigEndianBitConverter : BigEndianBitConverter
    {
        private static unsafe void IbmToIeeeStatic(float[] fromFloats, float[] toFloats)
        {
            int n = fromFloats.Length;
            fixed (float* fromFloatsPtr = fromFloats)
            {
                fixed (float* toFloatsPtr = toFloats)
                {
                    var from = (int*)fromFloatsPtr;
                    var to = (int*)toFloatsPtr;

                    int fconv;
                    int fmant;
                    int i;
                    int t;
                    for (i = 0; i < n; ++i)
                    {
                        fconv = from[i];
                        fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);
                        if (fconv != 0)
                        {
                            fmant = 0x00ffffff & fconv;
                            t = (int)((0x7f000000 & fconv) >> 22) - 130;
                            while ((fmant & 0x00800000) == 0) { --t; fmant <<= 1; }
                            if (t > 254) fconv = (int)((0x80000000 & fconv) | 0x7f7fffff);
                            else if (t <= 0) fconv = 0;
                            else fconv = unchecked((int)(0x80000000 & fconv)) | (t << 23) | (0x007fffff & fmant);
                        }
                        to[i] = fconv;
                    }
                }
            }
        }

        public static unsafe float[] IbmBytesToIeeeFloats(byte[] fromBytes, int startIndex, int singlesCount)
        {
            float[] result = new float[singlesCount];
            Buffer.BlockCopy(fromBytes, startIndex, result, 0, singlesCount * 4);

            fixed (float* fromFloatsPtr = result)
            {
                fixed (float* toFloatsPtr = result)
                {
                    var from = (int*)fromFloatsPtr;
                    var to = (int*)toFloatsPtr;

                    int fconv;
                    int fmant;
                    int i;
                    int t;
                    for (i = 0; i < singlesCount; ++i)
                    {
                        fconv = from[i];
                        fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);
                        if (fconv != 0)
                        {
                            fmant = 0x00ffffff & fconv;
                            t = (int)((0x7f000000 & fconv) >> 22) - 130;
                            while ((fmant & 0x00800000) == 0) { --t; fmant <<= 1; }
                            if (t > 254) fconv = (int)((0x80000000 & fconv) | 0x7f7fffff);
                            else if (t <= 0) fconv = 0;
                            else fconv = unchecked((int)(0x80000000 & fconv)) | (t << 23) | (0x007fffff & fmant);
                        }
                        to[i] = fconv;
                    }
                }
            }
            return result;
        }

        public void IbmToIeeeMethod(float[] from, float[] to)
        {
            IbmConverter.ibm_to_float(from, to, from.Length);
        }

        public override float[] ToSingles(byte[] values, int startIndex, int singlesCount)
        {
            return IbmBytesToIeeeFloats(values, startIndex, singlesCount);
        }

        public override float Int32BitsToSingle(int value)
        {
            int fmant;
            int t;
            int fconv = value;
            fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);
            if (fconv != 0)
            {
                fmant = 0x00ffffff & fconv;
                t = (int)((0x7f000000 & fconv) >> 22) - 130;
                while ((fmant & 0x00800000) == 0) { --t; fmant <<= 1; }
                if (t > 254) fconv = (int)((0x80000000 & fconv) | 0x7f7fffff);
                else if (t <= 0) fconv = 0;
                else fconv = unchecked((int)(0x80000000 & fconv)) | (t << 23) | (0x007fffff & fmant);
            }
            int to = fconv;
            return new Int32SingleUnion(to).AsSingle;
        }

        public override int SingleToInt32Bits(float value)
        {
            int fmant;
            int t;
            int fconv = new Int32SingleUnion(value).AsInt32;
            if (fconv != 0)
            {
                fmant = (0x007fffff & fconv) | 0x00800000;
                t = ((0x7f800000 & fconv) >> 23) - 126;
                while ((t & 0x3) != 0) { ++t; fmant >>= 1; }
                fconv = (int)(0x80000000 & fconv) | (((t >> 2) + 64) << 24) | fmant;
            }
            fconv = (fconv << 24) | ((fconv >> 24) & 0xff) | ((fconv & 0xff00) << 8) | ((fconv & 0xff0000) >> 8);
            int to = fconv;
            return to;
        }


        #region Private struct used for Single/Int32 conversions
        /// <summary>
        /// Union used solely for the equivalent of DoubleToInt64Bits and vice versa.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct Int32SingleUnion
        {
            /// <summary>
            /// Int32 version of the value.
            /// </summary>
            [FieldOffset(0)]
            int i;
            /// <summary>
            /// Single version of the value.
            /// </summary>
            [FieldOffset(0)]
            float f;

            /// <summary>
            /// Creates an instance representing the given integer.
            /// </summary>
            /// <param name="i">The integer value of the new instance.</param>
            internal Int32SingleUnion(int i)
            {
                this.f = 0; // Just to keep the compiler happy
                this.i = i;
            }

            /// <summary>
            /// Creates an instance representing the given floating point number.
            /// </summary>
            /// <param name="f">The floating point value of the new instance.</param>
            internal Int32SingleUnion(float f)
            {
                this.i = 0; // Just to keep the compiler happy
                this.f = f;
            }

            /// <summary>
            /// Returns the value of the instance as an integer.
            /// </summary>
            internal int AsInt32
            {
                get { return i; }
            }

            /// <summary>
            /// Returns the value of the instance as a floating point number.
            /// </summary>
            internal float AsSingle
            {
                get { return f; }
            }
        }
        #endregion
    }
}