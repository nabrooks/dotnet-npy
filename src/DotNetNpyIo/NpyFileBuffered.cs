using System.Runtime.InteropServices;
using System.Text;

namespace DotNetNpyIo
{
    /// <summary>
    /// A numpy file handler, handles reading and writing of numpy files.
    /// </summary>
    /// <typeparam name="T">The type of data to serialize into an array for storage in this file type</typeparam>
    public sealed class NpyFileBuffered<T> : NpyFile where T : unmanaged
    {
        private const int maxBufferSize = BaseTwoPower.TwentyOne; // buffer size must be at greatest 1/8 of the buffer size of both the binary reader and writer used
        private readonly IBinaryReader binaryReader;
        private readonly IBinaryWriter binaryWriter;
        private readonly Stream stream;

        /// <summary>
        /// Read and write functions, resets the buffer start position,
        /// Seeks to appropriate file position, then reads bytes from the
        /// appropriate binary reader
        /// </summary>
        private readonly Action<T[], long, int> _readFillBuff;
        private readonly Action<T[], long, int> _writeBuff;

        /// <summary>
        /// Ravel funcitons
        /// </summary>
        private readonly Func<int, int, long> _ravel2D;
        private readonly Func<int, int, int, long> _ravel3D;
        private readonly Func<int, int, int, int, long> _ravel4D;
        private readonly Func<int[], long> _ravelND;

        /// <summary>
        /// In memory data buffer, for super fast read and write operations
        /// </summary>
        private readonly T[] _buffer = new T[maxBufferSize];
        /// <summary>
        /// The array element position in the file to which the buffer's first element refers
        /// </summary>
        private long _bufferStartPosition = 0;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="stream">The file stream</param>
        /// <param name="isFortranOrder">Array ordering in the file</param>
        /// <param name="dataType">The data type to serialize</param>
        /// <param name="isLittleEndian">Byte ordering</param>
        /// <param name="shape">The dimensionality and length of array</param>
        /// <param name="headerSize">the starting index of the data, should be a multiple of 64 for efficent reading/writing, but should consider </param>
        private NpyFileBuffered(FileInfo fileInfo, bool isFortranOrder, bool isLittleEndian, int[] shape, int headerSize) : base(fileInfo, isFortranOrder, typeof(T), isLittleEndian, shape, headerSize)
        {
            stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            binaryReader = isLittleEndian ? (IBinaryReader)new LittleEndianBinaryReader(stream) : (IBinaryReader)new BigEndianBinaryReader(stream);
            binaryWriter = isLittleEndian ? (IBinaryWriter)new LittleEndianBinaryWriter(stream) : (IBinaryWriter)new BigEndianBinaryWriter(stream);

            _readFillBuff = (buffer, position, count) =>
            {
                _bufferStartPosition = position;
                stream.Seek(this.headerSize + (position * SizeOfType), SeekOrigin.Begin);
                binaryReader.ReadMany<T>(ref buffer, count);
            };

            _writeBuff = (buffer, position, count) =>
            {
                _bufferStartPosition = position;
                stream.Seek(this.headerSize + (position * SizeOfType), SeekOrigin.Begin);
                binaryWriter.Write(buffer, count);
            };

            long ni = shape.Length >= 1 ? shape[0] : 0;
            long nj = shape.Length >= 2 ? shape[1] : 0;
            long nk = shape.Length >= 3 ? shape[2] : 0;
            long nl = shape.Length >= 4 ? shape[3] : 0;

            long njni = ni * nj;
            long njnk = nj * nk;
            long nknl = nk * nl;
            long nknjni = ni * nj * nk;
            long njnknl = nj * nk * nl;

            _ravel2D = IsFortranOrder ?
                (int0, int1) => (int1 * ni) + int0 :
                (int0, int1) => (int0 * nj) + int1;

            _ravel3D = IsFortranOrder ?
              (int0, int1, int2) => (int2 * njni) + (int1 * ni) + int0 :
              (int0, int1, int2) => (int0 * njnk) + (int1 * nk) + int2;

            _ravel4D = IsFortranOrder ?
                (int0, int1, int2, int3) => (int3 * nknjni) + (int2 * njni) + (int1 * ni) + int0 :
                (int0, int1, int2, int3) => (int0 * njnknl) + (int1 * nknl) + (int2 * nl) + int3;

            _ravelND = IsFortranOrder ?
                (ints) =>
                {
                    long result = 0;
                    for (int indexI = ints.Length - 1; indexI >= 0; indexI--)
                    {
                        long index = ints[indexI];
                        long shapeSizeAgg = 1;
                        for (int shapeI = shape.Length - 1; shapeI >= indexI + 1; shapeI--)
                        {
                            var size = shape[shapeI];
                            shapeSizeAgg *= size;
                        }
                        var indexAgg = index * shapeSizeAgg;
                        result += indexAgg;
                    }
                    return result;
                }
            :
                (ints) =>
                {
                    long result = 0;
                    for (int indexI = 0; indexI < ints.Length; indexI++)
                    {
                        long index = ints[indexI];
                        long shapeSizeAgg = 1;
                        for (int shapeI = indexI + 1; shapeI < shape.Length; shapeI++)
                        {
                            var size = shape[shapeI];
                            shapeSizeAgg *= size;
                        }
                        var indexAgg = index * shapeSizeAgg;
                        result += indexAgg;
                    }
                    return result;
                };

            // Initial fill buffer
            if (SampleCount >= maxBufferSize)
                _readFillBuff(_buffer, _bufferStartPosition, maxBufferSize);
            else
                _readFillBuff(_buffer, _bufferStartPosition, (int)SampleCount);
        }

        /// <summary>
        /// Reads a single value at a flattened array index
        /// </summary>
        /// <param name="i">The flattened array index to read from</param>
        /// <returns>The array sample value at index <paramref name="i"/></returns>
        private T Read(long i)
        {
            TryFillFlushBuffer(i);
            return _buffer[i - _bufferStartPosition];
        }

        /// <summary>
        /// Reads all data from file into a single array.  the enumerable type implemented
        /// is a BigArray<typeparamref name="T"/> so long index access is available and 
        /// no memory limitations imposed by the runtime on objects exist, only hardware lims
        /// </summary>
        /// <param name="i">The offset of the file to start reading from</param>
        /// <param name="count">The number of object elements to read</param>
        /// <returns>A BigArray of elements <typeparamref name="T"/></returns>
        private IEnumerable<T> Read(long i, long count)
        {
            BigArray<T> result = new BigArray<T>(count);
            for (long e = 0; e < count; e++)
            {
                result[e] = Read(i + e);
            }
            return result;
        }

