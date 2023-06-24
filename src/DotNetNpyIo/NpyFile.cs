using System.IO;
using System.Runtime.InteropServices;
using System;

namespace DotNetNpyIo
{    
    /// <summary>
    /// Interface and partial implementation of a numpy file, refer to <see cref="NpyFileMemmap{T}"/> for 
    /// Generic implementation details and serialization details.
    /// </summary>
    public abstract class NpyFile : Disposable
    {
        //                                                     '?'  'N' 'U' 'M' 'P' 'Y'
        protected static byte[] MagicStringBytes = new byte[] { 147, 78, 85, 77, 80, 89 };

        protected int headerSize;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="stream">The file stream</param>
        /// <param name="isFortranOrder">Array ordering in the file</param>
        /// <param name="dataType">The data type to serialize</param>
        /// <param name="isLittleEndian">Byte ordering</param>
        /// <param name="shape">The dimensionality and length of array</param>
        /// <param name="dataStartIndex">the starting index of the data, should be a multiple of 64 for efficent reading/writing, but should consider </param>
        protected NpyFile(FileInfo fileInfo, bool isFortranOrder, Type dataType, bool isLittleEndian, int[] shape, int headerSize)
        {
            this.FileInfo = fileInfo;
            this.Endianess = isLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;
            this.IsFortranOrder = isFortranOrder;
            this.DataType = dataType;
            this.Shape = shape;
            this.SizeOfType = Marshal.SizeOf(dataType);
            this.SampleCount = this.Shape.Aggregate<int, long>(1, (x, y) => x * y);

            this.headerSize = headerSize;
        }

        /// <summary>
        /// The <see cref="FileInfo"/> of the numpy file
        /// </summary>
        public FileInfo FileInfo { get; }

        /// <summary>
        /// if k is 'slowest' then true. if k is 'fastest' then false
        /// </summary>
        public bool IsFortranOrder { get; }

        /// <summary>
        /// The byte ordering of the file's sample data
        /// </summary>
        public Endianness Endianess { get; }

        /// <summary>
        /// The data type of the samples in the array
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// The size of the data type of the samples in the array
        /// </summary>
        public int SizeOfType { get; }

        /// <summary>
        /// The number of samples in the array
        /// </summary>
        public long SampleCount { get; }

        /// <summary>
        /// The dimenionality of the array (1d, 2d, 3d, 4d, 5d, etc...)
        /// </summary>
        public int Dimensionality => Shape.Length;

        /// <summary>
        /// The size or length of each dimension (number of elements along each dimension's axis)
        /// </summary>
        public int[] Shape { get; }

        /// <summary>
        /// The major version number of this file format
        /// </summary>
        public byte MajorVersionNumber { get; protected set; }

        /// <summary>
        /// The minor version number of this file format
        /// </summary>
        public byte MinorVersionNumber { get; protected set; }

        /// <summary>
        /// The numpy file format version number
        /// </summary>
        public float VersionNumber => (float)MajorVersionNumber + ((float)MinorVersionNumber / 10);

        /// <summary>
        /// Gets a type based on a parsed character and word size from the header of the file
        /// </summary>
        /// <param name="type">A char representing data type</param>
        /// <param name="wordSize">The number of bytes representing a single instance of the data type</param>
        /// <returns>The type represented in the file array</returns>
        protected static Type GetType(char type, int wordSize)
        {
            switch (type)
            {
                case '?': return typeof(bool);
                case 'B': return typeof(byte);
                case 'b': return typeof(sbyte);
                case 'u':
                    {
                        if (wordSize == 1) return typeof(byte);
                        if (wordSize == 2) return typeof(ushort);
                        if (wordSize == 4) return typeof(UInt32);
                        else if (wordSize == 8) return typeof(UInt64);
                        else throw new Exception($"Unsigned Integer type of size {wordSize} is not supported.");
                    }
                case 'i':
                    {
                        if (wordSize == 1) return typeof(sbyte);
                        if (wordSize == 2) return typeof(short);
                        if (wordSize == 4) return typeof(Int32);
                        else if (wordSize == 8) return typeof(Int64);
                        else throw new Exception($"Integer type of size {wordSize} is not supported.");
                    }
                case 'f':
                    {
                        if (wordSize == 2) return typeof(Half);
                        if (wordSize == 4) return typeof(float);
                        else if (wordSize == 8) return typeof(double);
                        else throw new Exception($"Float type of size {wordSize} is not supported.");
                    }
                default:
                    throw new Exception($"Type with first character '{type}' is not yet supported");
            }
        }

