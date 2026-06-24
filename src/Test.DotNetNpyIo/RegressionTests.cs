using DotNetNpyIo;

namespace Test.DotNetNpyIo
{
    /// <summary>
    /// Regression and unit tests targeting bugs fixed during the comprehensive review and the
    /// newer API surface (Span access, strongly-typed factories). Many of these would have caught
    /// the original defects; they exist to keep them caught.
    /// </summary>
    public class RegressionTests
    {
        private static FileInfo TempFile(string hint) => new FileInfo($"reg_{hint}_{ShortGuid.NewGuid()}.npy");

        // ---------------------------------------------------------------------------------------
        // EndianBitConverter.ToDoubles / ToSingles with a non-zero startIndex (byte offset).
        // The original code conflated the loop counter, the element index and the byte offset, so
        // any startIndex > 0 produced wrong/short results. startIndex is a BYTE offset, consistent
        // with the scalar ToDouble(value, startIndex) / ToInt64(value, startIndex).
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void ToDoubles_HonoursByteStartIndex()
        {
            var values = new double[] { 1.5, -2.25, 3.125, 42.0, 100.0 };
            byte[] bytes = new byte[8 /*offset*/ + values.Length * 8];
            for (int i = 0; i < values.Length; i++)
                BitConverter.GetBytes(values[i]).CopyTo(bytes, 8 + i * 8);

            var conv = new LittleEndianBitConverter();
            double[] result = conv.ToDoubles(bytes, startIndex: 8, doublesCount: values.Length);

            Assert.Equal(values.Length, result.Length);
            Assert.Equal(values, result);
        }

        [Fact]
        public void ToSingles_HonoursByteStartIndex()
        {
            var values = new float[] { 1.5f, -2.25f, 3.125f, 42.0f };
            byte[] bytes = new byte[4 /*offset*/ + values.Length * 4];
            for (int i = 0; i < values.Length; i++)
                BitConverter.GetBytes(values[i]).CopyTo(bytes, 4 + i * 4);

            var conv = new LittleEndianBitConverter();
            float[] result = conv.ToSingles(bytes, startIndex: 4, singlesCount: values.Length);

            Assert.Equal(values.Length, result.Length);
            Assert.Equal(values, result);
        }

        // ---------------------------------------------------------------------------------------
        // IbmSingle comparison operators previously recursed infinitely (a == a -> a == a -> ...).
        // NOTE: values are chosen so the (separately buggy, pre-existing) IBM ctor conversion does
        // not hit its infinite loop — it spins forever for floats whose mantissa bits don't
        // intersect 0x00ffffff, e.g. exact powers of two like 2.0f. See review notes.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void IbmSingle_Operators_DoNotStackOverflow_AndAreConsistent()
        {
            var a = IbmSingle.FromIeee(1.0f);
            var a2 = IbmSingle.FromIeee(1.0f);
            var b = IbmSingle.FromIeee(1.5f);

            Assert.True(a == a2);
            Assert.False(a != a2);
            Assert.True(a != b);
            Assert.True(a < b);
            Assert.True(b > a);
            Assert.True(a <= a2);
            Assert.True(a >= a2);

            // 2.0f's mantissa bits don't intersect 0x00ffffff, which used to spin the ctor's
            // normalisation loop forever. The guard makes the call return (mapped to zero) instead
            // of hanging the process; simply reaching this line proves the loop terminates.
            var pow2 = IbmSingle.FromIeee(2.0f);
            Assert.True(pow2 == pow2);
        }

        // ---------------------------------------------------------------------------------------
        // BigArray.Resize: grow/shrink within a block and the resize-to-zero guard. (Multi-block
        // resize cannot be exercised here because a single block spans ~2GB; see review notes.)
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void BigArray_Resize_PreservesData_AndHandlesZero()
        {
            var arr = new BigArray<int>(10);
            for (int i = 0; i < 10; i++) arr[i] = i * 7;

            arr.Resize(20);
            Assert.Equal(20, arr.Length);
            for (int i = 0; i < 10; i++) Assert.Equal(i * 7, arr[i]); // old data preserved

            arr.Resize(5);
            Assert.Equal(5, arr.Length);
            for (int i = 0; i < 5; i++) Assert.Equal(i * 7, arr[i]);

            arr.Resize(0);
            Assert.Equal(0, arr.Length);
        }