        /// <summary>
        /// Writes a sample value to flattened array index 'i'
        /// </summary>
        /// <param name="value">The value to write</param>
        /// <param name="i">The flattened array index to write to</param>
        private void Write(T value, long i)
        {
            TryFillFlushBuffer(i);
            _buffer[i - _bufferStartPosition] = value;
        }

        #region System.Int32 and System.Int64 based index access

        /// <summary>
        /// class indexer allows the client to provide a set of indices to access nd array element of
        /// </summary>
        /// <param name="indices">Coordinate indices of the nd array to access</param>
        /// <returns>The array sample value</returns>
        public T this[params int[] indices]
        {
            get
            {
                if (indices.Length != Shape.Length) throw new Exception("Indices.Length must be equal to Shape.Length");
                for (int i = 0; i < Shape.Length; i++)
                {
                    var shapeLength = Shape[i];
                    var shapeIndex = indices[i];
                    if (shapeIndex >= shapeLength) throw new ArgumentException($"Shape index : {i} , value : {shapeIndex} is greater than that shape dimension");
                }
                var index = _ravelND(indices);
                return Read(index);
            }
            set
            {
                if (indices.Length != Shape.Length) throw new Exception("Indices.Length must be equal to Shape.Length");
                for (int i = 0; i < Shape.Length; i++)
                {
                    var shapeLength = Shape[i];
                    var shapeIndex = indices[i];
                    if (shapeIndex >= shapeLength) throw new ArgumentException($"Shape index : {i} , value : {shapeIndex} is greater than that shape dimension");
                }
                var index = _ravelND(indices);
                Write(value, index);
            }
        }

        /// <summary>
        /// Array indexer for npy files that contain 2d arrays
        /// </summary>
        /// <param name="i">The ith element</param>
        /// <param name="j">The jth element</param>
        /// <returns>The sample value at the index coordinate</returns>
        public T this[int i, int j]
        {
            get
            {
                if (Shape.Length != 2) throw new Exception("To access the array via 2d accessor, the array must be 2 dimensional");

                var index = _ravel2D(i, j);

                if (index >= SampleCount) throw new ArgumentException($"Index [{i},{j}] is out of bounds of array.");

                return Read(index);
            }
            set
            {
                if (Shape.Length != 2) throw new Exception("To access the array via 2d accessor, the array must be 2 dimensional");

                var index = _ravel2D(i, j);

                if (index >= SampleCount) throw new ArgumentException($"Index [{i},{j}] is out of bounds of array.");

                Write(value, index);
            }
        }

        /// <summary>
        /// Array indexer for npy files that contain 3d arrays
        /// </summary>
        /// <param name="i">The ith element</param>
        /// <param name="j">The jth element</param>
        /// <param name="k">The kth element</param>
        /// <returns>The sample value at the index coordinate</returns>
        public T this[int i, int j, int k]
        {
            get
            {
                if (Shape.Length != 3) throw new Exception("To access the array via 3d accessor, the array must be 3 dimensional");

                var index = _ravel3D(i, j, k);

                if (index >= SampleCount) throw new ArgumentException($"Index [{i},{j},{k}] is out of bounds of array.");

                return Read(index);
            }
            set
            {
                if (Shape.Length != 3) throw new Exception("To set the array via 3d setter, the array must be 3 dimensional");

                var index = _ravel3D(i, j, k);

                if (index >= SampleCount) throw new ArgumentException($"Index [{i},{j},{k}] is out of bounds of array.");

                Write(value, index);
            }
        }

        /// <summary>
        /// Array indexer for npy files that contain 4d arrays
        /// </summary>
        /// <param name="i">The ith element</param>
        /// <param name="j">The jth element</param>
        /// <param name="k">The kth element</param>
        /// <param name="l">The lth element</param>
        /// <returns>The sample value at the index coordinate</returns>
        public T this[int i, int j, int k, int l]
        {
            get
            {

                if (Shape.Length != 4) throw new Exception("To access the array via 4d accessor, the array must be 4 dimensional");

                var index = _ravel4D(i, j, k, l);

                if (index >= SampleCount) throw new ArgumentException($"Index [{i},{j},{k},{l}] is out of bounds of array.");

                return Read(index);
            }
            set
            {

                if (Shape.Length != 4) throw new Exception("To access the array via 4d accessor, the array must be 4 dimensional");

                var index = _ravel4D(i, j, k, l);

                if (index >= SampleCount) throw new ArgumentException($"Index [{i},{j},{k},{l}] is out of bounds of array.");

                Write(value, index);
            }
        }

        /// <summary>
        /// Class indexer allows the client to provide a single flattened array index to access nd array element.
        /// </summary>
        /// <param name="i">The flattened array index</param>
        /// <returns></returns>
        public T this[int i]
        {
            get
            {
                if (i >= SampleCount) throw new Exception($"Index must be less than or equal to SampleCount: {SampleCount}");
                return Read(i);
            }
            set
            {
                if (i >= SampleCount) throw new Exception($"Index must be less than or equal to SampleCount: {SampleCount}");
                Write(value, i);
            }
        }

        /// <summary>
        /// Class indexer allows the client to provide a single flattened array index to access nd array element.
        /// </summary>
        /// <param name="i">The flattened array index</param>
        /// <returns></returns>
        public T this[long i]
        {
            get
            {
                if (i >= SampleCount) throw new Exception($"Index must be less than or equal to SampleCount: {SampleCount}");
                return Read(i);
            }
            set
            {
                if (i >= SampleCount) throw new Exception($"Index must be less than or equal to SampleCount: {SampleCount}");
                Write(value, i);
            }
        }

        #endregion System.Int32 and System.Int64 based index access

        #region System.Index based index access

        #region 2D to 1D slice access

        /// <summary>
        /// j slice extraction. Assumes data Shape is 2 dimensional!
        /// </summary>
        /// <param name="indexI">The I index to get j array for which data is to be extracted</param>
        /// <param name="rangeJ">The range of values j for which data is to be extracted</param>
        /// <returns>the 1d data array slice from 2d data array</returns>
        public T[] this[Index indexI, Range rangeJ]
        {
            get
            {
                Range rangeI = new Range(indexI, indexI.IsFromEnd ? indexI.Value - 1 : indexI.Value + 1);
                return this[rangeI, rangeJ][0];
            }
            set
            {
                var dummy = Jagged.Create<T>(1, value.Length);
                for (int j = 0; j < value.Length; j++)
                    dummy[0][j] = value[j];

                Range rangeI = new Range(indexI, indexI.IsFromEnd ? indexI.Value - 1 : indexI.Value + 1);
                this[rangeI, rangeJ] = dummy;
            }
        }

