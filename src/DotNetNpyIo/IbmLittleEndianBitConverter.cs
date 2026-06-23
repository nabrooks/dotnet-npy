using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace DotNetNpyIo
{
    public class IbmLittleEndianBitConverter : LittleEndianBitConverter
    {
        public override float Int32BitsToSingle(int value)
        {
            int to = IbmFloat.IbmBitsToIeeeBits(BinaryPrimitives.ReverseEndianness(value));
            return new Int32SingleUnion(to).AsSingle;
        }

        public override int SingleToInt32Bits(float value)
        {
            return BinaryPrimitives.ReverseEndianness(IbmFloat.IeeeBitsToIbmBits(new Int32SingleUnion(value).AsInt32));
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
