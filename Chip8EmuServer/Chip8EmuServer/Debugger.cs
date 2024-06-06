using Chip8EmuServer.structs;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chip8EmuServer
{
    public class Debugger
    {
        #region "Locks"
        private object instructionsLock = new();
        private object isSteppingLock = new();
        private object clockSpeedLock = new();
        #endregion

        #region "Fields"
        private List<OpcodeBlockade> instructions;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Thread chip8Thread;
        private Func<byte[], ConnHandler.SendType, Task> send;
        private bool isStepping;
        private double clockSpeed;
        private bool clippingOn;
        #endregion


        public Chip8? Chip8 { get; set; }

        public List<OpcodeBlockade> Instructions
        {
            get
            {
                lock (instructionsLock)
                {
                    return instructions;
                }
            }
            set
            {
                lock (instructionsLock)
                {
                    instructions = value;
                }
            }
        }
        public bool IsStepping
        {
            get
            {
                lock (isSteppingLock)
                {
                    return isStepping;
                }
            }
            set
            {
                lock (isSteppingLock)
                {
                    isStepping = value;
                }
            }
        }
        public bool InstructionBlocked { get; set; }

        public byte[] Program { get; set; }
        public EventWaitHandle Block { get; set; }
        public double ClockSpeed
        {
            get
            {
                lock (clockSpeedLock)
                {
                    if (clockSpeed == 0) return 0;
                    return (1.0/(clockSpeed * 1.0)) * 1000.0;
                }
            }
            set
            {
                lock(clockSpeedLock)
                {
                    clockSpeed = value;
                }
            }
        }

        public Debugger(byte[] program, Func<byte[], ConnHandler.SendType, Task> send)
        {
            //Console.WriteLine(program.Length);
            Instructions = new();
            for(int i = 0; i < program.Length; i+=2) 
            {
                byte mostSig = program[i];
                byte leastSig;
                if (i + 1 < program.Length) leastSig = program[i + 1];
                else leastSig = 0x0;


                var instruction = new Instruction(mostSig, leastSig, i);
                Instructions.Add(new OpcodeBlockade(instruction));
                
            }
            Program = program;
            this.send = send;
            Block = new EventWaitHandle(true, EventResetMode.ManualReset);

        }

        public void Chip8Input(KeyAction input)
        {
            if(chip8Thread != null && chip8Thread.IsAlive)
            {
                Chip8.Keyboard.KeyAction(input);
            }
        }

        public string ExecuteCommand(string command)
        {
            string s = "";
            if (string.IsNullOrEmpty(command)) return "Error";
            command = command.ToLower().Trim();
            string[] commandDivisions = command.Split(' ');
            switch(commandDivisions[0])
            {
                case "help":
                    s += "Start:    Takes no argument. Starts a new Chip8 with the uploaded program.<br>        All renamings and blocks carry over<br><br>" +
                        "Rename:   Takes two arguments. Renames a given opcode for easier manipulation.<br>" +
                        "          Argument 1: Opcode to rename (is a number by default)<br>" +
                        "          Argument 2: New name given to opcode. (Cannot be \"all\")<br><br>" +
                        "Dump:     Takes one argument. Dumps info from the Chip8 that relates to the argument.<br>" +
                        "          Argument 1: must either be \"reg\" for registers or \"ram\" for memory<br><br>" +
                        "Block:    Takes one argument. Blocks execution when the provided opcode is read.<br>" +
                        "          Argument 1: Name of the opcode that should trigger a block<br><br>" +
                        "Release:  Takes one argument. Release a block on an opcode.<br>" +
                        "          Argument 1: Name of opcode to release from blocking. If \"all\" is given" +
                        "          all opcodes are released.<br><br>" +
                        "Step:     Takes one argument. Decides how to handle a block.<br>" +
                        "          Argument 1: If argument given is \"in\" the chip cycles and stop at the next opcode<br>" +
                        "          if argument given is \"out\" the chip cycles and continues to cycle until a new blocked opcode is reached.<br><br>" +
                        "Opcodes:  Takes zero arguments. Returns a list of Opcodes from the uploaded program<br>" +
                        "          Opcode data is formatted as to first show opcode name then description then whether its blocked or not<br><br>" +
                        "Clockspeed: Takes one argument. Sets the cycles per seconds to the argument<br>" +
                        "          Argument 1: an integer representing the speed of the chip8 in hz. Set to 0 to uncap clockspeed<br><br>" +
                        "Clipping: Takes one argument. sets whether clipping on the display should be on or off. Requires restart of program" +
                        "          Argument 1: If argument is on clipping is turn on, if argument is off clipping is turned off";
                    return s;
                case "start":
                    if (chip8Thread != null && chip8Thread.IsAlive)
                    {
                        Close();
                    }
                    Chip8 = new(send);
                    Chip8.Display.ClippingOn = clippingOn;
                    Chip8.Load(Program);
                    chip8Thread = new Thread(() =>
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        double frameRate = (1.0 / 60.0) * 1000;
                        Stopwatch clock = Stopwatch.StartNew();
                        while (!cts.IsCancellationRequested)
                        {
                            if(ClockSpeed == 0 || clock.ElapsedMilliseconds > ClockSpeed)
                            {
                                clock.Restart();
                                Chip8.Cycle((currentInstruction, delaySw, soundSw) =>
                                {
                                    if (Instructions[currentInstruction.Index].Blocked || IsStepping)
                                    {
                                        delaySw.Stop();
                                        soundSw.Stop();
                                        InstructionBlocked = true;
                                        Block.Reset();
                                    }
                                    Block.WaitOne();
                                    InstructionBlocked = false;
                                    delaySw.Start();
                                    soundSw.Start();
                                });
                                if (sw.ElapsedMilliseconds >= frameRate)
                                {
                                    sw.Restart();
                                    Chip8.SendGameState();
                                }
                            }
                        }
                    });
                    chip8Thread.Start();
                    break;
                case "rename":
                    if (commandDivisions.Length > 2)
                    {
                        if (commandDivisions[2] == "all") return s += "\"all\" as a name conflicts with protocol. Please choose a different name";
                        //OpcodeBlockade[] opCodes = Instructions.Select(kvp => kvp.Value).ToArray();
                        string[] names = Instructions.Select(ob => ob.Name).ToArray();
                        if (!names.Contains(commandDivisions[1])) return s += "did not recognize name";
                        if (names.Contains(commandDivisions[2])) return s += "name must be unique";
                        try
                        {
                            OpcodeBlockade newIns = Instructions.Where(ins => ins.Name == commandDivisions[1]).First();
                            newIns.Name = commandDivisions[2];
                            Instructions[newIns.Opcode.Index] = newIns;
                        }
                        catch {
                            return s += "An error occurred";
                        }
                        break;
                    }
                    else return s += "Specify which opcode to rename and what to name it";
                    break;
                case "dump":
                    if (commandDivisions.Length > 1)
                    {
                        if (InstructionBlocked && Chip8 != null)
                        {
                            switch (commandDivisions[1])
                            {
                                case "reg":
                                    s = "V:      ";
                                    for (int i = 0; i < Chip8.V.Length; i++)
                                    {
                                        if (i == 0) s += $"V[{i}] -{Chip8.V[i]}<br>";
                                        else s += $"        V[{i}] - {Chip8.V[i]}<br>";
                                    };
                                    s += $"<br>I:      {Chip8.I}<br>";
                                    s += $"PC:     {Chip8.Pc}<br>";
                                    s += $"SP:     {Chip8.Sp}<br>";
                                    s += $"Stack:  ";
                                    for (int i = 0; i < Chip8.Stack.Length; i++)
                                    {
                                        if (i == 0) s += $"Stack[{i}] - {Chip8.Stack[i]}<br>";
                                        else s += $"        Stack[{i}] - {Chip8.Stack[i]}<br>";
                                    };
                                    break;
                                case "ram":
                                    s += "Ram     ";
                                    for (int i = 0; i < Chip8.Ram.Length; i++)
                                    {
                                        if (i == 0) s += $"Ram[{i}] - {Chip8.Ram[i]}<br>";
                                        else s += $"        Ram[{i}] - {Chip8.Ram[i]}<br>";
                                    }
                                    break;
                                default:
                                    s += "Did not recognize argument. Type help for command descriptions";
                                    break;
                            }
                        }
                        else s += "Chip8 must have been started and then blocked to dump";
                        return s;
                    }
                    else return s += "Please specify what info should be dumped";
                case "block":
                    if (commandDivisions.Length > 1)
                    {
                        OpcodeBlockade[] results = Instructions.Where(ins => ins.Name == commandDivisions[1]).ToArray();
                        if (results.Length < 1) return "did not recognize name";
                        OpcodeBlockade value = results[0];
                        value.Blocked = true;
                        Instructions[results[0].Opcode.Index] = value;
                    }
                    else return s += "Specify which instruction to block";

                    break;
                case "release":
                    if (commandDivisions.Length > 1)
                    {
                        switch (commandDivisions[1])
                        {
                            case "all":
                                foreach (OpcodeBlockade ins in Instructions.ToList())
                                {
                                    OpcodeBlockade newIns = ins;
                                    newIns.Blocked = false;
                                    Instructions[newIns.Opcode.Index] = newIns;
                                }
                                break;
                            default:

                                OpcodeBlockade[] results = Instructions.Where(ins => ins.Name == commandDivisions[1]).ToArray();
                                if (results.Length < 1) return "did not recognize name";
                                OpcodeBlockade newInst = results[0];
                                newInst.Blocked = false;
                                Instructions[newInst.Opcode.Index] = newInst;
                                break;
                        }
                    }
                    else return s += "Specify which instruction to release";
                    break;
                case "step":
                    if (commandDivisions.Length > 1)
                    {
                        if (InstructionBlocked)
                        {
                            switch (commandDivisions[1])
                            {
                                case "in":
                                    IsStepping = true;
                                    Block.Set();
                                    break;
                                case "out":
                                    IsStepping = false;
                                    Block.Set();
                                    break;
                            }
                        }
                        else return s += "No instruction blocked";
                    }
                    else return s += "Specify step in or out";
                    break;
                case "opcodes":
                    foreach(OpcodeBlockade opcodeBlockade in Instructions)
                    {
                        s += $"{opcodeBlockade.Name}: {opcodeBlockade.Description}: {opcodeBlockade.Blocked}<br>";
                    }
                    return s;
                case "clockspeed":
                    if(commandDivisions.Length > 1 && int.TryParse(commandDivisions[1], out var newClockSpeed))
                    {
                        ClockSpeed = newClockSpeed;
                    }
                    else
                    {
                        return "did not recognize argument. Type an integer representing how many cycles a second Chip8 should run";
                    }
                    break;
                case "clipping":
                    if (commandDivisions.Length > 1)
                    {
                        switch (commandDivisions[1])
                        {
                            case "on":
                                clippingOn = true;
                                break;
                            case "off":
                                clippingOn = false;
                                break;
                        }
                    }
                    else return "Specify whether clipping should be on or off";
                    break;
                default:
                    return "Unrecognized command - enter \"help\" to see command descriptions";
            }
            return "";
        }

        public void Close()
        {
            if(chip8Thread != null && chip8Thread.IsAlive)
            {
                cts.Cancel();
                Chip8.Keyboard.WaitForKeyPress.Set();
                Block.Set();
                isStepping = false;
                chip8Thread.Join();
                Console.WriteLine("test");
                cts = new CancellationTokenSource();
            }
        }

        public byte[] DebuggerSaysHello()
        {
            string hello = ExecuteCommand("opcodes");
            hello += "Type \"start\" to run program. Type help for instructions";
            return Encoding.UTF8.GetBytes(hello);
        }


    }
}