        /// <summary>
        /// i slice extraction. Assumes data Shape is 2 dimensional!
        /// </summary>
        /// <param name="rangeI">The j index to get i array for which data is to be extracted</param>
        /// <param name="indexJ">The range of values i for which data is to be extracted</param>
        /// <returns>the 1d data array slice from 2d data array</returns>
        public T[] this[Range rangeI, Index indexJ]
        {
            get
            {
                Range rangeJ = new Range(indexJ, indexJ.IsFromEnd ? indexJ.Value - 1 : indexJ.Value + 1);
                return this[rangeI, rangeJ][..][0];
            }
            set
            {
                var dummy = Jagged.Create<T>(value.Length, 1);
                for (int i = 0; i < value.Length; i++)
                    dummy[i][0] = value[i];

                Range rangeJ = new Range(indexJ, indexJ.IsFromEnd ? indexJ.Value - 1 : indexJ.Value + 1);
                this[rangeI, rangeJ] = dummy;
            }
        }

        #endregion 2D to 1D slice access

        /// <summary>
        /// k slice extraction.  Assumes data Shape is 3 dimensional!
        /// </summary>
        /// <param name="indexI">The i index to get k array for which data is to be extracted</param>
        /// <param name="indexJ">The j index to get k array for which data is to be extracted</param>
        /// <param name="rangeK">The range of values k for which data is to be extracted</param>
        /// <returns>the 1d data array slice from 3d data array</returns>
        public T[] this[Index indexI, Index indexJ, Range rangeK]
        {
            get
            {
                int i_0 = indexI.IsFromEnd == false ? indexI.Value : Shape[0] - indexI.Value;
                int j_0 = indexJ.IsFromEnd == false ? indexJ.Value : Shape[1] - indexJ.Value;

                int k_0 = rangeK.Start.IsFromEnd == false ? rangeK.Start.Value : Shape[2] - rangeK.Start.Value;
                int k_n = rangeK.End.IsFromEnd == false ? rangeK.End.Value : Shape[2] - rangeK.End.Value;
                int k_count = k_n - k_0;

                T[] result = new T[k_count];

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int k = 0; k < k_count; k++)
                {
                    // get flattened index of file array element position
                    var file_index = _ravel3D(i_0, j_0, k_0 + k);

                    // if file_index is outside of bounds of buffer, read fill buffer
                    if (file_index < bufferSPosition || file_index >= bufferEPosition)
                    {
                        _readFillBuff(_buffer, file_index, maxBufferSize);
                        bufferSPosition = _bufferStartPosition;
                        bufferEPosition = _bufferStartPosition + _buffer.Length;
                    }

                    // otherwise just copy sample over
                    result[k] = _buffer[file_index - _bufferStartPosition];
                }
                return result;
            }
            set
            {
                var dummy = Jagged.Create<T>(1, 1, value.Length);
                for (int k = 0; k < value.Length; k++)
                    dummy[0][0][k] = value[k];

                Range rangeI = new Range(indexI, indexI.IsFromEnd ? indexI.Value - 1 : indexI.Value + 1);
                Range rangeJ = new Range(indexJ, indexJ.IsFromEnd ? indexJ.Value - 1 : indexJ.Value + 1);
                this[rangeI, rangeJ, rangeK] = dummy;
            }
        }

