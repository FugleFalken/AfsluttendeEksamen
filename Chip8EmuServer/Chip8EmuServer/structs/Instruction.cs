using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Chip8EmuServer.structs
{
    public struct Instruction
    {
        public int Index { get; set; }
        public byte OpCode { get; }
        public ushort Nnn { get; }
        public byte N { get; }
        public byte X { get; }
        public byte Y { get; }
        public byte Kk { get; }


        public Instruction(byte mostSig, byte leastSig, int index)
        {
            OpCode = (byte)((mostSig & 0xF0) >> 4);
            Nnn = (ushort)(((mostSig & 0x0F) << 8) | leastSig);
            N = (byte)(leastSig & 0x0F);
            X = (byte)(mostSig & 0x0F);
            Y = (byte)((leastSig & 0xF0) >> 4);
            Kk = leastSig;
            Index = index / 2;
        }
    }
}
