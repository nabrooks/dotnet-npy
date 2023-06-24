using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetNpyIo
{
    public interface IBinaryReader : IDisposable
    {
        Stream BaseStream { get; }

        T Read<T>();
        bool ReadBoolean();
        byte ReadByte();
        sbyte ReadSByte();
        short ReadInt16();
        ushort ReadUInt16();
        int ReadInt32();
        uint ReadUInt32();
        long ReadInt64();
        ulong ReadUInt64();
        Half ReadHalf();
        float ReadSingle();
        float ReadIbmSingle();
        double ReadDouble();
        double ReadIbmDouble();

        void ReadMany<T>(ref T[] objects, int count);
        T[] ReadMany<T>(int count);
        bool[] ReadBooleans(int count);
        byte[] ReadBytes(int count);
        sbyte[] ReadSBytes(int count);
        short[] ReadInt16s(int count);
        ushort[] ReadUInt16s(int count);
        int[] ReadInt32s(int count);
        uint[] ReadUInt32s(int count);
        long[] ReadInt64s(int count);
        ulong[] ReadUInt64s(int count);
        Half[] ReadHalfs(int count);
        float[] ReadSingles(int count);
        float[] ReadIbmSingles(int count);
        double[] ReadDoubles(int count);
        double[] ReadIbmDoubles(int count);
    }
}