        /// <summary>
        /// j slice extraction.  Assumes data Shape is 3 dimensional!
        /// </summary>
        /// <param name="indexI">The i index to get j array for which data is to be extracted</param>
        /// <param name="rangeJ">The range of values j for which data is to be extracted</param>
        /// <param name="indexK">The j index to get j array for which data is to be extracted</param>
        /// <returns>the 1d data array slice from 3d data array</returns>
        public T[] this[Index indexI, Range rangeJ, Index indexK]
        {
            get
            {
                int i_0 = indexI.IsFromEnd == false ? indexI.Value : Shape[0] - indexI.Value;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                int k_0 = indexK.IsFromEnd == false ? indexK.Value : Shape[2] - indexK.Value;

                T[] result = new T[j_count];

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int j = 0; j < j_count; j++)
                {
                    // get flattened index of file array element position
                    var file_index = _ravel3D(i_0, j_0 + j, k_0);

                    // if file_index is outside of bounds of buffer, read fill buffer
                    if (file_index < bufferSPosition || file_index >= bufferEPosition)
                    {
                        _readFillBuff(_buffer, file_index, maxBufferSize);
                        bufferSPosition = _bufferStartPosition;
                        bufferEPosition = _bufferStartPosition + _buffer.Length;
                    }

                    // otherwise just copy sample over
                    result[j] = _buffer[file_index - _bufferStartPosition];
                }
                return result;
            }
            set
            {
                var dummy = Jagged.Create<T>(1, value.Length, 1);
                for (int j = 0; j < value.Length; j++)
                    dummy[0][j][0] = value[j];

                Range rangeI = new Range(indexI, indexI.IsFromEnd ? indexI.Value - 1 : indexI.Value + 1);
                Range rangeK = new Range(indexK, indexK.IsFromEnd ? indexK.Value - 1 : indexK.Value + 1);
                this[rangeI, rangeJ, rangeK] = dummy;
            }
        }

        /// <summary>
        /// i slice extraction.  Assumes data Shape is 3 dimensional!
        /// </summary>
        /// <param name="rangeI">The range of values i for which data is to be extracted</param>
        /// <param name="indexJ">The j index to get i array for which data is to be extracted</param>
        /// <param name="indexK">The j index to get j array for which data is to be extracted</param>
        /// <returns>the 1d data array slice from 3d data array</returns>
        public T[] this[Range rangeI, Index indexJ, Index indexK]
        {
            get
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = indexJ.IsFromEnd == false ? indexJ.Value : Shape[1] - indexJ.Value;

                int k_0 = indexK.IsFromEnd == false ? indexK.Value : Shape[2] - indexK.Value;

                T[] result = new T[i_count];

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int i = 0; i < i_count; i++)
                {
                    // get flattened index of file array element position
                    var file_index = _ravel3D(i_0 + i, j_0, k_0);

                    // if file_index is outside of bounds of buffer, read fill buffer
                    if (file_index < bufferSPosition || file_index >= bufferEPosition)
                    {
                        _readFillBuff(_buffer, file_index, maxBufferSize);
                        bufferSPosition = _bufferStartPosition;
                        bufferEPosition = _bufferStartPosition + _buffer.Length;
                    }

                    // otherwise just copy sample over
                    result[i] = _buffer[file_index - _bufferStartPosition];
                }
                return result;
            }
            set
            {
                var dummy = Jagged.Create<T>(value.Length, 1, 1);
                for (int i = 0; i < value.Length; i++)
                    dummy[i][0][0] = value[i];

                Range rangeJ = new Range(indexJ, indexJ.IsFromEnd ? indexJ.Value - 1 : indexJ.Value + 1);
                Range rangeK = new Range(indexK, indexK.IsFromEnd ? indexK.Value - 1 : indexK.Value + 1);
                this[rangeI, rangeJ, rangeK] = dummy;
            }
        }

        /// <summary>
        /// [j,k] slice extraction.  Assumes data Shape is 3 dimensional!
        /// </summary>
        /// <param name="indexI">The i index to get [j,k] array for which data is to be extracted</param>
        /// <param name="rangeJ">The range of values j for which data is to be extracted</param>
        /// <param name="rangeK">The range of values k for which data is to be extracted</param>
        /// <returns>the 2d data array slice from 3d data array</returns>
        public T[][] this[Index indexI, Range rangeJ, Range rangeK]
        {
            get
            {
                int i_0 = indexI.IsFromEnd == false ? indexI.Value : Shape[0] - indexI.Value;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                int k_0 = rangeK.Start.IsFromEnd == false ? rangeK.Start.Value : Shape[2] - rangeK.Start.Value;
                int k_n = rangeK.End.IsFromEnd == false ? rangeK.End.Value : Shape[2] - rangeK.End.Value;
                int k_count = k_n - k_0;

                T[][] result = Jagged.Create<T>(j_count, k_count);

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int j = 0; j < j_count; j++)
                    for (int k = 0; k < k_count; k++)
                    {
                        // get flattened index of file array element position
                        var file_index = _ravel3D(i_0, j_0 + j, k_0 + k);

                        // if file_index is outside of bounds of buffer, read fill buffer
                        if (file_index < bufferSPosition || file_index >= bufferEPosition)
                        {
                            _readFillBuff(_buffer, file_index, maxBufferSize);
                            bufferSPosition = _bufferStartPosition;
                            bufferEPosition = _bufferStartPosition + _buffer.Length;
                        }

                        // otherwise just copy sample over
                        result[j][k] = _buffer[file_index - _bufferStartPosition];
                    }
                return result;
            }
            set
            {
                var dummy = Jagged.Create<T>(1, value.Length, value[0].Length);

                for (int j = 0; j < value.Length; j++)
                    for (int k = 0; k < value[0].Length; k++)
                        dummy[0][j][k] = value[j][k];

                Range rangeI = new Range(indexI, indexI.IsFromEnd ? indexI.Value - 1 : indexI.Value + 1);
                this[rangeI, rangeJ, rangeK] = dummy;
            }
        }

        /// <summary>
        /// [i,k] slice extraction.  Assumes data Shape is 3 dimensional!
        /// </summary>  
        /// <param name="rangeI">The range of values i for which data is to be extracted</param>
        /// <param name="indexJ">The j index to get [i,k] array for which data is to be extracted</param>
        /// <param name="rangeK">The range of values k for which data is to be extracted</param>
        /// <returns>the 2d data array slice from 3d data array</returns>
        public T[][] this[Range rangeI, Index indexJ, Range rangeK]
        {
            get
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = indexJ.IsFromEnd == false ? indexJ.Value : Shape[1] - indexJ.Value;

                int k_0 = rangeK.Start.IsFromEnd == false ? rangeK.Start.Value : Shape[2] - rangeK.Start.Value;
                int k_n = rangeK.End.IsFromEnd == false ? rangeK.End.Value : Shape[2] - rangeK.End.Value;
                int k_count = k_n - k_0;

                T[][] result = Jagged.Create<T>(i_count, k_count);

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int i = 0; i < i_count; i++)
                    for (int k = 0; k < k_count; k++)
                    {
                        // get flattened index of file array element position
                        var file_index = _ravel3D(i_0 + i, j_0, k_0 + k);

                        // if file_index is outside of bounds of buffer, read fill buffer
                        if (file_index < bufferSPosition || file_index >= bufferEPosition)
                        {
                            _readFillBuff(_buffer, file_index, maxBufferSize);
                            bufferSPosition = _bufferStartPosition;
                            bufferEPosition = _bufferStartPosition + _buffer.Length;
                        }

                        // otherwise just copy sample over
                        result[i][k] = _buffer[file_index - _bufferStartPosition];
                    }
                return result;
            }
            set
            {
                var dummy = Jagged.Create<T>(value.Length, 1, value[0].Length);

                for (int i = 0; i < value.Length; i++)
                    for (int k = 0; k < value[0].Length; k++)
                        dummy[i][0][k] = value[i][k];

                Range rangeJ = new Range(indexJ, indexJ.IsFromEnd ? indexJ.Value - 1 : indexJ.Value + 1);
                this[rangeI, rangeJ, rangeK] = dummy;
            }
        }

        /// <summary>
        /// [i,j] slice extraction.  Assumes data Shape is 3 dimensional!
        /// </summary>  
        /// <param name="rangeI">The range of values i for which data is to be extracted</param>
        /// <param name="rangeJ">The range of values j for which data is to be extracted</param>
        /// <param name="indexK">The k index to get [i,j] array for which data is to be extracted</param>
        /// <returns>the 2d data array slice from 3d data array</returns>
        public T[][] this[Range rangeI, Range rangeJ, Index indexK]
        {
            get
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                int k_0 = indexK.IsFromEnd == false ? indexK.Value : Shape[2] - indexK.Value;

                T[][] result = Jagged.Create<T>(i_count, j_count);

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int i = 0; i < i_count; i++)
                    for (int j = 0; j < j_count; j++)
                    {
                        // get flattened index of file array element position
                        var file_index = _ravel3D(i_0 + i, j_0 + j, k_0);

                        // if file_index is outside of bounds of buffer, read fill buffer
                        if (file_index < bufferSPosition || file_index >= bufferEPosition)
                        {
                            _readFillBuff(_buffer, file_index, maxBufferSize);
                            bufferSPosition = _bufferStartPosition;
                            bufferEPosition = _bufferStartPosition + _buffer.Length;
                        }

                        // otherwise just copy sample over
                        result[i][j] = _buffer[file_index - _bufferStartPosition];
                    }
                return result;
            }
            set
            {
                var dummy = Jagged.Create<T>(value.Length, value[0].Length, 1);
                for (int i = 0; i < value.Length; i++)
                    for (int j = 0; j < value[0].Length; j++)
                        dummy[i][j][0] = value[i][j];

                Range rangeK = new Range(indexK, indexK.IsFromEnd ? indexK.Value - 1 : indexK.Value + 1);
                this[rangeI, rangeJ, rangeK] = dummy;
            }
        }

        #endregion System.Index based index access

        #region System.Range based access

        /// <summary>
        /// 1d slice extraction. Assumes 1d dimensional data.
        /// </summary>
        /// <param name="range">The range of indices for which a slice should be extracted. assumes 1D data</param>
        /// <returns>The slice</returns>
        public T[] this[Range range]
        {
            get
            {
                int i_0 = range.Start.IsFromEnd == false ? range.Start.Value : Shape[0] - range.Start.Value;
                int i_n = range.End.IsFromEnd == false ? range.End.Value : Shape[0] - range.End.Value;
                int i_count = i_n - i_0;

                if (i_0 + i_count - 1 >= SampleCount) throw new Exception("Cannot read more samples than are in the file");

                T[] result = new T[i_count];

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                TryFillFlushBuffer(i_0);

                for (int i = 0; i < i_count; i++)
                {
                    long file_index = i_0 + i;

                    // if file_index is outside of bounds of buffer, read fill buffer
                    if (file_index < bufferSPosition || file_index >= bufferEPosition)
                    {
                        _readFillBuff(_buffer, file_index, maxBufferSize);
                        bufferSPosition = _bufferStartPosition;
                        bufferEPosition = _bufferStartPosition + _buffer.Length;
                    }
                    //if (file_index < _bufferStartPosition || file_index >= _bufferStartPosition + _buffer.Length)
                    //{
                    //    //if (file_index + _buffer.Length >= SampleCount)
                    //        //_readFillBuff(_buffer, file_index, (int)(SampleCount - file_index));
                    //    //else
                    //        _readFillBuff(_buffer, file_index, maxBufferSize);
                    //}
                    // otherwise just copy sample over
                    result[i] = _buffer[file_index - _bufferStartPosition];
                }
                return result;
            }
            set
            {
                int i_0 = range.Start.IsFromEnd == false ? range.Start.Value : Shape[0] - range.Start.Value;
                int i_n = range.End.IsFromEnd == false ? range.End.Value : Shape[0] - range.End.Value;
                int i_count = i_n - i_0;

                var file_index_0 = i_0;

                FlushBuffer();
                _bufferStartPosition = file_index_0;
                for (int i = 0; i < i_count; i++)
                {
                    // get flattened index of file array element position
                    var file_index = i_0 + i;

                    // if file_index is outside of bounds of buffer, flush buffer, then read fill buffer to new index
                    if (file_index < _bufferStartPosition || file_index >= _bufferStartPosition + _buffer.Length)
                    {
                        // first write data to disk
                        FlushBuffer();
                        _bufferStartPosition = file_index;
                    }
                    _buffer[file_index - _bufferStartPosition] = value[i];
                }
                FlushBuffer();
            }
        }

        /// <summary>
        /// 2d slice extraction. Assumes 2d dimensional data.
        /// </summary>
        /// <param name="rangeI">The range of i values to extract</param>
        /// <param name="rangeJ">The range of j values to extract</param>
        /// <returns>The range of [i,j] pairs extracted</returns>
        public T[][] this[Range rangeI, Range rangeJ]
        {
            get
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                T[][] result = Jagged.Create<T>(i_count, j_count);

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here
                for (int i = 0; i < i_count; i++)
                    for (int j = 0; j < j_count; j++)
                    {
                        var file_index = _ravel2D(i_0 + i, j_0 + j);

                        // if file_index is outside of bounds of buffer, read fill buffer
                        if (file_index < bufferSPosition || file_index >= bufferEPosition)
                        {
                            _readFillBuff(_buffer, file_index, maxBufferSize);
                            bufferSPosition = _bufferStartPosition;
                            bufferEPosition = _bufferStartPosition + _buffer.Length;
                        }

                        result[i][j] = _buffer[file_index - _bufferStartPosition];
                    }
                return result;
            }
            set
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                var file_index_0 = _ravel2D(i_0, j_0);

                TryFillFlushBuffer(file_index_0);
                for (int i = 0; i < i_count; i++)
                    for (int j = 0; j < j_count; j++)
                    {
                        // get flattened index of file array element position
                        var file_index = _ravel2D(i_0 + i, j_0 + j);

                        TryFillFlushBuffer(file_index);

                        _buffer[file_index - _bufferStartPosition] = value[i][j];
                    }
                FlushBuffer();
            }
        }

        /// <summary>
        /// 3d slice ranged indexing. Assumes 3d dimensional data.
        /// </summary>
        /// <param name="rangeI">The range of i values to extract or set</param>
        /// <param name="rangeJ">The range of j values to extract or set</param>
        /// <param name="rangeK">The range of k values to extract or set</param>
        /// <returns>The range of [i,j,k] pairs extracted or set</returns>
        public T[][][] this[Range rangeI, Range rangeJ, Range rangeK]
        {
            get
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                int k_0 = rangeK.Start.IsFromEnd == false ? rangeK.Start.Value : Shape[2] - rangeK.Start.Value;
                int k_n = rangeK.End.IsFromEnd == false ? rangeK.End.Value : Shape[2] - rangeK.End.Value;
                int k_count = k_n - k_0;

                T[][][] result = Jagged.Create<T>(i_count, j_count, k_count);

                var bufferSPosition = _bufferStartPosition;
                var bufferEPosition = _bufferStartPosition + _buffer.Length;

                FlushBuffer();  // Should flush data in buffer to file in case it needs to be read here

                //if (IsFortranOrder == false)
                //{
                //    for (int i = 0; i < i_count; i++)
                //        for (int j = 0; j < j_count; j++)
                //        {
                //            var file_index = _ravel3D(i_0 + i, j_0 + j, k_0);
                //            for (int k = 0; k < k_count; k++)
                //            {
                //                // if file_index is outside of bounds of buffer, read fill buffer
                //                if (file_index < bufferSPosition || file_index >= bufferEPosition)
                //                {
                //                    _readFillBuff(_buffer, file_index, maxBufferSize);
                //                    bufferSPosition = _bufferStartPosition;
                //                    bufferEPosition = _bufferStartPosition + _buffer.Length;
                //                }

                //                result[i][j][k] = _buffer[file_index - _bufferStartPosition];
                //                file_index++;
                //            }
                //        }
                //}
                //else
                //{
                //    for (int k = 0; k < k_count; k++)
                //        for (int j = 0; j < j_count; j++)
                //        {
                //            var file_index = _ravel3D(i_0, j_0 + j, k_0);
                //            for (int i = 0; i < i_count; i++)
                //            {
                //                // if file_index is outside of bounds of buffer, read fill buffer
                //                if (file_index < bufferSPosition || file_index >= bufferEPosition)
                //                {
                //                    _readFillBuff(_buffer, file_index, maxBufferSize);
                //                    bufferSPosition = _bufferStartPosition;
                //                    bufferEPosition = _bufferStartPosition + _buffer.Length;
                //                }

                //                result[i][j][k] = _buffer[file_index - _bufferStartPosition];
                //                file_index++;
                //            }
                //        }
                //}
                for (int i = 0; i < i_count; i++)
                    for (int j = 0; j < j_count; j++)
                        for (int k = 0; k < k_count; k++)
                        {
                            // get flattened index of file array element position
                            var file_index = _ravel3D(i_0 + i, j_0 + j, k_0 + k);

                            // if file_index is outside of bounds of buffer, read fill buffer
                            if (file_index < bufferSPosition || file_index >= bufferEPosition)
                            {
                                _readFillBuff(_buffer, file_index, maxBufferSize);
                                bufferSPosition = _bufferStartPosition;
                                bufferEPosition = _bufferStartPosition + _buffer.Length;
                            }

                            // otherwise just copy sample over
                            result[i][j][k] = _buffer[file_index - _bufferStartPosition];
                        }
                return result;
            }
            set
            {
                int i_0 = rangeI.Start.IsFromEnd == false ? rangeI.Start.Value : Shape[0] - rangeI.Start.Value;
                int i_n = rangeI.End.IsFromEnd == false ? rangeI.End.Value : Shape[0] - rangeI.End.Value;
                int i_count = i_n - i_0;

                int j_0 = rangeJ.Start.IsFromEnd == false ? rangeJ.Start.Value : Shape[1] - rangeJ.Start.Value;
                int j_n = rangeJ.End.IsFromEnd == false ? rangeJ.End.Value : Shape[1] - rangeJ.End.Value;
                int j_count = j_n - j_0;

                int k_0 = rangeK.Start.IsFromEnd == false ? rangeK.Start.Value : Shape[2] - rangeK.Start.Value;
                int k_n = rangeK.End.IsFromEnd == false ? rangeK.End.Value : Shape[2] - rangeK.End.Value;
                int k_count = k_n - k_0;

                var file_index_0 = _ravel3D(i_0, j_0, k_0);

                TryFillFlushBuffer(file_index_0);  // Try to flush any pending changes and reset buffer to start position for this write
                for (int i = 0; i < i_count; i++)
                    for (int j = 0; j < j_count; j++)
                        for (int k = 0; k < k_count; k++)
                        {
                            // get flattened index of file array element position
                            var file_index = _ravel3D(i_0 + i, j_0 + j, k_0 + k);

                            // if file_index is outside of bounds of buffer, flush buffer, then read fill buffer to new index
                            if (file_index < _bufferStartPosition || file_index >= _bufferStartPosition + _buffer.Length)
                            {
                                // first write data to disk
                                FlushBuffer();
                                _bufferStartPosition = file_index;
                            }
                            _buffer[file_index - _bufferStartPosition] = value[i][j][k];
                        }
                FlushBuffer();
            }
        }

        #endregion System.Range based access

        /// <summary>
        /// Reads a slice from a 2d array file.  Array in npy array file must be 2 dimensions as asssumed that 1d exemplifies the slice notion.
        /// </summary>
        /// <param name="index">The index of the slice along the axis intended to read from</param>
        /// <param name="array">The array to read data into</param>
        /// <param name="axis">The axis perpendicular to which a slice should be read</param>
        public void Read(int index, out T[] array, int axis = 0)
        {
            CodeContract.Requires(axis >= 0, "The axis index of the slice must be greater than 0");
            CodeContract.Requires(axis < Shape.Length, "The axis index of the slice cannot be greater than the length of the arrays shape");
            CodeContract.Requires(Shape.Length == 2, "In order to extract a 2D slice, the shape of the array must be 3D");
            CodeContract.Requires(index < Shape[axis], "The slice index of the axis to acquire a slice for must be less than the size of that axis");

            var ni = Shape[0];
            var nj = Shape[1];
            var arrayIndices = new int[2];

            if (axis == 0)
            {
                array = new T[nj];
                for (int j = 0; j < nj; j++)
                {
                    arrayIndices[0] = index;
                    arrayIndices[1] = j;
                    array[j] = this[arrayIndices];
                }
            }
            else if (axis == 1)
            {
                array = new T[ni];
                for (int i = 0; i < ni; i++)
                {
                    arrayIndices[0] = i;
                    arrayIndices[1] = index;
                    array[i] = this[arrayIndices];
                }
            }
            else throw new ArgumentException("In order to extract a 2D slice, the shape of the array must be 3D and must have 3 axes");
        }

        /// <summary>
        /// Reads a slice from a 3d array file.  Array in npy array file must be 3 dimensions as asssumed that 2d exemplifies the slice notion.
        /// </summary>
        /// <param name="index">The index of the slice along the axis intended to read from</param>
        /// <param name="array">The array to read data into</param>
        /// <param name="axis">The axis perpendicular to which a slice should be read</param>
        public void Read(int index, out T[][] array, int axis = 0)
        {
            CodeContract.Requires(axis >= 0, "The axis index of the slice must be greater than 0");
            CodeContract.Requires(axis < Shape.Length, "The axis index of the slice cannot be greater than the length of the arrays shape");
            CodeContract.Requires(Shape.Length == 3, "In order to extract a 2D slice, the shape of the array must be 3D");
            CodeContract.Requires(index < Shape[axis], "The slice index of the axis to acquire a slice for must be less than the size of that axis");

            var ni = Shape[0];
            var nj = Shape[1];
            var nk = Shape[2];
            var arrayIndices = new int[3];

            if (axis == 0)
            {
                array = Jagged.Create<T>(nj, nk);
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        arrayIndices[0] = index;
                        arrayIndices[1] = j;
                        arrayIndices[2] = k;
                        array[j][k] = this[index, j, k];
                    }
            }
            else if (axis == 1)
            {
                array = Jagged.Create<T>(ni, nk);
                for (int i = 0; i < ni; i++)
                    for (int k = 0; k < nk; k++)
                    {
                        arrayIndices[0] = i;
                        arrayIndices[1] = index;
                        arrayIndices[2] = k;
                        array[i][k] = this[i, index, k];
                    }
            }
            else if (axis == 2)
            {
                array = Jagged.Create<T>(ni, nj);
                for (int i = 0; i < ni; i++)
                {
                    for (int j = 0; j < nj; j++)
                    {
                        arrayIndices[0] = i;
                        arrayIndices[1] = j;
                        arrayIndices[2] = index;
                        array[i][j] = this[i, j, index];
                    }
                }
            }
            else throw new ArgumentException("In order to extract a 2D slice, the shape of the array must be 3D and must have 3 axes");
        }

        /// <summary>
        /// Reads a slice from a 4d array file.  Array in npy array file must be 4 dimensions as asssumed that 3d exemplifies the slice notion.
        /// </summary>
        /// <param name="index">The index of the slice along the axis intended to read from</param>
        /// <param name="array">The array to read data into</param>
        /// <param name="axis">The axis perpendicular to which a slice should be read</param>
        public void Read(int index, out T[][][] array, int axis = 0)
        {
            CodeContract.Requires(axis >= 0, "The axis index of the slice must be greater than 0");
            CodeContract.Requires(axis < Shape.Length, "The axis index of the slice cannot be greater than the length of the arrays shape");
            CodeContract.Requires(Shape.Length == 4, "In order to extract a 3D slice, the shape of the array must be 4D");
            CodeContract.Requires(index < Shape[axis], "The slice index of the axis to acquire a slice for must be less than the size of that axis");

            var ni = Shape[0];
            var nj = Shape[1];
            var nk = Shape[2];
            var nl = Shape[3];
            var arrayIndices = new int[4];
            if (axis == 0)
            {
                array = Jagged.Create<T>(nj, nk, nl);
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        for (int l = 0; l < nl; l++)
                        {
                            arrayIndices[0] = index;
                            arrayIndices[1] = j;
                            arrayIndices[2] = k;
                            arrayIndices[3] = l;
                            array[j][k][l] = this[index, j, k, l];
                        }
            }
            else if (axis == 1)
            {
                array = Jagged.Create<T>(ni, nk, nl);
                for (int i = 0; i < ni; i++)
                    for (int k = 0; k < nk; k++)
                        for (int l = 0; l < nl; l++)
                        {
                            arrayIndices[0] = i;
                            arrayIndices[1] = index;
                            arrayIndices[2] = k;
                            arrayIndices[3] = l;
                            array[i][k][l] = this[i, index, k, l];
                        }
            }
            else if (axis == 2)
            {
                array = Jagged.Create<T>(ni, nj, nl);
                for (int i = 0; i < ni; i++)
                    for (int j = 0; j < nj; j++)
                        for (int l = 0; l < nl; l++)
                        {
                            arrayIndices[0] = i;
                            arrayIndices[1] = j;
                            arrayIndices[2] = index;
                            arrayIndices[3] = l;
                            array[i][j][l] = this[i, j, index, l];
                        }
            }
            else if (axis == 3)
            {
                array = Jagged.Create<T>(ni, nj, nk);

                for (int i = 0; i < ni; i++)
                    for (int j = 0; j < nj; j++)
                        for (int k = 0; k < nk; k++)
                        {
                            arrayIndices[0] = i;
                            arrayIndices[1] = j;
                            arrayIndices[2] = k;
                            arrayIndices[3] = index;
                            array[i][j][k] = this[i, j, k, index];
                        }
            }
            else throw new ArgumentException("In order to extract a 3D slice, the shape of the array must be 4D and have 4 axes");
        }

        /// <summary>
        /// Opens a preexisting numpy file
        /// </summary>
        /// <param name="fileInfo">The file info of the file to be opened</param>
        /// <returns>A numpy file handler</returns>
        public static NpyFileBuffered<T> Open(FileInfo fileInfo)
        {
            if (fileInfo.Exists == false)
                throw new FileNotFoundException($"File not found {fileInfo.FullName}");

            FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            byte[] buffer = new byte[10];

            var res = fileStream.Read(buffer, 0, 10);
            if (res != 10)
                throw new Exception("Numpy header parse failed on stream read bytes");
            var majorVersionNumber = buffer[6];
            var minorVersionNumber = buffer[7];
            var headerLength = BitConverter.ToUInt16(new byte[] { buffer[8], buffer[9] });

            TextReader strReader = new StreamReader(fileStream);
            char[] charBuffer = new char[headerLength];
            strReader.Read(charBuffer, 0, headerLength);
            if (charBuffer.Length != headerLength)
                throw new Exception("Numpy header parse failed on stream read bytes");

            //var str = new string(charBuffer);
            //if (charBuffer[charBuffer.Length - 1] != '\n')
            //    throw new Exception("Numpy header parse failed on stream read bytes");
            int loc1 = 0;
            int loc2 = 0;
            var headerStr = new string(charBuffer);

            // fortran_order
            var fortranOrderIndex = headerStr.IndexOf("fortran_order");
            bool fortranOrder = false;
            if (headerStr[fortranOrderIndex + 14 + 2] == 'F')
                fortranOrder = false;
            else
                fortranOrder = true;

            // description
            var descriptionIndex = headerStr.IndexOf("descr");
            descriptionIndex += 9;
            bool littleEndian = (headerStr[descriptionIndex] == '<' || headerStr[descriptionIndex] == '|' ? true : false);

            // data type
            var dataType = headerStr[descriptionIndex + 1];

            // word size
            var str_ws = headerStr.Substring(descriptionIndex + 2);
            loc2 = str_ws.IndexOf("'");
            int wordSizeChar = int.Parse(str_ws.Substring(0, loc2));

            // shape
            loc1 = Array.IndexOf(charBuffer, '(');
            loc2 = Array.IndexOf(charBuffer, ')');
            var r = new string(charBuffer).Substring(loc1 + 1, loc2 - loc1 - 1).Split(',');
            var dimensionality = r.Length;
            var dimensionSizes = r.Select(s => int.Parse(s)).ToArray();

            // datastartIndex
            var headerSize = MagicStringBytes.Length + 2 + 2 + headerLength;
            var type = GetType(dataType, wordSizeChar);

            if (type != typeof(T))
                throw new Exception("Generic type used to create the file is not the same as the type of data specified in the file");

            fileStream.Dispose();

            var result = new NpyFileBuffered<T>(fileInfo, fortranOrder, littleEndian, dimensionSizes, headerSize);
            result.MajorVersionNumber = majorVersionNumber;
            result.MinorVersionNumber = minorVersionNumber;
            result.headerSize = headerSize;
            return result;
        }

        /// <summary>
        /// Creates a NUMPY NdArray file for 1d data
        /// </summary>
        /// <param name="fileInfo">The file info to create the file as. Extension ".npy" is recommended</param>
        /// <param name="shape">The shape of the file, or dimensionality and sizes of the nd array</param>
        /// <param name="isLittleEndian">Is binary data to be serialized little or big endian byte ordering</param>
        /// <param name="isFortranOrder">Array storage technique with "k" or last index as the "slowest" column, otherwise
        /// assume "c" style ordering with "k" or the last index as the "fastest" column</param>
        /// <returns>A numpy file handler</returns>
        public static NpyFileBuffered<T> Create(FileInfo fileInfo, long shape, bool isLittleEndian = true, bool isFortranOrder = false, bool overwrite = true)
        {
            if (overwrite == false)
                if (fileInfo.Exists == true)
                    throw new ArgumentException("The file {} already exists, either delete it before creating this file or set the 'overwrite' parameter to true");
            if (Validate<T>() == false)
                throw new ArgumentException($"Type {typeof(T)} is not supported as a numpy file type");

            byte majorVersion = 1;
            byte minorVersion = 0;
            var headerLengthBytes = BitConverter.GetBytes((ushort)118);
            var fortranOrderString = isFortranOrder ? "True" : "False";
            // "{'descr': '<f4', 'fortran_order': False, 'shape': (16, 23, 15), }                                                    \n";
            var header = "{'descr': '";
            header = isLittleEndian ? header + "<" + GetTypeString(typeof(T)) + "'" : header + ">" + GetTypeString(typeof(T)) + "'";
            header = header + ", 'fortran_order': " + fortranOrderString + ", 'shape': (" + string.Join(", ", shape) + "), }";
            header = header.PadRight(127 - 10);
            header = new String(header.Append('\n').ToArray());

            var headerBytes = Encoding.ASCII.GetBytes(header);

            long totalSize = shape;

            var elementSize = Marshal.SizeOf<T>();
            long totalByteCount = 128 + totalSize * elementSize;

            using (var fileStream = File.Create(fileInfo.FullName))
            {
                fileStream.Write(MagicStringBytes);
                fileStream.WriteByte(majorVersion);
                fileStream.WriteByte(minorVersion);
                fileStream.Write(headerLengthBytes);

                fileStream.Write(headerBytes);
                fileStream.Seek(totalByteCount - 1, SeekOrigin.Begin);
                fileStream.WriteByte(0);
            }

            return Open(fileInfo);
        }

        /// <summary>
        /// Creates a NUMPY NdArray file
        /// </summary>
        /// <param name="fileInfo">The file info to create the file as. Extension ".npy" is recommended</param>
        /// <param name="shape">The shape of the file, or dimensionality and sizes of the nd array</param>
        /// <param name="isLittleEndian">Is binary data to be serialized little or big endian byte ordering</param>
        /// <param name="isFortranOrder">Array storage technique with "k" or last index as the "slowest" column, otherwise
        /// assume "c" style ordering with "k" or the last index as the "fastest" column</param>
        /// <returns>A numpy file handler</returns>
        public static NpyFileBuffered<T> Create(FileInfo fileInfo, int[] shape, bool isLittleEndian = true, bool isFortranOrder = false, bool overwrite = true)
        {
            if (overwrite == false)
                if (fileInfo.Exists == true)
                    throw new ArgumentException("The file {} already exists, either delete it before creating this file or set the 'overwrite' parameter to true");
            if (Validate<T>() == false)
                throw new ArgumentException($"Type {typeof(T)} is not supported as a numpy file type");

            byte majorVersion = 1;
            byte minorVersion = 0;
            var headerLengthBytes = BitConverter.GetBytes((ushort)118);
            var fortranOrderString = isFortranOrder ? "True" : "False";
            // "{'descr': '<f4', 'fortran_order': False, 'shape': (16, 23, 15), }                                                    \n";
            var header = "{'descr': '";
            header = isLittleEndian ? header + "<" + GetTypeString(typeof(T)) + "'" : header + ">" + GetTypeString(typeof(T)) + "'";
            header = header + ", 'fortran_order': " + fortranOrderString + ", 'shape': (" + string.Join(", ", shape) + "), }";
            header = header.PadRight(127 - 10);
            header = new String(header.Append('\n').ToArray());

            var headerBytes = Encoding.ASCII.GetBytes(header);

            long totalSize = 1;
            for (int i = 0; i < shape.Length; i++)
                totalSize *= shape[i];

            var elementSize = Marshal.SizeOf<T>();
            long totalByteCount = 128 + totalSize * elementSize;

            using (var fileStream = File.Create(fileInfo.FullName))
            {
                fileStream.Write(MagicStringBytes);
                fileStream.WriteByte(majorVersion);
                fileStream.WriteByte(minorVersion);
                fileStream.Write(headerLengthBytes);

                fileStream.Write(headerBytes);
                fileStream.Seek(totalByteCount - 1, SeekOrigin.Begin);
                fileStream.WriteByte(0);
            }

            return Open(fileInfo);
        }

        /// <summary>
        /// Tries to refill the buffer with samples near the coordinate input.
        /// </summary>
        /// <param name="file_index">Sample coordinate to fill buffer near</param>
        private void TryFillFlushBuffer(long file_index)
        {
            // if file_index is outside of bounds of buffer, flush buffer, then read fill buffer to new index
            if (file_index < _bufferStartPosition || file_index >= _bufferStartPosition + _buffer.Length)
            {
                // first write data to disk
                FlushBuffer();

                // if file_index is in the 'last' chunk of data to be read
                if (file_index + _buffer.Length >= SampleCount)
                    _readFillBuff(_buffer, file_index, (int)(SampleCount - file_index));// (int)(SampleCount - file_index));
                else
                    _readFillBuff(_buffer, file_index, maxBufferSize);
            }
        }

        /// <summary>
        /// Flush the sample buffer in this class to file
        /// </summary>
        private void FlushBuffer()
        {
            // if were not at the end chunk of the file just write the whole array
            // otherwise only write SampleCount - bufferFlattenedStartIndex
            if (SampleCount - _bufferStartPosition <= maxBufferSize)
                _writeBuff(_buffer, _bufferStartPosition, (int)(SampleCount - _bufferStartPosition));
            else
                _writeBuff(_buffer, _bufferStartPosition, maxBufferSize);
        }

        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            FlushBuffer();

            TryDontThrow(() => this.stream.Dispose());
            TryDontThrow(() => this.binaryReader.Dispose());
            TryDontThrow(() => this.binaryWriter.Dispose());
        }
    }
}