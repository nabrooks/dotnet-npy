using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace DotNetNpyIo
{
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct IbmSingle : IComparable, IFormattable, IConvertible, IComparable<IbmSingle>, IEquatable<IbmSingle>
    {
        internal float m_value;

        public static IbmSingle MinValue = new IbmSingle((float)-3.40282346638528859e+38);
        public const float Epsilon = (float)1.4e-45;
        public const float MaxValue = (float)3.40282346638528859e+38;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;

        private unsafe IbmSingle(float ieeeFloat)
        {
            int fmant;
            int t;
            int fconv = *(int*)&ieeeFloat;
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
            m_value = *(float*)&fconv;
        }

        public static unsafe IbmSingle FromIeee(float value)
        {
            return new IbmSingle(value);
        }

        public float ToIeeeSingle()
        {
            throw new NotImplementedException();
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static bool IsInfinity(IbmSingle f)
        {
            return (*(int*)(&f.m_value) & 0x7FFFFFFF) == 0x7F800000;
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static bool IsPositiveInfinity(IbmSingle f)
        {
            return *(int*)(&f.m_value) == 0x7F800000;
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static bool IsNegativeInfinity(IbmSingle f)
        {
            return *(int*)(&f.m_value) == unchecked((int)0xFF800000);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]
        public unsafe static bool IsNaN(IbmSingle f)
        {
            return (*(int*)(&f.m_value) & 0x7FFFFFFF) > 0x7F800000;
        }

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Single, this method throws an ArgumentException.
        //
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (value is Single)
            {
                IbmSingle ibmSingle = new IbmSingle((float)value);
                float f = (float)ibmSingle.m_value;
                if (m_value < f) return -1;
                if (m_value > f) return 1;
                if (m_value == f) return 0;

                // At least one of the values is NaN.
                if (float.IsNaN(m_value))
                    return (float.IsNaN(f) ? 0 : -1);
                else // f is NaN.
                    return 1;
            }
            throw new ArgumentException("Arg_MustBeSingle");
        }

        public int CompareTo(IbmSingle value)
        {
            float f = (float)value.m_value;
            if (m_value < value.m_value) return -1;
            if (m_value > value.m_value) return 1;
            if (m_value == value.m_value) return 0;

            // At least one of the values is NaN.
            if (float.IsNaN(m_value))
                return (float.IsNaN(value.m_value) ? 0 : -1);
            else // f is NaN.
                return 1;
        }

        public int CompareTo(Single value)
        {
            IbmSingle ibmSingle = new IbmSingle((float)value);
            float f = (float)ibmSingle.m_value;
            if (m_value < ibmSingle.m_value) return -1;
            if (m_value > ibmSingle.m_value) return 1;
            if (m_value == ibmSingle.m_value) return 0;

            // At least one of the values is NaN.
            if (float.IsNaN(m_value))
                return (float.IsNaN(ibmSingle.m_value) ? 0 : -1);
            else // f is NaN.
                return 1;
        }

        public static bool operator ==(IbmSingle left, IbmSingle right)
        {
            return left == right;
        }

        public static bool operator !=(IbmSingle left, IbmSingle right)
        {
            return left != right;
        }

        public static bool operator <(IbmSingle left, IbmSingle right)
        {
            return left < right;
        }

        public static bool operator >(IbmSingle left, IbmSingle right)
        {
            return left > right;
        }

        public static bool operator <=(IbmSingle left, IbmSingle right)
        {
            return left <= right;
        }

        public static bool operator >=(IbmSingle left, IbmSingle right)
        {
            return left >= right;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is IbmSingle))
            {
                return false;
            }
            float temp = ((IbmSingle)obj).m_value;
            if (temp == m_value)
            {
                return true;
            }

            return float.IsNaN(temp) && float.IsNaN(m_value);
        }

        public bool Equals(IbmSingle obj)
        {
            if (obj.m_value == m_value)
            {
                return true;
            }

            return float.IsNaN(obj.m_value) && float.IsNaN(m_value);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override int GetHashCode()
        {
            float f = m_value;
            if (f == 0)
            {
                // Ensure that 0 and -0 have the same hash code
                return 0;
            }
            int v = *(int*)(&f);
            return v;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return m_value.ToString();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return m_value.ToString(provider);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return m_value.ToString(format);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return m_value.ToString(format, provider);
        }
        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Single;
        }


        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(m_value);
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException("InvalidCast_FromTo Single Char");
        }

        /// <internalonly/>
        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(m_value);
        }

        /// <internalonly/>
        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(m_value);
        }

        /// <internalonly/>
        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(m_value);
        }

        /// <internalonly/>
        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(m_value);
        }

        /// <internalonly/>
        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(m_value);
        }

        /// <internalonly/>
        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(m_value);
        }

        /// <internalonly/>
        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(m_value);
        }

        /// <internalonly/>
        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(m_value);
        }

        /// <internalonly/>
        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return m_value;
        }

        /// <internalonly/>
        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(m_value);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException("InvalidCast_FromTo Single DateTime");
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.ChangeType(m_value, type, provider);
        }
    }
}
