namespace DotNetNpyIo
{
    public interface IBinaryWriter : IDisposable
    {
        Stream BaseStream { get; }
        void Flush();
        void Write<T>(T[] values, int count);
        void Write<T>(T value);
        void Write(ulong value);
        void Write(uint value);
        void Write(ushort value);
        void Write(short value);
        void Write(int value);
        void Write(long value);
        void Write(string value);
        void Write(Half value);
        void Write(float value);
        void WriteIbm(float value);
        void Write(sbyte value);
        void Write(double value);
        void Write(decimal value);
        void Write(byte value);
        void Write(bool value);
        void Write(char ch);
        void Write<T>(T[] values);
        void Write(ulong[] value);
        void Write(uint[] value);
        void Write(ushort[] value);
        void Write(short[] value);
        void Write(int[] value);
        void Write(long[] value);
        void Write(string[] value);
        void Write(Half[] value);
        void Write(float[] value);
        void WriteIbm(float[] value);
        void Write(sbyte[] value);
        void Write(double[] value);
        void Write(decimal[] value);
        void Write(byte[] value);
        void Write(bool[] value);
        void Write(char[] ch);
    }
}
