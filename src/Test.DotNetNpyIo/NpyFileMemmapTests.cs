using DotNetNpyIo;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Test.DotNetNpyIo
{
    public class NpyFileMemmapTests
    {
        private static Func<int[], int[], long> cRavelAlgorithm = (ints, shape) =>
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

        private readonly ITestOutputHelper _testOutputHelper;

        public NpyFileMemmapTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(@"../../../Data/float_2d_counter__301_5000.npy")]
        public void ReadSequential2DFloat(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var npyFile = NpyFileMemmap<float>.Open(fileInfo);

            DateTime start = DateTime.Now;
            var counter = 0;
            for (int i = 0; i < npyFile.Shape[0]; i++)
                for (int j = 0; j < npyFile.Shape[1]; j++)
                {
                    var value = npyFile[i, j];
                    Assert.True(counter == value);
                    counter++;
                }

            _testOutputHelper.WriteLine($"Sequential read and valid assert elapsed time: {DateTime.Now - start}");
            npyFile.Dispose();
        }

        [Theory]
        [InlineData(1, 11)]
        [InlineData(1, 262)]
        [InlineData(300, 5624)]
        [InlineData(3001, 11442)]
        public void CreateWriteSequentialReadSequential2DFloat(int ni, int nj)
        {
            var npyFile = NpyFileMemmap<float>.Create(new FileInfo($"float_{ni}_{nj}__{ShortGuid.NewGuid()}.npy"), new[] { ni, nj });

            DateTime start = DateTime.Now;
            var counter = 0;
            for (int i = 0; i < npyFile.Shape[0]; i++)
                for (int j = 0; j < npyFile.Shape[1]; j++)
                {
                    npyFile[i, j] = counter;
                    counter++;
                }

            counter = 0;
            for (int i = 0; i < npyFile.Shape[0]; i++)
                for (int j = 0; j < npyFile.Shape[1]; j++)
                {
                    var value = npyFile[i, j];
                    Assert.True(value == counter);
                    counter++;
                }

            _testOutputHelper.WriteLine($"Sequential read and valid assert elapsed time: {DateTime.Now - start}");

            npyFile.Dispose();
            npyFile.FileInfo.Delete();
        }

        [Theory]
        [InlineData(10, 23, 51)]
        [InlineData(42, 234, 111)]
        [InlineData(12, 2341, 411)]
        public void CreateWriteSequentialReadSequential3DInt32(int ni, int nj, int nk)
        {
            var fileInfo = new FileInfo($"int_{ni}_{nj}_{nk}__{ShortGuid.NewGuid()}.npy");
            var file = NpyFileMemmap<int>.Create(fileInfo, new int[] { ni, nj, nk }, true, false);

            Assert.True(file.Shape[0] == ni);
            Assert.True(file.Shape[1] == nj);
            Assert.True(file.Shape[2] == nk);

            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        Assert.True(file[i, j, k] == 0);

            file.Dispose();

            int counter = 0;
            file = NpyFileMemmap<int>.Open(fileInfo);
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                    {
                        file[i, j, k] = counter;
                        int counter2 = file[i, j, k];

                        Assert.True(counter2 == counter);
                        counter++;
                    }

            file.Dispose();
            file = NpyFileMemmap<int>.Open(fileInfo);
            counter = 0;
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                    {
                        int counter2 = file[i, j, k];
                        Assert.True(counter2 == counter);
                        counter++;
                    }
            file.Dispose();
            file.FileInfo.Delete();
        }

        [Theory]
        [InlineData(10, 23, 51)]
        [InlineData(42, 234, 111)]
        [InlineData(12, 2341, 411)]
        public void CreateWriteSequentialReadSequential3DFloat(int ni, int nj, int nk)
        {
            var fileInfo = new FileInfo($"float_{ni}_{nj}_{nk}__{ShortGuid.NewGuid()}.npy");
            var file = NpyFileMemmap<float>.Create(fileInfo, new int[] { ni, nj, nk }, true, false);

            Assert.True(file.Shape[0] == ni);
            Assert.True(file.Shape[1] == nj);
            Assert.True(file.Shape[2] == nk);

            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        Assert.True(file[i, j, k] == 0);

            file.Dispose();

            float counter = 0;
            file = NpyFileMemmap<float>.Open(fileInfo);
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                    {
                        file[i, j, k] = counter;
                        float counter2 = file[i, j, k];
                        Assert.True(counter2 == counter);
                        counter++;
                    }

            file.Dispose();
            file = NpyFileMemmap<float>.Open(fileInfo);
            counter = 0;
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                    {
                        float counter2 = file[i, j, k];
                        Assert.True(counter2 == counter);
                        counter++;
                    }
            file.Dispose();
            file.FileInfo.Delete();
        }

        [Theory]
        [InlineData(10, 23, 51, 14)]
        [InlineData(42, 234, 111, 20)]
        [InlineData(12, 2341, 411, 1)]
        public void CreateWriteSequentialReadSequential4DFloat(int ni, int nj, int nk, int nl)
        {
            var fileInfo = new FileInfo($"float_{ni}_{nj}_{nk}_{nl}__{ShortGuid.NewGuid()}.npy");
            var file = NpyFileMemmap<float>.Create(fileInfo, new int[] { ni, nj, nk, nl }, true, false);

            Assert.True(file.Shape[0] == ni);
            Assert.True(file.Shape[1] == nj);
            Assert.True(file.Shape[2] == nk);
            Assert.True(file.Shape[3] == nl);

            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        for (int l = 0; l < file.Shape[3]; l++)
                            Assert.True(file[i, j, k, l] == 0);

            file.Dispose();

            float counter = 0;
            file = NpyFileMemmap<float>.Open(fileInfo);
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        for (int l = 0; l < file.Shape[3]; l++)
                        {
                            file[i, j, k, l] = counter;
                            float counter2 = file[i, j, k, l];

                            Assert.True(counter2 == counter);
                            counter++;
                        }

            file.Dispose();
            file = NpyFileMemmap<float>.Open(fileInfo);
            counter = 0;
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        for (int l = 0; l < file.Shape[3]; l++)
                        {
                            float counter2 = file[i, j, k, l];
                            Assert.True(counter2 == counter);
                            counter++;
                        }
            file.Dispose();
            file.FileInfo.Delete();
        }

        [Theory]
        [InlineData(23, 33, 71, 2, 11)]
        [InlineData(10, 23, 51, 42, 20)]
        public void CreateWriteSequentialReadSequential5DDouble(int ni, int nj, int nk, int nl, int nm)
        {
            var fileInfo = new FileInfo($"float_{ni}_{nj}_{nk}_{nl}_{nm}_{ShortGuid.NewGuid()}.npy");
            var file = NpyFileMemmap<Double>.Create(fileInfo, new int[] { ni, nj, nk, nl, nm }, true, false);
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        for (int l = 0; l < file.Shape[3]; l++)
                            for (int m = 0; m < file.Shape[4]; m++)
                            {
                                Assert.True(file[i, j, k, l, m] == 0);
                            }

            file.Dispose();
            file = NpyFileMemmap<Double>.Open(fileInfo);
            int counter = 0;
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        for (int l = 0; l < file.Shape[3]; l++)
                            for (int m = 0; m < file.Shape[4]; m++)
                            {
                                file[i, j, k, l, m] = counter;
                                int counter2 = (int)file[i, j, k, l, m];
                                Assert.True(counter2 == counter++);
                            }
            file.Dispose();
            file = NpyFileMemmap<Double>.Open(fileInfo);
            counter = 0;
            for (int i = 0; i < file.Shape[0]; i++)
                for (int j = 0; j < file.Shape[1]; j++)
                    for (int k = 0; k < file.Shape[2]; k++)
                        for (int l = 0; l < file.Shape[3]; l++)
                            for (int m = 0; m < file.Shape[4]; m++)
                            {
                                int counter2 = (int)file[i, j, k, l, m];
                                Assert.True(counter2 == counter++);
                            }
            file.Dispose();
            file.FileInfo.Delete();
        }

        [Theory]
        [InlineData(2, 13, 14)]
        [InlineData(251, 121, 41)]
        [InlineData(41, 161, 515)]
        public void CreateWriteSequentialReadRange3DDouble(int ni, int nj, int nk)
        {
            string fileName = $"double_{ni}_{nj}_{nk}_{ShortGuid.NewGuid()}.npy";
            var npyFile = NpyFileMemmap<double>.Create(new FileInfo(fileName), new int[] { ni, nj, nk }, true, false);

            DateTime start = DateTime.Now;
            double counter = 0;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        npyFile[i, j, k] = counter++;
                    }
            TimeSpan elapsed0 = DateTime.Now - start;

            start = DateTime.Now;
            double[][][] rangeReadArray = npyFile[.., .., ..];
            var elapsed1 = DateTime.Now - start;

            _testOutputHelper.WriteLine($"Sequential write elapsed: {elapsed0}");
            _testOutputHelper.WriteLine($"Range read elapsed: {elapsed1}");

            counter = 0;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        Assert.True(rangeReadArray[i][j][k] == counter++);
                    }
            //Assert.Equal(rangeReadArray[i][j][k], counter++);

            npyFile.Dispose();
            npyFile.FileInfo.Delete();
        }

        public class StorageViewAccessor<T> : IDisposable, IEnumerable<T> where T : struct
        {
            MemoryMappedFile mappedFile;
            MemoryMappedViewAccessor accesor;
            long elementSize;
            long numberOfElements;

            public StorageViewAccessor(string filePath, long elementCount)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException();
                }

                FileInfo info = new FileInfo(filePath);

                elementSize = Marshal.SizeOf(typeof(T));

                if (info.Exists == false)
                    using (var fs = new FileStream(info.FullName, FileMode.CreateNew))
                    {
                        fs.Seek((elementCount * elementSize) - 1, SeekOrigin.Begin);
                        fs.WriteByte(0);
                        fs.Close();
                    }
                info.Refresh();
                mappedFile = MemoryMappedFile.CreateFromFile(filePath);
                accesor = mappedFile.CreateViewAccessor(0, info.Length);
                numberOfElements = info.Length / elementSize;
            }

            public long Length
            {
                get
                {
                    return numberOfElements;
                }
            }

            public T this[long index]
            {
                get
                {
                    if (index < 0 || index > numberOfElements)
                        throw new ArgumentOutOfRangeException();

                    T value = default(T);
                    accesor.Read<T>(index * elementSize, out value);
                    return value;
                }
                set
                {
                    accesor.Write<T>(index * elementSize, ref value);
                }
            }

            public T[] this[Range range]
            {
                get
                {
                    int i_0 = (int)(range.Start.IsFromEnd == false ? range.Start.Value : numberOfElements - range.Start.Value);
                    int i_n = (int)(range.End.IsFromEnd == false ? range.End.Value : numberOfElements - range.End.Value);
                    int i_count = i_n - i_0;

                    var buffer = new T[i_count];
                    accesor.ReadArray<T>(i_0 * elementSize, buffer, i_0, i_count);
                    return buffer;
                }
                set
                {
                    int i_0 = (int)(range.Start.IsFromEnd == false ? range.Start.Value : numberOfElements - range.Start.Value);
                    int i_n = (int)(range.End.IsFromEnd == false ? range.End.Value : numberOfElements - range.End.Value);
                    int i_count = i_n - i_0;
                    accesor.WriteArray<T>(i_0 * elementSize, value, i_0, i_count);
                }
            }

            public void Dispose()
            {
                if (accesor != null)
                {
                    accesor.Dispose();
                    accesor = null;
                }

                if (mappedFile != null)
                {
                    mappedFile.Dispose();
                    mappedFile = null;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                T value;
                for (int index = 0; index < numberOfElements; index++)
                {
                    value = default(T);
                    accesor.Read<T>(index * elementSize, out value);
                    yield return value;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                T value;
                for (int index = 0; index < numberOfElements; index++)
                {
                    value = default(T);
                    accesor.Read<T>(index * elementSize, out value);
                    yield return value;
                }
            }

            public static T[] GetArray(string filePath)
            {
                T[] elements;
                int elementSize;
                long numberOfElements;

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException();
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException();
                }

                FileInfo info = new FileInfo(filePath);
                using (MemoryMappedFile mappedFile = MemoryMappedFile.CreateFromFile(filePath))
                {
                    using (MemoryMappedViewAccessor accesor = mappedFile.CreateViewAccessor(0, info.Length))
                    {
                        elementSize = Marshal.SizeOf(typeof(T));
                        numberOfElements = info.Length / elementSize;
                        elements = new T[numberOfElements];

                        if (numberOfElements > int.MaxValue)
                        {
                            //you will need to split the array
                        }
                        else
                        {
                            accesor.ReadArray<T>(0, elements, 0, (int)numberOfElements);
                        }
                    }
                }

                return elements;
            }
        }

        public unsafe class Array1DViewStream<T> : IDisposable where T : unmanaged
        {
            FileInfo fileInfo;
            MemoryMappedFile mappedFile;
            MemoryMappedViewStream stream;
            int elementSize;
            long numberOfElements;
            byte[] buffer = new byte[BaseTwoPower.Thirty];
            T[] objectBuffer = new T[BaseTwoPower.Eighteen];

            byte* memPtr;
            private bool disposed;

            public Array1DViewStream(string filePath, long elementCount)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException();
                }

                fileInfo = new FileInfo(filePath);

                elementSize = Marshal.SizeOf(typeof(T));

                if (fileInfo.Exists == false)
                    using (var fs = new FileStream(fileInfo.FullName, FileMode.CreateNew))
                    {
                        fs.Seek((elementCount * elementSize) - 1, SeekOrigin.Begin);
                        fs.WriteByte(0);
                        fs.Close();
                    }
                fileInfo.Refresh();
                mappedFile = MemoryMappedFile.CreateFromFile(filePath);

                stream = mappedFile.CreateViewStream(0, fileInfo.Length);

                memPtr = (byte*)0;

                stream.SafeMemoryMappedViewHandle.AcquirePointer(ref memPtr);

                numberOfElements = fileInfo.Length / elementSize;
            }

            public FileInfo FileInfo => fileInfo;


            public long Length => numberOfElements;

            public T this[long index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return *(T*)(memPtr + (index * elementSize)); }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set { *(T*)(memPtr + (index * elementSize)) = value; }
            }

            public T[] this[Range range]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    int i_0 = (int)(range.Start.IsFromEnd == false ? range.Start.Value : numberOfElements - range.Start.Value);
                    int i_n = (int)(range.End.IsFromEnd == false ? range.End.Value : numberOfElements - range.End.Value);
                    int i_count = i_n - i_0;

                    var objBuffer = new T[i_count];
                    var byteCount = i_count * elementSize;

                    for (int i = 0; i < i_count; i++)
                        objBuffer[i] = this[i];

                    return objBuffer;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    int i_0 = (int)(range.Start.IsFromEnd == false ? range.Start.Value : numberOfElements - range.Start.Value);
                    int i_n = (int)(range.End.IsFromEnd == false ? range.End.Value : numberOfElements - range.End.Value);
                    int i_count = i_n - i_0;
                    var byteCount = i_count * elementSize;
                    for (int i = i_0; i < i_n; i++)
                    {
                        this[i] = value[i - i_0];
                    }
                }
            }

            public void Flush()
            {
                stream.Flush();
            }

            /// <param name="offset">offset of the stream in bytes to start the read</param>
            /// <param name="num">number of bytes from data to write to stream</param>
            /// <param name="buffer">buffer to fill</param>
            public unsafe void ReadBytes(byte[] buffer, long offset, int num)
            {
                IntPtr pointer = new IntPtr(new IntPtr(memPtr).ToInt64() + offset);
                Marshal.Copy(pointer, buffer, 0, num);
            }

            /// <param name="offset">offset of the stream in bytes to start the write</param>
            /// <param name="data">data to write</param>
            /// <param name="num">number of bytes from data to write to stream</param>
            public unsafe void WriteBytes(byte[] buffer, long offset, int num)
            {
                IntPtr pointer = new IntPtr(new IntPtr(memPtr).ToInt64() + offset);
                Marshal.Copy(buffer, 0, pointer, num);
            }

            #region Disposable

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposed) return;
                stream.SafeMemoryMappedViewHandle.ReleasePointer();
                if (disposing)
                {
                    stream.Dispose();
                    mappedFile.Dispose();
                }
                disposed = true;
            }

            ~Array1DViewStream()
            {
                Dispose(false);
            }

            #endregion
        }

        [Theory]
        [InlineData(268435455)]
        [InlineData(536870910)]
        public void SuperLongMMapFileAccess(long ni)
        {
            var npyImplFileInfo = new FileInfo($"ndarray_impl_double_{ni}___{ShortGuid.NewGuid()}.npy");
            var storageImpl2FileInfo = new FileInfo($"ndarray_impl_2_double_{ni}___{ShortGuid.NewGuid()}.npy");
            var npyArrayFile = NpyFileMemmap<float>.Create(npyImplFileInfo, ni);
            var storageArr2 = new Array1DViewStream<float>(storageImpl2FileInfo.FullName, ni);

            Stopwatch sw = Stopwatch.StartNew();
            for (long i = 0; i < ni; i++)
                storageArr2[i] = i;
            _testOutputHelper.WriteLine($"mmap sequential write time: {sw.Elapsed}");

            sw.Restart();

            float[] resultMmp2 = new float[ni];
            sw.Restart();
            for (long i = 0; i < ni; i++)
                resultMmp2[i] = storageArr2[i];
            _testOutputHelper.WriteLine($"mmap sequential read time: {sw.Elapsed.ToString("c")}");

            sw.Restart();
            float[] result0 = storageArr2[..];//.Get(.., result0);
            _testOutputHelper.WriteLine($"mmap Range read time: {sw.Elapsed.ToString("c")}");

            sw.Restart();
            //storageArr[^(int)(ni / 4)..^1] = result0;
            storageArr2[..] = result0;
            _testOutputHelper.WriteLine($"mmap Range write time: {sw.Elapsed.ToString("c")}");

            for (long i = 0; i < ni; i++)
            {
                Assert.Equal(resultMmp2[i], i);
            }

            sw.Restart();
            for (long i = 0; i < ni; i++)
                npyArrayFile[i] = i;
            _testOutputHelper.WriteLine($"npy sequential write time: {sw.Elapsed.ToString("c")}");

            sw.Restart();
            for (long i = 0; i < ni; i++)
                result0[i] = npyArrayFile[i];
            _testOutputHelper.WriteLine($"npy sequential read time: {sw.Elapsed.ToString("c")}");

            sw.Restart();
            //float[] result1 = npyArrayFile[^(int)(ni / 4)..^1];
            float[] result1 = npyArrayFile[..];
            _testOutputHelper.WriteLine($"npy Range read time: {sw.Elapsed.ToString("c")}");

            sw.Restart();
            //npyArrayFile[^(int)(ni / 4)..^1] = result1;
            npyArrayFile[..] = result1;
            _testOutputHelper.WriteLine($"npy Range write time: {sw.Elapsed.ToString("c")}");

            storageArr2.Dispose();
            storageArr2.FileInfo.Delete();
            npyArrayFile.Dispose();
            npyArrayFile.FileInfo.Delete();
        }

        [Theory]
        [InlineData(268435455)]
        public void CreateWriteRangeReadRange1DFloat(int ni)
        {
            var newImplFileInfo = new FileInfo($"new_impl_double_{ni}___{ShortGuid.NewGuid()}.npy");

            var newImplFile = NpyFileMemmap<float>.Create(newImplFileInfo, new int[] { ni }, true, false);

            var storageImplFileInfo = new FileInfo($"storage_impl_double_{ni}___{ShortGuid.NewGuid()}.npy");

            //var storageArr = new StorageViewAccessor<float>(storageImplFileInfo.FullName, ni);
            var storageArr = new Array1DViewStream<float>(storageImplFileInfo.FullName, ni);
            var newImplReadArray = new float[ni];

            // test write first to see if any caching improves performance
            for (int i = 0; i < ni; i++)
                newImplFile[i] = (float)(i);
            // test read first to see if any caching improves performance
            for (int i = 0; i < ni; i++)
                newImplReadArray[i] = newImplFile[i];

            /// Write data to files and perf test
            DateTime start = DateTime.Now;
            for (int i = 0; i < ni; i++)
                newImplFile[i] = (float)(i);
            var elapsedNewImpWrite = DateTime.Now - start;

            start = DateTime.Now;
            for (int i = 0; i < ni; i++)
                newImplReadArray[i] = newImplFile[i];
            var elapsedNewImplRead = DateTime.Now - start;

            // close file and reopen to refresh buffer (quick hack while changing implementations)
            newImplFile.Dispose();

            ////////////////////////////////////////////////////////////////
            start = DateTime.Now;
            var storageResult = storageArr[..];
            //var storageResult = storageArr[..];
            var elapsedStorageNewImplRangeRead = DateTime.Now - start;

            start = DateTime.Now;
            storageArr[..] = storageResult;
            var elapsedStorageNewImplRangeWrite = DateTime.Now - start;

            _testOutputHelper.WriteLine($"Memmory mapped data structure Range write: {elapsedStorageNewImplRangeWrite}");
            _testOutputHelper.WriteLine($"Memmory mapped data structure Range read: {elapsedStorageNewImplRangeRead}");

            //////////////////////////////////////////////////////////////////

            newImplFile = NpyFileMemmap<float>.Open(newImplFileInfo);

            start = DateTime.Now;
            var result = newImplFile[..];
            var elapsedNewImplRangeRead = DateTime.Now - start;

            start = DateTime.Now;
            newImplFile[..] = result;
            var elapsedNewImplRangeWrite = DateTime.Now - start;

            _testOutputHelper.WriteLine($"Sequential write: {elapsedNewImpWrite}");
            _testOutputHelper.WriteLine($"Sequential read: {elapsedNewImplRead}");
            _testOutputHelper.WriteLine($"Range write: {elapsedNewImplRangeWrite}");
            _testOutputHelper.WriteLine($"Range read: {elapsedNewImplRangeRead}");

            // Equality assertion
            for (int i = 0; i < ni; i++)
            {
                Assert.Equal(newImplReadArray[i], i);
                Assert.Equal(result[i], i);
            }

            newImplFile.Dispose();
            newImplFile.FileInfo.Delete();
        }

        [Theory]
        [InlineData(10, 20, 30)]
        [InlineData(100, 200, 300)]
        [InlineData(101, 201, 301)]
        [InlineData(1002, 110, 292)]
        [InlineData(1002, 1100, 992)]
        [InlineData(1200, 1201, 1202)]
        public void CompareImplementationsRead(int ni, int nj, int nk)
        {
            var newImplFileInfo = new FileInfo($"new_impl_double_{ni}_{nj}_{nk}___{ShortGuid.NewGuid()}.npy");

            var newImplFile = NpyFileMemmap<float>.Create(newImplFileInfo, new int[] { ni, nj, nk }, true, false);

            newImplFileInfo.Refresh();
            long initialFileSize = newImplFileInfo.Length;

            /// Write data to files and perf test
            DateTime start = DateTime.Now;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        newImplFile[i, j, k] = (float)((long)i * nj * nk + (long)j * nk + k);
            var elapsedNewImpWrite = DateTime.Now - start;

            var newImplReadArray = Jagged.Create<float>(ni, nj, nk);

            start = DateTime.Now;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        newImplReadArray[i][j][k] = newImplFile[i, j, k];
            var elapsedNewImplRead = DateTime.Now - start;

            // close file and reopen to refresh buffer (quick hack while changing implementations)
            newImplFile.Dispose();
            newImplFile.FileInfo.Refresh();

            Assert.Equal(initialFileSize, newImplFile.FileInfo.Length);

            newImplFile = NpyFileMemmap<float>.Open(newImplFileInfo);

            start = DateTime.Now;
            float[][][] result = newImplFile[.., .., ..];
            var elapsedNewImplRangeRead = DateTime.Now - start;

            start = DateTime.Now;
            newImplFile[.., .., ..] = result;
            var elapsedNewImplRangeWrite = DateTime.Now - start;

            _testOutputHelper.WriteLine($"Sequential write: {elapsedNewImpWrite}");
            _testOutputHelper.WriteLine($"Sequential read: {elapsedNewImplRead}");
            _testOutputHelper.WriteLine($"Range write: {elapsedNewImplRangeWrite}");
            _testOutputHelper.WriteLine($"Range read: {elapsedNewImplRangeRead}");

            // Equality assertion
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        var sequeReadValue = newImplReadArray[i][j][k];
                        var rangeReadValue = result[i][j][k];

                        Assert.Equal(newImplReadArray[i][j][k], (long)i * nj * nk + (long)j * nk + k);
                        Assert.Equal(result[i][j][k], (long)i * nj * nk + (long)j * nk + k);
                    }

            newImplFile.Dispose();
            newImplFile.FileInfo.Delete();
        }


        [Theory]
        [InlineData(10, 20, 30)]
        [InlineData(100, 200, 300)]
        [InlineData(101, 201, 301)]
        [InlineData(1002, 110, 292)]
        [InlineData(1002, 1100, 992)]
        [InlineData(1200, 1201, 1202)]
        public void CompareMemmapImplementationsRead(int ni, int nj, int nk)
        {
            var newImplFileInfo = new FileInfo($"new_impl_double_{ni}_{nj}_{nk}___{ShortGuid.NewGuid()}.npy");

            var newImplFile = NpyFileMemmap<float>.Create(newImplFileInfo, new int[] { ni, nj, nk }, true, false);

            newImplFileInfo.Refresh();
            long initialFileSize = newImplFileInfo.Length;

            /// Write data to files and perf test
            DateTime start = DateTime.Now;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        newImplFile[i, j, k] = (float)((long)i * nj * nk + (long)j * nk + k);
            var elapsedNewImpWrite = DateTime.Now - start;

            var newImplReadArray = Jagged.Create<float>(ni, nj, nk);

            start = DateTime.Now;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        newImplReadArray[i][j][k] = newImplFile[i, j, k];
            var elapsedNewImplRead = DateTime.Now - start;

            // close file and reopen to refresh buffer (quick hack while changing implementations)
            newImplFile.Dispose();
            newImplFile.FileInfo.Refresh();

            Assert.Equal(initialFileSize, newImplFile.FileInfo.Length);

            newImplFile = NpyFileMemmap<float>.Open(newImplFileInfo);

            start = DateTime.Now;
            float[][][] result = newImplFile[.., .., ..];
            var elapsedNewImplRangeRead = DateTime.Now - start;

            start = DateTime.Now;
            newImplFile[.., .., ..] = result;
            var elapsedNewImplRangeWrite = DateTime.Now - start;

            _testOutputHelper.WriteLine($"Sequential write: {elapsedNewImpWrite}");
            _testOutputHelper.WriteLine($"Sequential read: {elapsedNewImplRead}");
            _testOutputHelper.WriteLine($"Range write: {elapsedNewImplRangeWrite}");
            _testOutputHelper.WriteLine($"Range read: {elapsedNewImplRangeRead}");

            // Equality assertion
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        var sequeReadValue = newImplReadArray[i][j][k];
                        var rangeReadValue = result[i][j][k];

                        Assert.Equal(newImplReadArray[i][j][k], (long)i * nj * nk + (long)j * nk + k);
                        Assert.Equal(result[i][j][k], (long)i * nj * nk + (long)j * nk + k);
                    }

            newImplFile.Dispose();
            newImplFile.FileInfo.Delete();
        }

        [Theory]
        [InlineData(20, 12, 56)]
        public void CreateWriteSequentialRead2DSliceFrom3DDouble(int ni, int nj, int nk)
        {
            var testFileInfo = new FileInfo($"new_impl_double_{ni}_{nj}_{nk}___{ShortGuid.NewGuid()}.npy");
            var npyFile = NpyFileMemmap<double>.Create(testFileInfo, new int[] { ni, nj, nk });
            npyFile.FileInfo.Refresh();
            var initialFileSize = npyFile.FileInfo.Length;

            // Fill file with data
            var indices = new int[3];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        indices[0] = i; indices[1] = j; indices[2] = k;
                        npyFile[indices] = i + (i * j) + (i * j * k);
                    }
            npyFile.Dispose();
            npyFile.FileInfo.Refresh();

            Assert.Equal(initialFileSize, npyFile.FileInfo.Length);

            npyFile = NpyFileMemmap<double>.Open(testFileInfo);
            double[][] arr;

            //npyFile.Read(ni / 2, out arr, 0);
            arr = npyFile[ni / 2, .., ..];
            for (int j = 0; j < nj; j++)
                for (int k = 0; k < nk; k++)
                {
                    var value = (ni / 2) + ((ni / 2) * j) + ((ni / 2) * j * k);
                    Assert.True(Math.Abs(arr[j][k] - value) < 0.01f);
                }

            //npyFile.Read(nj / 2, out arr, 1);
            arr = npyFile[.., nj / 2, ..];
            for (int i = 0; i < ni; i++)
                for (int k = 0; k < nk; k++)
                {
                    var value = i + (i * (nj / 2)) + (i * (nj / 2) * k);
                    Assert.True(Math.Abs(arr[i][k] - value) < 0.01f);
                }

            npyFile.Read(nk / 2, out arr, 2);
            arr = npyFile[.., .., nk / 2];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                {
                    var value = i + (i * j) + (i * j * (nk / 2));
                    Assert.True(Math.Abs(arr[i][j] - value) < 0.01f);
                }

            npyFile.Dispose();
            npyFile.FileInfo.Refresh();
            Assert.Equal(initialFileSize, npyFile.FileInfo.Length);

            npyFile.FileInfo.Delete();
        }

        [Theory]
        [InlineData(20, 12, 56)]
        public void CreateWriteSequentialRead2DSliceFrom3DFloat(int ni, int nj, int nk)
        {
            bool isFortranOrder = false;
            var shape = new int[] { ni, nj, nk };
            var testFileInfo = new FileInfo($"test_slice_read_file___{ShortGuid.NewGuid()}.npy");
            var npyFile = NpyFileMemmap<float>.Create(testFileInfo, shape, true, isFortranOrder, true);

            Func<int, int, int, long> ravel3D = isFortranOrder ?
                (int0, int1, int2) => ((long)int2 * shape[1] * shape[0]) + ((long)int1 * shape[0]) + int0 :
                (int0, int1, int2) => ((long)int0 * shape[1] * shape[2]) + ((long)int1 * shape[2]) + int2;

            // Fill file with data
            var indices = new int[3];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                    {
                        indices[0] = i; indices[1] = j; indices[2] = k;
                        var flatIndex = ravel3D(i, j, k);
                        npyFile[indices] = flatIndex;
                    }
            npyFile.Dispose();

            npyFile = NpyFileMemmap<float>.Open(testFileInfo);
            float[][] arr;

            //npyFile.Read(ni / 2, out arr, 0);
            arr = npyFile[ni / 2, .., ..];
            for (int j = 0; j < nj; j++)
                for (int k = 0; k < nk; k++)
                {
                    var flatIndex = ravel3D(ni / 2, j, k);
                    Assert.True(Math.Abs(arr[j][k] - flatIndex) < 0.01f);
                }

            //npyFile.Read(nj / 2, out arr, 1);
            arr = npyFile[.., nj / 2, ..];
            for (int i = 0; i < ni; i++)
                for (int k = 0; k < nk; k++)
                {
                    var flatIndex = ravel3D(i, nj / 2, k);
                    Assert.True(Math.Abs(arr[i][k] - flatIndex) < 0.01f);
                }

            //npyFile.Read(nk / 2, out arr, 2);
            arr = npyFile[.., .., nk / 2];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                {
                    var flatIndex = ravel3D(i, j, nk / 2);
                    Assert.True(Math.Abs(arr[i][j] - flatIndex) < 0.01f);
                }
            npyFile.Dispose();
            npyFile.FileInfo.Delete();
        }

        [Theory]
        [InlineData(200)]
        public void CreateByteFile(int sampleCount)
        {
            bool isFortranOrder = false;
            var shape = new int[] { sampleCount };
            var testFileInfo = new FileInfo($"test_byte_read_write_file___{ShortGuid.NewGuid()}.npy");
            var npyFile = NpyFileMemmap<byte>.Create(testFileInfo, shape, true, isFortranOrder, true);

            Random rand = new Random();
            for (int i = 0; i < sampleCount; i++)
            {
                var sample = rand.Next(byte.MinValue, byte.MaxValue);
                npyFile[i] = (byte)sample;
            }


        }
    }
}