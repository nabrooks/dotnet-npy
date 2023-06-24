using DotNetNpyIo;
using Xunit.Abstractions;

namespace Test.DotNetNpyIo
{
    public class SerializationTests
    {
        private ITestOutputHelper _testOutputHelper;

        public SerializationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void BytesToFloatSerialization()
        {
            var lilEndianFloatSerializer = new FloatSerializer(true);
            var bigEndianFloatSerializer = new FloatSerializer(false);

            List<float> floatList = new List<float>();
            for (float f = -200; f < 200; f += 0.12f)
            {
                floatList.Add(f);
                var lilEndianBytesLoop = BitConverter.GetBytes(f);
                var bigEndianBytesLoop = new byte[] { lilEndianBytesLoop[3], lilEndianBytesLoop[2], lilEndianBytesLoop[1], lilEndianBytesLoop[0] };

                var vFromLilEndian = lilEndianFloatSerializer.Deserialize(lilEndianBytesLoop);
                var vFromBigEndian = bigEndianFloatSerializer.Deserialize(bigEndianBytesLoop);

                Assert.Equal(vFromLilEndian, f);
                Assert.Equal(vFromBigEndian, f);
            }

            float[] ieeefloats = floatList.ToArray();
            byte[] lilEndianBytes = lilEndianFloatSerializer.Serialize(ieeefloats);
            byte[] bigEndianBytes = bigEndianFloatSerializer.Serialize(ieeefloats);

            // Conversion from byte[] to Ieee754 float[]
            DateTime startTime = DateTime.Now;
            float[] defaultFloats = new float[ieeefloats.Length];
            Buffer.BlockCopy(lilEndianBytes, 0, defaultFloats, 0, lilEndianBytes.Length);
            DateTime endTime = DateTime.Now;
            var processTime1 = endTime - startTime;
            _testOutputHelper.WriteLine($"byte[] to ieee745 float[] conversion via .net block copy elapsed time: {processTime1}");

            // Conversion from byte[] to Ieee754 float[] via new bigEndianSerializer
            startTime = DateTime.Now;
            var bigEFloats = bigEndianFloatSerializer.Deserialize(bigEndianBytes, 0, ieeefloats.Length);
            endTime = DateTime.Now;
            var processTime2 = endTime - startTime;
            _testOutputHelper.WriteLine($"byte[] to ieee745 float[] conversion via BigEndianSerializer elapsed time: {processTime2}");

            // Conversion from byte[] to Ieee754 float[] via new lilEndianSerializer
            startTime = DateTime.Now;
            var lilEFloats = lilEndianFloatSerializer.Deserialize(lilEndianBytes, 0, ieeefloats.Length);
            endTime = DateTime.Now;
            var processTime3 = endTime - startTime;
            _testOutputHelper.WriteLine($"byte[] to ieee745 float[] conversion via LilEndianSerializer elapsed time: {processTime3}");

            for (int i = 0; i < ieeefloats.Length; i++)
            {
                Assert.True(defaultFloats[i] == bigEFloats[i] && defaultFloats[i] == lilEFloats[i]);
            }

            IbmLittleEndianBitConverter ibmLittleEndianConverter = new IbmLittleEndianBitConverter();
            IbmBigEndianBitConverter ibmBigEndianConverter = new IbmBigEndianBitConverter();
            LittleEndianBitConverter ieeelittleEndianConverter = new LittleEndianBitConverter();

            for (int i = 0; i < ieeefloats.Length; i++)
            {
                byte[] dataLilEndian = new byte[] { lilEndianBytes[i], lilEndianBytes[i + 1], lilEndianBytes[i + 2], lilEndianBytes[i + 3] };
                byte[] dataBigEndian = new byte[] { lilEndianBytes[i + 3], lilEndianBytes[i + 2], lilEndianBytes[i + 1], lilEndianBytes[i] };

                var ieeeFloat = ieeefloats[i];
                var ieeefloatArray = new float[] { ieeeFloat };

                DateTime start = DateTime.Now;
                var aResult = IbmConverter.ibm_to_float(ieeefloatArray);
                TimeSpan aProcessElapsed = DateTime.Now - start;
                var a = aResult[0];

                start = DateTime.Now;
                var b = BitConverter.ToSingle(dataLilEndian, 0);
                TimeSpan bProcessElapsed = DateTime.Now - start;

                start = DateTime.Now;
                var c = ieeelittleEndianConverter.ToSingle(dataLilEndian, 0);
                TimeSpan cProcessElapsed = DateTime.Now - start;

                start = DateTime.Now;
                var d = ibmBigEndianConverter.ToSingle(dataBigEndian, 0);
                TimeSpan dProcessElapsed = DateTime.Now - start;

                start = DateTime.Now;
                var e = ibmLittleEndianConverter.ToSingle(dataLilEndian, 0);
                TimeSpan eProcessElapsed = DateTime.Now - start;
            }

            var start2 = DateTime.Now;
            float[] tmpIeeeFloats = new float[ieeefloats.Length];
            Buffer.BlockCopy(ieeefloats, 0, tmpIeeeFloats, 0, ieeefloats.Length);
            var tResult = IbmConverter.ibm_to_float(ieeefloats);
            var elapsed2 = DateTime.Now - start2;
            _testOutputHelper.WriteLine($"ieee745 float[] to ibm365 float[] conversion via IbmConverter.ibm_to_float elapsed time: {elapsed2}");

            var bigEFloats2 = lilEndianFloatSerializer.Deserialize(bigEndianBytes, 0, ieeefloats.Length);
            var bigEFloats3 = lilEndianFloatSerializer.Deserialize(bigEndianBytes, 0, ieeefloats.Length);
            var bigEFloats4 = lilEndianFloatSerializer.Deserialize(bigEndianBytes, 0, ieeefloats.Length);

            start2 = DateTime.Now;
            var r = IbmConverter.ibm_to_float(bigEFloats4);
            var elapsed = DateTime.Now - start2;

            start2 = DateTime.Now;
            float[] floats = new float[bigEndianBytes.Length / 4];
            Buffer.BlockCopy(bigEndianBytes, 0, floats, 0, bigEndianBytes.Length);
            IbmConverter.ibm_to_float(floats, floats, floats.Length);
            var elapsed6 = DateTime.Now - start2;
            _testOutputHelper.WriteLine($"ieee745 float[] to ibm365 float[] conversion via IbmConverter.ibm_to_float (inplace) elapsed time: {elapsed6}");

            start2 = DateTime.Now;
            var res = ibmBigEndianConverter.ToSingles(bigEndianBytes, 0, bigEFloats.Length);// (bigEFloats2, bigEFloats2);
            var elapsed3 = DateTime.Now - start2;
            _testOutputHelper.WriteLine($"ieee745 float[] to ibm365 float[] conversion via IbmBigEndianBitConverter.ToSingles elapsed time: {elapsed3}");

            start2 = DateTime.Now;
            ibmBigEndianConverter.IbmToIeeeMethod(bigEFloats3, bigEFloats3);
            var elapsed5 = DateTime.Now - start2;
            _testOutputHelper.WriteLine($"ieee745 float[] to ibm365 float[] conversion via IbmBigEndianBitConverter.IbmToIeeeMethod (inplace) elapsed time: {elapsed5}");
        }

