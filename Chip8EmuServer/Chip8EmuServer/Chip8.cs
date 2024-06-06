using Chip8EmuServer.structs;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Net.WebSockets;
using Microsoft.Extensions.Configuration.Ini;
using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Chip8EmuServer
{
    public class Chip8
    {
        #region "Fields"
        private Stopwatch delayStopwatch = new Stopwatch();
        private Stopwatch soundStopwatch = new Stopwatch();
        private byte delayTimer;
        private byte soundTimer;
        private double timerHz = 1.0 / 60.0 * 1000;
        private GameState gameState;
        private Func<byte[], ConnHandler.SendType, Task> gameStateOut;
        //private Stopwatch timer;
        #endregion
        #region "Memory"
        public byte[] Ram { get; private set; } = new byte[4096];

        #endregion

        #region "Registers"

        public byte[] V { get; private set; } = new byte[16];
        public ushort I { get; private set; } = 0;
        public ushort Pc { get; private set; } = 0x200;
        public byte Sp { get; private set; } = 0;
        public ushort[] Stack { get; private set; } = new ushort[16];
        #endregion

        #region "Timers"
        public byte DelayTimer
        {
            get
            {
                int ticks = (int)(delayStopwatch.ElapsedMilliseconds / timerHz);
                if (delayTimer <= ticks)
                {
                    delayStopwatch.Stop();
                    return 0;
                }
                else return (byte)(delayTimer - ticks);
            }
            set
            {
                delayTimer = value;
                delayStopwatch.Restart();
            }
        }
        public byte SoundTimer
        {
            get
            {
                int ticks = (int)(soundStopwatch.ElapsedMilliseconds / timerHz);
                if (soundTimer <= ticks)
                {
                    soundStopwatch.Stop();
                    return 0;
                }
                else return (byte)(soundTimer - ticks);
            }
            set
            {
                soundTimer = value;
                soundStopwatch.Restart();
            }
        }
        #endregion

        #region "External hardware"
        public Display Display { get; set; }
        public Keyboard Keyboard { get; set; }
        #endregion

        public Chip8(Func<byte[], ConnHandler.SendType, Task> gameStateOut)
        {
            byte[] sprites = {
                0xF0, 0x90, 0x90, 0x90, 0xF0,
                0x20, 0x60, 0x20, 0x20, 0x70,
                0xF0, 0x10, 0xF0, 0x80, 0xF0,
                0xF0, 0x10, 0xF0, 0x10, 0xF0,
                0x90, 0x90, 0xF0, 0x10, 0x10,
                0xF0, 0x80, 0xF0, 0x10, 0xF0,
                0xF0, 0x80, 0xF0, 0x90, 0xF0,
                0xF0, 0x10, 0x20, 0x40, 0x40,
                0xF0, 0x90, 0xF0, 0x90, 0xF0,
                0xF0, 0x90, 0xF0, 0x10, 0xF0,

                0xF0, 0x90, 0xF0, 0x90, 0x90,
                0xE0, 0x90, 0xE0, 0x90, 0xE0,
                0xF0, 0x80, 0x80, 0x80, 0xF0,
                0xE0, 0x90, 0x90, 0x90, 0xE0,
                0xF0, 0x80, 0xF0, 0x80, 0xF0,
                0xF0, 0x80, 0xF0, 0x80, 0x80
            };
            for (int i = 0; i < sprites.Length; i++)
            {
                Ram[i] = sprites[i];
            }
            


            Display = new();
            Keyboard = new();
            this.gameStateOut = gameStateOut;
        }

        public byte[] Load(byte[] program)
        {
            for (int i = 0; i < program.Length; i++)
            {
                Ram[i + Pc] = program[i];
            }
            return Ram;
        }
        
        public void Cycle(Action<Instruction, Stopwatch, Stopwatch> checkBlock)
        {
            Instruction instruction = ReadInstruction();

            checkBlock.Invoke(instruction, delayStopwatch, soundStopwatch);
            
            byte oldVx = V[instruction.X];

            switch (instruction.OpCode)
            {
                case 0x0:
                    switch (instruction.Nnn)
                    {
                        case 0x0:
                            //For testing
                            break;
                        case 0x00E0:
                            Array.Fill(Display.PixelArray, (byte)0);
                            break;
                        case 0x00EE:
                            Pc = Stack[--Sp];
                            break;
                    }
                    break;
                case 0x1:
                    Pc = instruction.Nnn;
                    break;
                case 0x2:
                    Stack[Sp++] = Pc;
                    Pc = instruction.Nnn;
                    break;
                case 0x3:
                    if (V[instruction.X] == instruction.Kk) Pc += 2;
                    break;
                case 0x4:
                    if (V[instruction.X] != instruction.Kk) Pc += 2;
                    break;
                case 0x5:
                    if (V[instruction.X] == V[instruction.Y]) Pc += 2;
                    break;
                case 0x6:
                    V[instruction.X] = instruction.Kk;
                    break;
                case 0x7:
                    V[instruction.X] += instruction.Kk;
                    break;
                case 0x8:
                    switch (instruction.N)
                    {
                        case 0x0:
                            V[instruction.X] = V[instruction.Y];
                            break;
                        case 0x1:
                            V[instruction.X] |= V[instruction.Y];
                            break;
                        case 0x2:
                            V[instruction.X] &= V[instruction.Y];
                            break;
                        case 0x3:
                            V[instruction.X] ^= V[instruction.Y];
                            break;
                        case 0x4:
                            ushort result = (ushort)(V[instruction.X] + V[instruction.Y]);
                            V[instruction.X] = (byte)result;
                            if (result > 255) V[0xF] = 1;
                            else V[0xF] = 0;
                            break;
                        case 0x5:
                            V[instruction.X] -= V[instruction.Y];
                            if (oldVx >= V[instruction.Y]) V[0xF] = 1;
                            else V[0xF] = 0;
                            break;
                        case 0x6:
                            V[instruction.X] /= 2;
                            if ((oldVx & 0x1) == 1) V[0xF] = 1;
                            else V[0xF] = 0;;
                            break;
                        case 0x7:
                            V[instruction.X] = (byte)(V[instruction.Y] - V[instruction.X]);
                            if (V[instruction.Y] > V[instruction.X]) V[0xF] = 1;
                            else V[0xF] = 0;
                            break;
                        case 0xE:
                            V[instruction.X] *= 2;
                            if (((oldVx & 0x80) >> 7) == 1) V[0xF] = 1;
                            else V[0xF] = 0;
                            break;
                    }
                    break;
                case 0x9:
                    if (V[instruction.X] != V[instruction.Y]) Pc += 2;
                    break;
                case 0xA:
                    I = instruction.Nnn;
                    break;
                case 0xB:
                    Pc = (ushort)(instruction.Nnn + V[0]);
                    break;
                case 0xC:
                    Random random = new Random();
                    V[instruction.X] = (byte)(random.Next(256) & instruction.Kk);
                    break;
                case 0xD:
                    byte[] sprite = new byte[instruction.N];
                    for (int i = 0; i < instruction.N; i++)
                    {
                        sprite[i] = Ram[this.I + i];
                    }
                    V[0xF] = Display.InsertSprite(V[instruction.X], V[instruction.Y], sprite);
                    break;
                case 0xE:
                    switch (instruction.Kk)
                    {
                        case 0x9E:
                            if (Keyboard.IsPressed(V[instruction.X])) Pc += 2;
                            break;
                        case 0xA1:
                            if (!Keyboard.IsPressed(V[instruction.X])) Pc += 2;
                            break;
                    }
                    break;
                case 0xF:
                    switch (instruction.Kk)
                    {
                        case 0x7:
                            V[instruction.X] = DelayTimer;
                            break;
                        case 0xA:
                            SendGameState();
                            V[instruction.X] = (byte)Keyboard.GetNextPress();
                            Console.WriteLine("test");
                            break;
                        case 0x15:
                            DelayTimer = V[instruction.X];
                            break;
                        case 0x18:
                            SoundTimer = V[instruction.X];
                            break;
                        case 0x1E:
                            I += V[instruction.X];
                            break;
                        case 0x29:
                            I = (ushort)(V[instruction.X] * 5);
                            break;
                        case 0x33:
                            Ram[I] = (byte)(V[instruction.X] / 100);
                            Ram[I + 1] = (byte)(V[instruction.X] % 100 / 10);
                            Ram[I + 2] = (byte)(V[instruction.X] % 10 / 1);
                            break;
                        case 0x55:
                            for (int i = 0; i <= instruction.X; i++)
                            {
                                Ram[I + i] = V[i];
                            }
                            break;
                        case 0x65:
                            for (int i = 0; i <= instruction.X; i++)
                            {
                                V[i] = Ram[I + i];
                            }
                            break;
                    }
                    break;
            }
            gameState = new GameState(Display.PixelArray, SoundTimer != 0);
        }
        

        public Instruction ReadInstruction()
        {
            byte mostSig = Ram[Pc];
            byte leastSig = Ram[Pc + 1];
            Instruction instruction = new Instruction(mostSig, leastSig, Pc - 0x200);
            Pc += 2;
            return instruction;
        }
        
        public async Task SendGameState()
        {
            await gameStateOut(gameState.GetAsByteArray(), ConnHandler.SendType.GameState);
        }
    }
}