        // ---------------------------------------------------------------------------------------
        // Fortran-order 3D round-trip. Exercises the memmap range-read fortran branch whose loop
        // bound was 'k < i_count' instead of 'k < k_count'. Non-cube shape so the bug would surface.
        // ---------------------------------------------------------------------------------------
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Memmap_3D_RangeRead_MatchesScalar_AnyOrder(bool isFortranOrder)
        {
            int ni = 3, nj = 4, nk = 5;
            var fi = TempFile($"memmap_f{isFortranOrder}");
            var file = NpyFileMemmap<int>.Create(fi, new[] { ni, nj, nk }, isLittleEndian: true, isFortranOrder: isFortranOrder);

            int counter = 0;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        file[i, j, k] = counter++;

            int[][][] range = file[.., .., ..];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        Assert.Equal(file[i, j, k], range[i][j][k]);

            file.Dispose();
            fi.Delete();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Buffered_3D_RangeRead_MatchesScalar_AnyOrder(bool isFortranOrder)
        {
            int ni = 3, nj = 4, nk = 5;
            var fi = TempFile($"buf_f{isFortranOrder}");
            var file = NpyFileBuffered<int>.Create(fi, new[] { ni, nj, nk }, isLittleEndian: true, isFortranOrder: isFortranOrder);

            int counter = 0;
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        file[i, j, k] = counter++;

            int[][][] range = file[.., .., ..];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                    for (int k = 0; k < nk; k++)
                        Assert.Equal(file[i, j, k], range[i][j][k]);

            file.Dispose();
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // Big-endian round-trip via the buffered implementation (which honours byte order through
        // the Big/LittleEndianBinaryReader/Writer). Also validates the on-disk descr is '>'.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void Buffered_BigEndian_RoundTrips()
        {
            var fi = TempFile("be");
            var file = NpyFileBuffered<float>.Create(fi, new[] { 64 }, isLittleEndian: false);
            Assert.Equal(Endianness.BigEndian, file.Endianess);

            for (int i = 0; i < 64; i++) file[i] = i * 1.5f;
            file.Dispose();

            // On-disk header must advertise big-endian ('>f4').
            using (var fs = fi.OpenRead())
            {
                var head = new byte[128];
                fs.Read(head, 0, 128);
                string headerStr = System.Text.Encoding.ASCII.GetString(head);
                Assert.Contains(">f4", headerStr);
            }

            var reopened = NpyFileBuffered<float>.Open(fi);
            for (int i = 0; i < 64; i++) Assert.Equal(i * 1.5f, reopened[i]);
            reopened.Dispose();
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // 1-D numpy header must use the trailing-comma tuple "(N,)" to be numpy-compatible, and
        // must round-trip through Open.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void OneDimensional_Header_IsNumpyCompatible_AndRoundTrips()
        {
            var fi = TempFile("1d");
            var file = NpyFile.Create<double>(fi.FullName, new[] { 5 });
            for (int i = 0; i < 5; i++) file[i] = i + 0.5;
            file.Dispose();

            using (var fs = fi.OpenRead())
            {
                var head = new byte[64];
                fs.Read(head, 0, 64);
                string headerStr = System.Text.Encoding.ASCII.GetString(head);
                Assert.Contains("'shape': (5,)", headerStr);
                Assert.Contains("'descr': '<f8'", headerStr);
            }

            var reopened = NpyFile.Open<double>(fi.FullName);
            Assert.Single(reopened.Shape);
            Assert.Equal(5, reopened.Shape[0]);
            for (int i = 0; i < 5; i++) Assert.Equal(i + 0.5, reopened[i]);
            reopened.Dispose();
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // Zero-copy Span<T> access over the memory-mapped region: writes through the span are
        // visible through the indexer, and bounds are enforced.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void Memmap_AsSpan_IsZeroCopy_AndBoundsChecked()
        {
            var fi = TempFile("span");
            var file = NpyFile.Create<float>(fi.FullName, new[] { 16 });

            Span<float> span = file.AsSpan();
            Assert.Equal(16, span.Length);
            for (int i = 0; i < span.Length; i++) span[i] = i * 2.0f;

            // Mutations through the span are visible via the indexer (same memory).
            for (int i = 0; i < 16; i++) Assert.Equal(i * 2.0f, file[i]);

            // Windowed span.
            Span<float> window = file.AsSpan(4, 4);
            window[0] = 999f;
            Assert.Equal(999f, file[4]);

            Assert.Throws<ArgumentOutOfRangeException>(() => file.AsSpan(14, 4)); // 14+4 > 16
            Assert.Throws<ArgumentOutOfRangeException>(() => file.AsSpan(-1, 2));

            file.Dispose();
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // Indexer bounds checking on the raw flattened accessors (previously an unchecked native
        // pointer dereference -> access violation / silent corruption).
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void Memmap_FlattenedIndexer_RejectsOutOfBounds()
        {
            var fi = TempFile("bounds");
            var file = NpyFile.Create<int>(fi.FullName, new[] { 8 });

            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = file[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = file[8]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = file[100L]; });

            file.Dispose();
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // Half (f2) support: readable + writable + validated.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void Half_RoundTrips()
        {
            var fi = TempFile("half");
            var file = NpyFile.Create<Half>(fi.FullName, new[] { 4 });
            var values = new[] { (Half)1.5, (Half)(-2.5), (Half)0.25, (Half)100.0 };
            for (int i = 0; i < values.Length; i++) file[i] = values[i];
            file.Dispose();

            var reopened = NpyFile.Open<Half>(fi.FullName);
            for (int i = 0; i < values.Length; i++) Assert.Equal(values[i], reopened[i]);
            reopened.Dispose();
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // Opening a file with a generic type that disagrees with the stored dtype must throw.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void Open_WithWrongType_Throws()
        {
            var fi = TempFile("wrongtype");
            var file = NpyFile.Create<float>(fi.FullName, new[] { 4 });
            file.Dispose();

            Assert.ThrowsAny<Exception>(() => NpyFile.Open<int>(fi.FullName));
            fi.Delete();
        }

        // ---------------------------------------------------------------------------------------
        // Dispose must release handles so the file can be deleted/reopened immediately afterwards.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void Memmap_Dispose_ReleasesHandles()
        {
            var fi = TempFile("dispose");
            var file = NpyFile.Create<int>(fi.FullName, new[] { 4 });
            file[0] = 123;
            file.Dispose();

            // If the mmap handle leaked, reopening / deleting would fail on Windows.
            var reopened = NpyFile.Open<int>(fi.FullName);
            Assert.Equal(123, reopened[0]);
            reopened.Dispose();
            fi.Delete();
            Assert.False(File.Exists(fi.FullName));
        }

        // ---------------------------------------------------------------------------------------
        // Big-endian binary writer/reader array round-trip across every primitive type. This is
        // the path where the hand-rolled byte-shuffle bugs lived (e.g. Write(double[]) overrun);
        // it locks behaviour while those loops are replaced with BinaryPrimitives.
        // ---------------------------------------------------------------------------------------
        [Fact]
        public void BigEndian_Array_RoundTrips_AllPrimitiveTypes()
        {
            var i16 = new short[] { -5, 0, 1, short.MaxValue, short.MinValue };
            var u16 = new ushort[] { 0, 1, 1000, ushort.MaxValue };
            var i32 = new int[] { -5, 0, 1, int.MaxValue, int.MinValue };
            var u32 = new uint[] { 0, 1, 1000, uint.MaxValue };
            var i64 = new long[] { -5, 0, 1, long.MaxValue, long.MinValue };
            var u64 = new ulong[] { 0, 1, 1000, ulong.MaxValue };
            var f32 = new float[] { -1.5f, 0f, 3.25f, float.MaxValue, float.MinValue };
            var f64 = new double[] { -1.5, 0, 3.25, double.MaxValue, double.MinValue };

            using var ms = new MemoryStream();
            var w = new BigEndianBinaryWriter(ms);
            w.Write(i16); w.Write(u16); w.Write(i32); w.Write(u32);
            w.Write(i64); w.Write(u64); w.Write(f32); w.Write(f64);

            ms.Position = 0;
            var r = new BigEndianBinaryReader(ms);
            Assert.Equal(i16, r.ReadInt16s(i16.Length));
            Assert.Equal(u16, r.ReadUInt16s(u16.Length));
            Assert.Equal(i32, r.ReadInt32s(i32.Length));
            Assert.Equal(u32, r.ReadUInt32s(u32.Length));
            Assert.Equal(i64, r.ReadInt64s(i64.Length));
            Assert.Equal(u64, r.ReadUInt64s(u64.Length));
            Assert.Equal(f32, r.ReadSingles(f32.Length));
            Assert.Equal(f64, r.ReadDoubles(f64.Length));
        }
    }
}