        [Fact]
        public static void BigEndianBinaryWriterQualityTest()
        {
            FileStream bigEInt16FileStream = File.Open("big_endian_int16_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEInt32FileStream = File.Open("big_endian_int32_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEInt64FileStream = File.Open("big_endian_int64_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEUInt16FileStream = File.Open("big_endian_uint16_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEUInt32FileStream = File.Open("big_endian_uint32_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEUInt64FileStream = File.Open("big_endian_uint64_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEFloat32FileStream = File.Open("big_endian_float32_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream bigEFloat64FileStream = File.Open("big_endian_float64_values_quality_1.bin", FileMode.Create, FileAccess.ReadWrite);

            BigEndianBinaryWriter bigEInt16Writer = new BigEndianBinaryWriter(bigEInt16FileStream);
            BigEndianBinaryWriter bigEInt32Writer = new BigEndianBinaryWriter(bigEInt32FileStream);
            BigEndianBinaryWriter bigEInt64Writer = new BigEndianBinaryWriter(bigEInt64FileStream);
            BigEndianBinaryWriter bigEUInt16Writer = new BigEndianBinaryWriter(bigEUInt16FileStream);
            BigEndianBinaryWriter bigEUInt32Writer = new BigEndianBinaryWriter(bigEUInt32FileStream);
            BigEndianBinaryWriter bigEUInt64Writer = new BigEndianBinaryWriter(bigEUInt64FileStream);
            BigEndianBinaryWriter bigEFloat32Writer = new BigEndianBinaryWriter(bigEFloat32FileStream);
            BigEndianBinaryWriter bigEFloat64Writer = new BigEndianBinaryWriter(bigEFloat64FileStream);

            var floatCount = 0;
            for (int i = -2000; i < 2000; i++)
            {
                bigEInt16Writer.Write((short)i);
                bigEInt32Writer.Write((int)i);
                bigEInt64Writer.Write((long)i);
                bigEUInt16Writer.Write((ushort)Math.Abs(i));
                bigEUInt32Writer.Write((uint)Math.Abs(i));
                bigEUInt64Writer.Write((ulong)Math.Abs(i));
                bigEFloat32Writer.Write((float)i);
                bigEFloat64Writer.Write((double)i);
                floatCount++;
            }

            bigEInt16FileStream.Seek(0, SeekOrigin.Begin);
            bigEInt32FileStream.Seek(0, SeekOrigin.Begin);
            bigEInt64FileStream.Seek(0, SeekOrigin.Begin);
            bigEUInt16FileStream.Seek(0, SeekOrigin.Begin);
            bigEUInt32FileStream.Seek(0, SeekOrigin.Begin);
            bigEUInt64FileStream.Seek(0, SeekOrigin.Begin);
            bigEFloat32FileStream.Seek(0, SeekOrigin.Begin);
            bigEFloat64FileStream.Seek(0, SeekOrigin.Begin);

            BigEndianBinaryReader bigEInt16Reader = new BigEndianBinaryReader(bigEInt16FileStream);
            BigEndianBinaryReader bigEInt32Reader = new BigEndianBinaryReader(bigEInt32FileStream);
            BigEndianBinaryReader bigEInt64Reader = new BigEndianBinaryReader(bigEInt64FileStream);
            BigEndianBinaryReader bigEUInt16Reader = new BigEndianBinaryReader(bigEUInt16FileStream);
            BigEndianBinaryReader bigEUInt32Reader = new BigEndianBinaryReader(bigEUInt32FileStream);
            BigEndianBinaryReader bigEUInt64Reader = new BigEndianBinaryReader(bigEUInt64FileStream);
            BigEndianBinaryReader bigEFloat32Reader = new BigEndianBinaryReader(bigEFloat32FileStream);
            BigEndianBinaryReader bigEFloat64Reader = new BigEndianBinaryReader(bigEFloat64FileStream);

            var valInt16 = bigEInt16Reader.ReadInt16s(floatCount);
            var valInt32 = bigEInt32Reader.ReadInt32s(floatCount);
            var valInt64 = bigEInt64Reader.ReadInt64s(floatCount);
            var valUInt16 = bigEUInt16Reader.ReadUInt16s(floatCount);
            var valUInt32 = bigEUInt32Reader.ReadUInt32s(floatCount);
            var valUInt64 = bigEUInt64Reader.ReadUInt64s(floatCount);
            var valFloat32 = bigEFloat32Reader.ReadSingles(floatCount);
            var valFloat64 = bigEFloat64Reader.ReadDoubles(floatCount);

            Assert.True(valInt16.Length == floatCount);
            Assert.True(valInt32.Length == floatCount);
            Assert.True(valInt64.Length == floatCount);
            Assert.True(valUInt16.Length == floatCount);
            Assert.True(valUInt32.Length == floatCount);
            Assert.True(valUInt64.Length == floatCount);
            Assert.True(valFloat32.Length == floatCount);
            Assert.True(valFloat64.Length == floatCount);

            for (int i = 0; i < floatCount; i++)
            {
                Assert.True(valInt16[i] == i - 2000);
                Assert.True(valInt16[i] == valInt32[i]);
                Assert.True(valInt16[i] == valInt64[i]);
                Assert.True(valUInt16[i] == valUInt32[i]);
                Assert.True(valUInt16[i] == valUInt64[i]);
                Assert.True(valUInt16[i] == Math.Abs(valInt16[i]));
                Assert.True(valUInt32[i] == Math.Abs(valInt32[i]));
                Assert.True((long)valUInt64[i] == Math.Abs(valInt64[i]));
                Assert.True(valFloat32[i] == valFloat64[i]);
                Assert.True(valFloat32[i] == valInt32[i]);
            }
            bigEInt16FileStream.Close();
            bigEInt32FileStream.Close();
            bigEInt64FileStream.Close();
            bigEUInt16FileStream.Close();
            bigEUInt32FileStream.Close();
            bigEUInt64FileStream.Close();
            bigEFloat32FileStream.Close();
            bigEFloat64FileStream.Close();
            File.Delete(bigEInt16FileStream.Name);
            File.Delete(bigEInt32FileStream.Name);
            File.Delete(bigEInt64FileStream.Name);
            File.Delete(bigEUInt16FileStream.Name);
            File.Delete(bigEUInt32FileStream.Name);
            File.Delete(bigEUInt64FileStream.Name);
            File.Delete(bigEFloat32FileStream.Name);
            File.Delete(bigEFloat64FileStream.Name);
        }

        [Fact]
        public static void LilEndianBinaryWriterExplicitQualityTest()
        {
            FileStream lilEInt16FileStream = File.Open("lil_endian_int16_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEInt32FileStream = File.Open("lil_endian_int32_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEInt64FileStream = File.Open("lil_endian_int64_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEUInt16FileStream = File.Open("lil_endian_uint16_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEUInt32FileStream = File.Open("lil_endian_uint32_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEUInt64FileStream = File.Open("lil_endian_uint64_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEFloat32FileStream = File.Open("lil_endian_float32_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEFloat64FileStream = File.Open("lil_endian_float64_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEIbmFloat32FileStream = File.Open("lil_endian_ibm_float32_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEIbmFloat64FileStream = File.Open("lil_endian_ibm_float64_values_quality_2.bin", FileMode.Create, FileAccess.ReadWrite);

            LittleEndianBinaryWriter lilEInt16Writer = new LittleEndianBinaryWriter(lilEInt16FileStream);
            LittleEndianBinaryWriter lilEInt32Writer = new LittleEndianBinaryWriter(lilEInt32FileStream);
            LittleEndianBinaryWriter lilEInt64Writer = new LittleEndianBinaryWriter(lilEInt64FileStream);
            LittleEndianBinaryWriter lilEUInt16Writer = new LittleEndianBinaryWriter(lilEUInt16FileStream);
            LittleEndianBinaryWriter lilEUInt32Writer = new LittleEndianBinaryWriter(lilEUInt32FileStream);
            LittleEndianBinaryWriter lilEUInt64Writer = new LittleEndianBinaryWriter(lilEUInt64FileStream);
            LittleEndianBinaryWriter lilEFloat32Writer = new LittleEndianBinaryWriter(lilEFloat32FileStream);
            LittleEndianBinaryWriter lilEFloat64Writer = new LittleEndianBinaryWriter(lilEFloat64FileStream);
            LittleEndianBinaryWriter lilEIbmFloat32Writer = new LittleEndianBinaryWriter(lilEIbmFloat32FileStream);
            LittleEndianBinaryWriter lilEIbmFloat64Writer = new LittleEndianBinaryWriter(lilEIbmFloat64FileStream);

            float[] values = new float[4000];

            var floatCount = 0;
            for (int i = -2000; i < 2000; i++)
            {
                lilEInt16Writer.Write((short)i);
                lilEInt32Writer.Write((int)i);
                lilEInt64Writer.Write((long)i);
                lilEUInt16Writer.Write((ushort)Math.Abs(i));
                lilEUInt32Writer.Write((uint)Math.Abs(i));
                lilEUInt64Writer.Write((ulong)Math.Abs(i));
                lilEFloat32Writer.Write((float)i);
                lilEFloat64Writer.Write((double)i);
                lilEIbmFloat32Writer.WriteIbm((float)i);
                values[i + 2000] = (float)i;
                floatCount++;
            }

            //lilEIbmFloat32Writer.WriteIbm(values);

            lilEInt16FileStream.Seek(0, SeekOrigin.Begin);
            lilEInt32FileStream.Seek(0, SeekOrigin.Begin);
            lilEInt64FileStream.Seek(0, SeekOrigin.Begin);
            lilEUInt16FileStream.Seek(0, SeekOrigin.Begin);
            lilEUInt32FileStream.Seek(0, SeekOrigin.Begin);
            lilEUInt64FileStream.Seek(0, SeekOrigin.Begin);
            lilEFloat32FileStream.Seek(0, SeekOrigin.Begin);
            lilEFloat64FileStream.Seek(0, SeekOrigin.Begin);
            lilEIbmFloat32FileStream.Seek(0, SeekOrigin.Begin);

            LittleEndianBinaryReader lilEInt16Reader = new LittleEndianBinaryReader(lilEInt16FileStream);
            LittleEndianBinaryReader lilEInt32Reader = new LittleEndianBinaryReader(lilEInt32FileStream);
            LittleEndianBinaryReader lilEInt64Reader = new LittleEndianBinaryReader(lilEInt64FileStream);
            LittleEndianBinaryReader lilEUInt16Reader = new LittleEndianBinaryReader(lilEUInt16FileStream);
            LittleEndianBinaryReader lilEUInt32Reader = new LittleEndianBinaryReader(lilEUInt32FileStream);
            LittleEndianBinaryReader lilEUInt64Reader = new LittleEndianBinaryReader(lilEUInt64FileStream);
            LittleEndianBinaryReader lilEFloat32Reader = new LittleEndianBinaryReader(lilEFloat32FileStream);
            LittleEndianBinaryReader lilEFloat64Reader = new LittleEndianBinaryReader(lilEFloat64FileStream);
            LittleEndianBinaryReader lilEIbmFloat32Reader = new LittleEndianBinaryReader(lilEIbmFloat32FileStream);
            LittleEndianBinaryReader lilEIbmFloat64Reader = new LittleEndianBinaryReader(lilEIbmFloat64FileStream);

            var valInt16 = lilEInt16Reader.ReadInt16s(floatCount);
            var valInt32 = lilEInt32Reader.ReadInt32s(floatCount);
            var valInt64 = lilEInt64Reader.ReadInt64s(floatCount);
            var valUInt16 = lilEUInt16Reader.ReadUInt16s(floatCount);
            var valUInt32 = lilEUInt32Reader.ReadUInt32s(floatCount);
            var valUInt64 = lilEUInt64Reader.ReadUInt64s(floatCount);
            var valFloat32 = lilEFloat32Reader.ReadSingles(floatCount);
            var valFloat64 = lilEFloat64Reader.ReadDoubles(floatCount);
            var valIbmFloat32 = lilEIbmFloat32Reader.ReadIbmSingles(floatCount);

            Assert.True(valInt16.Length == floatCount);
            Assert.True(valInt32.Length == floatCount);
            Assert.True(valInt64.Length == floatCount);
            Assert.True(valUInt16.Length == floatCount);
            Assert.True(valUInt32.Length == floatCount);
            Assert.True(valUInt64.Length == floatCount);
            Assert.True(valFloat32.Length == floatCount);
            Assert.True(valFloat64.Length == floatCount);
            Assert.True(valIbmFloat32.Length == floatCount);

            for (int i = 0; i < floatCount; i++)
            {
                Assert.True(valInt16[i] == i - 2000);
                Assert.True(valInt16[i] == valInt32[i]);
                Assert.True(valInt16[i] == valInt64[i]);
                Assert.True(valUInt16[i] == valUInt32[i]);
                Assert.True(valUInt16[i] == valUInt64[i]);
                Assert.True(valUInt16[i] == Math.Abs(valInt16[i]));
                Assert.True(valUInt32[i] == Math.Abs(valInt32[i]));
                Assert.True((long)valUInt64[i] == Math.Abs(valInt64[i]));
                Assert.True(valFloat32[i] == valFloat64[i]);
                Assert.True(valFloat32[i] == valInt32[i]);
                Assert.True(valIbmFloat32[i] == valInt32[i]);
            }
            lilEInt16FileStream.Close();
            lilEInt32FileStream.Close();
            lilEInt64FileStream.Close();
            lilEUInt16FileStream.Close();
            lilEUInt32FileStream.Close();
            lilEUInt64FileStream.Close();
            lilEFloat32FileStream.Close();
            lilEFloat64FileStream.Close();
            lilEIbmFloat32FileStream.Close();
            lilEIbmFloat64FileStream.Close();
            File.Delete(lilEInt16FileStream.Name);
            File.Delete(lilEInt32FileStream.Name);
            File.Delete(lilEInt64FileStream.Name);
            File.Delete(lilEUInt16FileStream.Name);
            File.Delete(lilEUInt32FileStream.Name);
            File.Delete(lilEUInt64FileStream.Name);
            File.Delete(lilEFloat32FileStream.Name);
            File.Delete(lilEFloat64FileStream.Name);
            File.Delete(lilEIbmFloat32FileStream.Name);
            File.Delete(lilEIbmFloat64FileStream.Name);
        }

        [Fact]
        public static void LilEndianBinaryWriterArrayQualityTest()
        {
            FileStream lilEInt16FileStream = File.Open("lil_endian_int16_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEInt32FileStream = File.Open("lil_endian_int32_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEInt64FileStream = File.Open("lil_endian_int64_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEUInt16FileStream = File.Open("lil_endian_uint16_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEUInt32FileStream = File.Open("lil_endian_uint32_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEUInt64FileStream = File.Open("lil_endian_uint64_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEFloat32FileStream = File.Open("lil_endian_float32_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEFloat64FileStream = File.Open("lil_endian_float64_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEIbmFloat32FileStream = File.Open("lil_endian_ibm_float32_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream lilEIbmFloat64FileStream = File.Open("lil_endian_ibm_float64_values_quality_3.bin", FileMode.Create, FileAccess.ReadWrite);

            LittleEndianBinaryWriter lilEInt16Writer = new LittleEndianBinaryWriter(lilEInt16FileStream);
            LittleEndianBinaryWriter lilEInt32Writer = new LittleEndianBinaryWriter(lilEInt32FileStream);
            LittleEndianBinaryWriter lilEInt64Writer = new LittleEndianBinaryWriter(lilEInt64FileStream);
            LittleEndianBinaryWriter lilEUInt16Writer = new LittleEndianBinaryWriter(lilEUInt16FileStream);
            LittleEndianBinaryWriter lilEUInt32Writer = new LittleEndianBinaryWriter(lilEUInt32FileStream);
            LittleEndianBinaryWriter lilEUInt64Writer = new LittleEndianBinaryWriter(lilEUInt64FileStream);
            LittleEndianBinaryWriter lilEFloat32Writer = new LittleEndianBinaryWriter(lilEFloat32FileStream);
            LittleEndianBinaryWriter lilEFloat64Writer = new LittleEndianBinaryWriter(lilEFloat64FileStream);
            LittleEndianBinaryWriter lilEIbmFloat32Writer = new LittleEndianBinaryWriter(lilEIbmFloat32FileStream);
            LittleEndianBinaryWriter lilEIbmFloat64Writer = new LittleEndianBinaryWriter(lilEIbmFloat64FileStream);

            var ns = 4000;
            uint[] uintArr = new uint[ns];
            ulong[] ulongArr = new ulong[ns];
            ushort[] ushortArr = new ushort[ns];
            int[] intArr = new int[ns];
            long[] longArr = new long[ns];
            short[] shortArr = new short[ns];
            float[] floatArr = new float[ns];
            double[] doubleArr = new double[ns];

            var floatCount = 0;
            for (int i = -2000; i < 2000; i++)
            {
                shortArr[i + 2000] = (short)i;
                intArr[i + 2000] = (int)i;
                longArr[i + 2000] = (long)i;
                ushortArr[i + 2000] = (ushort)Math.Abs(i);
                uintArr[i + 2000] = (uint)Math.Abs(i);
                ulongArr[i + 2000] = (ulong)Math.Abs(i);
                floatArr[i + 2000] = (float)i;
                doubleArr[i + 2000] = (double)i;
                floatCount++;
            }
            lilEInt16Writer.Write(shortArr);
            lilEInt32Writer.Write(intArr);
            lilEInt64Writer.Write(longArr);
            lilEUInt16Writer.Write(ushortArr);
            lilEUInt32Writer.Write(uintArr);
            lilEUInt64Writer.Write(ulongArr);
            lilEFloat32Writer.Write(floatArr);
            lilEFloat64Writer.Write(doubleArr);
            lilEIbmFloat32Writer.WriteIbm(floatArr);

            lilEInt16FileStream.Seek(0, SeekOrigin.Begin);
            lilEInt32FileStream.Seek(0, SeekOrigin.Begin);
            lilEInt64FileStream.Seek(0, SeekOrigin.Begin);
            lilEUInt16FileStream.Seek(0, SeekOrigin.Begin);
            lilEUInt32FileStream.Seek(0, SeekOrigin.Begin);
            lilEUInt64FileStream.Seek(0, SeekOrigin.Begin);
            lilEFloat32FileStream.Seek(0, SeekOrigin.Begin);
            lilEFloat64FileStream.Seek(0, SeekOrigin.Begin);
            lilEIbmFloat32FileStream.Seek(0, SeekOrigin.Begin);

            LittleEndianBinaryReader lilEInt16Reader = new LittleEndianBinaryReader(lilEInt16FileStream);
            LittleEndianBinaryReader lilEInt32Reader = new LittleEndianBinaryReader(lilEInt32FileStream);
            LittleEndianBinaryReader lilEInt64Reader = new LittleEndianBinaryReader(lilEInt64FileStream);
            LittleEndianBinaryReader lilEUInt16Reader = new LittleEndianBinaryReader(lilEUInt16FileStream);
            LittleEndianBinaryReader lilEUInt32Reader = new LittleEndianBinaryReader(lilEUInt32FileStream);
            LittleEndianBinaryReader lilEUInt64Reader = new LittleEndianBinaryReader(lilEUInt64FileStream);
            LittleEndianBinaryReader lilEFloat32Reader = new LittleEndianBinaryReader(lilEFloat32FileStream);
            LittleEndianBinaryReader lilEFloat64Reader = new LittleEndianBinaryReader(lilEFloat64FileStream);
            LittleEndianBinaryReader lilEIbmFloat32Reader = new LittleEndianBinaryReader(lilEIbmFloat32FileStream);
            LittleEndianBinaryReader lilEIbmFloat64Reader = new LittleEndianBinaryReader(lilEIbmFloat64FileStream);

            var valInt16 = lilEInt16Reader.ReadInt16s(floatCount);
            var valInt32 = lilEInt32Reader.ReadInt32s(floatCount);
            var valInt64 = lilEInt64Reader.ReadInt64s(floatCount);
            var valUInt16 = lilEUInt16Reader.ReadUInt16s(floatCount);
            var valUInt32 = lilEUInt32Reader.ReadUInt32s(floatCount);
            var valUInt64 = lilEUInt64Reader.ReadUInt64s(floatCount);
            var valFloat32 = lilEFloat32Reader.ReadSingles(floatCount);
            var valFloat64 = lilEFloat64Reader.ReadDoubles(floatCount);
            var valIbmFloat32 = lilEIbmFloat32Reader.ReadIbmSingles(floatCount);

            Assert.True(valInt16.Length == floatCount);
            Assert.True(valInt32.Length == floatCount);
            Assert.True(valInt64.Length == floatCount);
            Assert.True(valUInt16.Length == floatCount);
            Assert.True(valUInt32.Length == floatCount);
            Assert.True(valUInt64.Length == floatCount);
            Assert.True(valFloat32.Length == floatCount);
            Assert.True(valFloat64.Length == floatCount);
            Assert.True(valIbmFloat32.Length == floatCount);

            for (int i = 0; i < floatCount; i++)
            {
                Assert.True(valInt16[i] == i - 2000);
                Assert.True(valInt16[i] == valInt32[i]);
                Assert.True(valInt16[i] == valInt64[i]);
                Assert.True(valUInt16[i] == valUInt32[i]);
                Assert.True(valUInt16[i] == valUInt64[i]);
                Assert.True(valUInt16[i] == Math.Abs(valInt16[i]));
                Assert.True(valUInt32[i] == Math.Abs(valInt32[i]));
                Assert.True((long)valUInt64[i] == Math.Abs(valInt64[i]));
                Assert.True(valFloat32[i] == valFloat64[i]);
                Assert.True(valFloat32[i] == valInt32[i]);
                Assert.True(valIbmFloat32[i] == valInt32[i]);
            }
            lilEInt16FileStream.Close();
            lilEInt32FileStream.Close();
            lilEInt64FileStream.Close();
            lilEUInt16FileStream.Close();
            lilEUInt32FileStream.Close();
            lilEUInt64FileStream.Close();
            lilEFloat32FileStream.Close();
            lilEFloat64FileStream.Close();
            lilEIbmFloat32FileStream.Close();
            lilEIbmFloat64FileStream.Close();
            File.Delete(lilEInt16FileStream.Name);
            File.Delete(lilEInt32FileStream.Name);
            File.Delete(lilEInt64FileStream.Name);
            File.Delete(lilEUInt16FileStream.Name);
            File.Delete(lilEUInt32FileStream.Name);
            File.Delete(lilEUInt64FileStream.Name);
            File.Delete(lilEFloat32FileStream.Name);
            File.Delete(lilEFloat64FileStream.Name);
            File.Delete(lilEIbmFloat32FileStream.Name);
            File.Delete(lilEIbmFloat64FileStream.Name);
        }

        [Fact]
        public static void BigEndianBinaryWriterExplicitQualityTest()
        {
            FileStream BigEInt16FileStream = File.Open("Big_endian_int16_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEInt32FileStream = File.Open("Big_endian_int32_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEInt64FileStream = File.Open("Big_endian_int64_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEUInt16FileStream = File.Open("Big_endian_uint16_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEUInt32FileStream = File.Open("Big_endian_uint32_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEUInt64FileStream = File.Open("Big_endian_uint64_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEFloat32FileStream = File.Open("Big_endian_float32_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEFloat64FileStream = File.Open("Big_endian_float64_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEIbmFloat32FileStream = File.Open("Big_endian_ibm_float32_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEIbmFloat64FileStream = File.Open("Big_endian_ibm_float64_values_quality_4.bin", FileMode.Create, FileAccess.ReadWrite);

            BigEndianBinaryWriter BigEInt16Writer = new BigEndianBinaryWriter(BigEInt16FileStream);
            BigEndianBinaryWriter BigEInt32Writer = new BigEndianBinaryWriter(BigEInt32FileStream);
            BigEndianBinaryWriter BigEInt64Writer = new BigEndianBinaryWriter(BigEInt64FileStream);
            BigEndianBinaryWriter BigEUInt16Writer = new BigEndianBinaryWriter(BigEUInt16FileStream);
            BigEndianBinaryWriter BigEUInt32Writer = new BigEndianBinaryWriter(BigEUInt32FileStream);
            BigEndianBinaryWriter BigEUInt64Writer = new BigEndianBinaryWriter(BigEUInt64FileStream);
            BigEndianBinaryWriter BigEFloat32Writer = new BigEndianBinaryWriter(BigEFloat32FileStream);
            BigEndianBinaryWriter BigEFloat64Writer = new BigEndianBinaryWriter(BigEFloat64FileStream);
            BigEndianBinaryWriter BigEIbmFloat32Writer = new BigEndianBinaryWriter(BigEIbmFloat32FileStream);
            BigEndianBinaryWriter BigEIbmFloat64Writer = new BigEndianBinaryWriter(BigEIbmFloat64FileStream);

            var floatCount = 0;
            for (int i = -2000; i < 2000; i++)
            {
                BigEInt16Writer.Write((short)i);
                BigEInt32Writer.Write((int)i);
                BigEInt64Writer.Write((long)i);
                BigEUInt16Writer.Write((ushort)Math.Abs(i));
                BigEUInt32Writer.Write((uint)Math.Abs(i));
                BigEUInt64Writer.Write((ulong)Math.Abs(i));
                BigEFloat32Writer.Write((float)i);
                BigEFloat64Writer.Write((double)i);
                BigEIbmFloat32Writer.WriteIbm((float)i);
                floatCount++;
            }

            BigEInt16FileStream.Seek(0, SeekOrigin.Begin);
            BigEInt32FileStream.Seek(0, SeekOrigin.Begin);
            BigEInt64FileStream.Seek(0, SeekOrigin.Begin);
            BigEUInt16FileStream.Seek(0, SeekOrigin.Begin);
            BigEUInt32FileStream.Seek(0, SeekOrigin.Begin);
            BigEUInt64FileStream.Seek(0, SeekOrigin.Begin);
            BigEFloat32FileStream.Seek(0, SeekOrigin.Begin);
            BigEFloat64FileStream.Seek(0, SeekOrigin.Begin);
            BigEIbmFloat32FileStream.Seek(0, SeekOrigin.Begin);

            BigEndianBinaryReader BigEInt16Reader = new BigEndianBinaryReader(BigEInt16FileStream);
            BigEndianBinaryReader BigEInt32Reader = new BigEndianBinaryReader(BigEInt32FileStream);
            BigEndianBinaryReader BigEInt64Reader = new BigEndianBinaryReader(BigEInt64FileStream);
            BigEndianBinaryReader BigEUInt16Reader = new BigEndianBinaryReader(BigEUInt16FileStream);
            BigEndianBinaryReader BigEUInt32Reader = new BigEndianBinaryReader(BigEUInt32FileStream);
            BigEndianBinaryReader BigEUInt64Reader = new BigEndianBinaryReader(BigEUInt64FileStream);
            BigEndianBinaryReader BigEFloat32Reader = new BigEndianBinaryReader(BigEFloat32FileStream);
            BigEndianBinaryReader BigEFloat64Reader = new BigEndianBinaryReader(BigEFloat64FileStream);
            BigEndianBinaryReader BigEIbmFloat32Reader = new BigEndianBinaryReader(BigEIbmFloat32FileStream);
            BigEndianBinaryReader BigEIbmFloat64Reader = new BigEndianBinaryReader(BigEIbmFloat64FileStream);

            var valInt16 = BigEInt16Reader.ReadInt16s(floatCount);
            var valInt32 = BigEInt32Reader.ReadInt32s(floatCount);
            var valInt64 = BigEInt64Reader.ReadInt64s(floatCount);
            var valUInt16 = BigEUInt16Reader.ReadUInt16s(floatCount);
            var valUInt32 = BigEUInt32Reader.ReadUInt32s(floatCount);
            var valUInt64 = BigEUInt64Reader.ReadUInt64s(floatCount);
            var valFloat32 = BigEFloat32Reader.ReadSingles(floatCount);
            var valFloat64 = BigEFloat64Reader.ReadDoubles(floatCount);
            var valIbmFloat32 = BigEIbmFloat32Reader.ReadIbmSingles(floatCount);

            Assert.True(valInt16.Length == floatCount);
            Assert.True(valInt32.Length == floatCount);
            Assert.True(valInt64.Length == floatCount);
            Assert.True(valUInt16.Length == floatCount);
            Assert.True(valUInt32.Length == floatCount);
            Assert.True(valUInt64.Length == floatCount);
            Assert.True(valFloat32.Length == floatCount);
            Assert.True(valFloat64.Length == floatCount);
            Assert.True(valIbmFloat32.Length == floatCount);

            for (int i = 0; i < floatCount; i++)
            {
                Assert.True(valInt16[i] == i - 2000);
                Assert.True(valInt16[i] == valInt32[i]);
                Assert.True(valInt16[i] == valInt64[i]);
                Assert.True(valUInt16[i] == valUInt32[i]);
                Assert.True(valUInt16[i] == valUInt64[i]);
                Assert.True(valUInt16[i] == Math.Abs(valInt16[i]));
                Assert.True(valUInt32[i] == Math.Abs(valInt32[i]));
                Assert.True((long)valUInt64[i] == Math.Abs(valInt64[i]));
                Assert.True(valFloat32[i] == valFloat64[i]);
                Assert.True(valFloat32[i] == valInt32[i]);
                Assert.True(valIbmFloat32[i] == valInt32[i]);
            }
            BigEInt16FileStream.Close();
            BigEInt32FileStream.Close();
            BigEInt64FileStream.Close();
            BigEUInt16FileStream.Close();
            BigEUInt32FileStream.Close();
            BigEUInt64FileStream.Close();
            BigEFloat32FileStream.Close();
            BigEFloat64FileStream.Close();
            BigEIbmFloat32FileStream.Close();
            BigEIbmFloat64FileStream.Close();
            File.Delete(BigEInt16FileStream.Name);
            File.Delete(BigEInt32FileStream.Name);
            File.Delete(BigEInt64FileStream.Name);
            File.Delete(BigEUInt16FileStream.Name);
            File.Delete(BigEUInt32FileStream.Name);
            File.Delete(BigEUInt64FileStream.Name);
            File.Delete(BigEFloat32FileStream.Name);
            File.Delete(BigEFloat64FileStream.Name);
            File.Delete(BigEIbmFloat32FileStream.Name);
            File.Delete(BigEIbmFloat64FileStream.Name);
        }

        [Fact]
        public static void BigEndianBinaryWriterArrayQualityTest()
        {
            FileStream BigEInt16FileStream = File.Open("Big_endian_int16_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEInt32FileStream = File.Open("Big_endian_int32_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEInt64FileStream = File.Open("Big_endian_int64_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEUInt16FileStream = File.Open("Big_endian_uint16_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEUInt32FileStream = File.Open("Big_endian_uint32_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEUInt64FileStream = File.Open("Big_endian_uint64_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEFloat32FileStream = File.Open("Big_endian_float32_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEFloat64FileStream = File.Open("Big_endian_float64_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEIbmFloat32FileStream = File.Open("Big_endian_ibm_float32_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);
            FileStream BigEIbmFloat64FileStream = File.Open("Big_endian_ibm_float64_values_quality_5.bin", FileMode.Create, FileAccess.ReadWrite);

            BigEndianBinaryWriter BigEInt16Writer = new BigEndianBinaryWriter(BigEInt16FileStream);
            BigEndianBinaryWriter BigEInt32Writer = new BigEndianBinaryWriter(BigEInt32FileStream);
            BigEndianBinaryWriter BigEInt64Writer = new BigEndianBinaryWriter(BigEInt64FileStream);
            BigEndianBinaryWriter BigEUInt16Writer = new BigEndianBinaryWriter(BigEUInt16FileStream);
            BigEndianBinaryWriter BigEUInt32Writer = new BigEndianBinaryWriter(BigEUInt32FileStream);
            BigEndianBinaryWriter BigEUInt64Writer = new BigEndianBinaryWriter(BigEUInt64FileStream);
            BigEndianBinaryWriter BigEFloat32Writer = new BigEndianBinaryWriter(BigEFloat32FileStream);
            BigEndianBinaryWriter BigEFloat64Writer = new BigEndianBinaryWriter(BigEFloat64FileStream);
            BigEndianBinaryWriter BigEIbmFloat32Writer = new BigEndianBinaryWriter(BigEIbmFloat32FileStream);
            BigEndianBinaryWriter BigEIbmFloat64Writer = new BigEndianBinaryWriter(BigEIbmFloat64FileStream);

            var ns = 4000;
            uint[] uintArr = new uint[ns];
            ulong[] ulongArr = new ulong[ns];
            ushort[] ushortArr = new ushort[ns];
            int[] intArr = new int[ns];
            long[] longArr = new long[ns];
            short[] shortArr = new short[ns];
            float[] floatArr = new float[ns];
            double[] doubleArr = new double[ns];

            var floatCount = 0;
            for (int i = -2000; i < 2000; i++)
            {
                shortArr[i + 2000] = (short)i;
                intArr[i + 2000] = (int)i;
                longArr[i + 2000] = (long)i;
                ushortArr[i + 2000] = (ushort)Math.Abs(i);
                uintArr[i + 2000] = (uint)Math.Abs(i);
                ulongArr[i + 2000] = (ulong)Math.Abs(i);
                floatArr[i + 2000] = (float)i;
                doubleArr[i + 2000] = (double)i;
                floatCount++;
            }
            BigEInt16Writer.Write(shortArr);
            BigEInt32Writer.Write(intArr);
            BigEInt64Writer.Write(longArr);
            BigEUInt16Writer.Write(ushortArr);
            BigEUInt32Writer.Write(uintArr);
            BigEUInt64Writer.Write(ulongArr);
            BigEFloat32Writer.Write(floatArr);
            BigEFloat64Writer.Write(doubleArr);
            BigEIbmFloat32Writer.WriteIbm(floatArr);

            BigEInt16FileStream.Seek(0, SeekOrigin.Begin);
            BigEInt32FileStream.Seek(0, SeekOrigin.Begin);
            BigEInt64FileStream.Seek(0, SeekOrigin.Begin);
            BigEUInt16FileStream.Seek(0, SeekOrigin.Begin);
            BigEUInt32FileStream.Seek(0, SeekOrigin.Begin);
            BigEUInt64FileStream.Seek(0, SeekOrigin.Begin);
            BigEFloat32FileStream.Seek(0, SeekOrigin.Begin);
            BigEFloat64FileStream.Seek(0, SeekOrigin.Begin);
            BigEIbmFloat32FileStream.Seek(0, SeekOrigin.Begin);

            BigEndianBinaryReader BigEInt16Reader = new BigEndianBinaryReader(BigEInt16FileStream);
            BigEndianBinaryReader BigEInt32Reader = new BigEndianBinaryReader(BigEInt32FileStream);
            BigEndianBinaryReader BigEInt64Reader = new BigEndianBinaryReader(BigEInt64FileStream);
            BigEndianBinaryReader BigEUInt16Reader = new BigEndianBinaryReader(BigEUInt16FileStream);
            BigEndianBinaryReader BigEUInt32Reader = new BigEndianBinaryReader(BigEUInt32FileStream);
            BigEndianBinaryReader BigEUInt64Reader = new BigEndianBinaryReader(BigEUInt64FileStream);
            BigEndianBinaryReader BigEFloat32Reader = new BigEndianBinaryReader(BigEFloat32FileStream);
            BigEndianBinaryReader BigEFloat64Reader = new BigEndianBinaryReader(BigEFloat64FileStream);
            BigEndianBinaryReader BigEIbmFloat32Reader = new BigEndianBinaryReader(BigEIbmFloat32FileStream);
            BigEndianBinaryReader BigEIbmFloat64Reader = new BigEndianBinaryReader(BigEIbmFloat64FileStream);

            var valInt16 = BigEInt16Reader.ReadInt16s(floatCount);
            var valInt32 = BigEInt32Reader.ReadInt32s(floatCount);
            var valInt64 = BigEInt64Reader.ReadInt64s(floatCount);
            var valUInt16 = BigEUInt16Reader.ReadUInt16s(floatCount);
            var valUInt32 = BigEUInt32Reader.ReadUInt32s(floatCount);
            var valUInt64 = BigEUInt64Reader.ReadUInt64s(floatCount);
            var valFloat32 = BigEFloat32Reader.ReadSingles(floatCount);
            var valFloat64 = BigEFloat64Reader.ReadDoubles(floatCount);
            var valIbmFloat32 = BigEIbmFloat32Reader.ReadIbmSingles(floatCount);

            Assert.True(valInt16.Length == floatCount);
            Assert.True(valInt32.Length == floatCount);
            Assert.True(valInt64.Length == floatCount);
            Assert.True(valUInt16.Length == floatCount);
            Assert.True(valUInt32.Length == floatCount);
            Assert.True(valUInt64.Length == floatCount);
            Assert.True(valFloat32.Length == floatCount);
            Assert.True(valFloat64.Length == floatCount);
            Assert.True(valIbmFloat32.Length == floatCount);

            for (int i = 0; i < floatCount; i++)
            {
                Assert.True(valInt16[i] == i - 2000);
                Assert.True(valInt16[i] == valInt32[i]);
                Assert.True(valInt16[i] == valInt64[i]);
                Assert.True(valUInt16[i] == valUInt32[i]);
                Assert.True(valUInt16[i] == valUInt64[i]);
                Assert.True(valUInt16[i] == Math.Abs(valInt16[i]));
                Assert.True(valUInt32[i] == Math.Abs(valInt32[i]));
                Assert.True((long)valUInt64[i] == Math.Abs(valInt64[i]));
                Assert.True(valFloat32[i] == valFloat64[i]);
                Assert.True(valFloat32[i] == valInt32[i]);
                Assert.True(valIbmFloat32[i] == valInt32[i]);
            }

            BigEInt16FileStream.Close();
            BigEInt32FileStream.Close();
            BigEInt64FileStream.Close();
            BigEUInt16FileStream.Close();
            BigEUInt32FileStream.Close();
            BigEUInt64FileStream.Close();
            BigEFloat32FileStream.Close();
            BigEFloat64FileStream.Close();
            BigEIbmFloat32FileStream.Close();
            BigEIbmFloat64FileStream.Close();
            File.Delete(BigEInt16FileStream.Name);
            File.Delete(BigEInt32FileStream.Name);
            File.Delete(BigEInt64FileStream.Name);
            File.Delete(BigEUInt16FileStream.Name);
            File.Delete(BigEUInt32FileStream.Name);
            File.Delete(BigEUInt64FileStream.Name);
            File.Delete(BigEFloat32FileStream.Name);
            File.Delete(BigEFloat64FileStream.Name);
            File.Delete(BigEIbmFloat32FileStream.Name);
            File.Delete(BigEIbmFloat64FileStream.Name);
        }
    }
}