        /// <summary>
        /// Gets a type based on a parsed character and word size from the header of the file
        /// </summary>
        /// <param name="type">A char representing data type</param>
        /// <param name="wordSize">The number of bytes representing a single instance of the data type</param>
        /// <returns>The type represented in the file array</returns>
        protected static Tuple<bool, Type> GetType(string arrayProtocolTypeString)
        {
            bool isLittleEndian = true;
            if (arrayProtocolTypeString[0] == '<' || arrayProtocolTypeString[0] == '|') isLittleEndian = true;
            if (arrayProtocolTypeString[0] == '>') isLittleEndian = false;

            if (arrayProtocolTypeString.Contains("?")) return new Tuple<bool, Type>(isLittleEndian, typeof(bool));
            else if (arrayProtocolTypeString.Contains("b")) return new Tuple<bool, Type>(isLittleEndian, typeof(sbyte));
            else if (arrayProtocolTypeString.Contains("B")) return new Tuple<bool, Type>(isLittleEndian, typeof(byte));

            else if (arrayProtocolTypeString.Contains("i2")) return new Tuple<bool, Type>(isLittleEndian, typeof(Int16));
            else if (arrayProtocolTypeString.Contains("i4")) return new Tuple<bool, Type>(isLittleEndian, typeof(Int32));
            else if (arrayProtocolTypeString.Contains("i8")) return new Tuple<bool, Type>(isLittleEndian, typeof(Int64));

            else if (arrayProtocolTypeString.Contains("u2")) return new Tuple<bool, Type>(isLittleEndian, typeof(UInt16));
            else if (arrayProtocolTypeString.Contains("u4")) return new Tuple<bool, Type>(isLittleEndian, typeof(UInt32));
            else if (arrayProtocolTypeString.Contains("u8")) return new Tuple<bool, Type>(isLittleEndian, typeof(UInt64));

            else if (arrayProtocolTypeString.Contains("f2")) return new Tuple<bool, Type>(isLittleEndian, typeof(Half));
            else if (arrayProtocolTypeString.Contains("f4")) return new Tuple<bool, Type>(isLittleEndian, typeof(Single));
            else if (arrayProtocolTypeString.Contains("f8")) return new Tuple<bool, Type>(isLittleEndian, typeof(Double));
            else throw new Exception($"Type with array protocol type string value '{arrayProtocolTypeString}' is not yet supported");
        }

        /// <summary>
        /// Gets a header string value for a data type (used for writing files)
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>A string value</returns>
        protected static string GetTypeString(Type type)
        {
            if (type == typeof(bool)) return "?";
            else if (type == typeof(sbyte)) return "b";
            else if (type == typeof(byte)) return "B";
            else if (type == typeof(float)) return "f4";
            else if (type == typeof(double)) return "f8";
            else if (type == typeof(short)) return "i2";
            else if (type == typeof(int)) return "i4";
            else if (type == typeof(long)) return "i8";
            else if (type == typeof(ushort)) return "u2";
            else if (type == typeof(uint)) return "u4";
            else if (type == typeof(ulong)) return "u8";
            else throw new Exception($"Type {type} is not supported");
        }

        /// <summary>
        /// Validates that type <see cref="{T}"/> is a valid type for this file format
        /// </summary>
        /// <typeparam name="T">The data type intended to serialize</typeparam>
        /// <returns>True if valid, otherwise false</returns>
        public static bool Validate<T>()
        {
            if (typeof(T) == typeof(bool)) return true;
            else if (typeof(T) == typeof(sbyte)) return true;
            else if (typeof(T) == typeof(byte)) return true;
            else if (typeof(T) == typeof(float)) return true;
            else if (typeof(T) == typeof(double)) return true;
            else if (typeof(T) == typeof(short)) return true;
            else if (typeof(T) == typeof(int)) return true;
            else if (typeof(T) == typeof(long)) return true;
            else if (typeof(T) == typeof(ushort)) return true;
            else if (typeof(T) == typeof(uint)) return true;
            else if (typeof(T) == typeof(ulong)) return true;
            else return false;
        }

        protected static void TryDontThrow(Action action)
        {
            try
            {
                action();
            }
            catch
            {

            }
        }
    }
}