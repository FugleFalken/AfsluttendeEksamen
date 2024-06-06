namespace Chip8EmuServer.structs
{
    public struct OpcodeBlockade
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Instruction Opcode { get; set; }
        public bool Blocked { get; set; }

        public OpcodeBlockade(Instruction opcode)
        {
            Opcode = opcode;
            Blocked = false;
            Description = $"Unrecognized opcode";
            Name = opcode.Index.ToString();
            
            
            switch (opcode.OpCode)
            {
                case 0x0:
                    switch (opcode.Nnn)
                    {
                        case 0x0:
                            Description = $"SYS {opcode.Nnn} - Not supported";
                            break;
                        case 0x00E0:
                            Description = "CLS";
                            break;
                        case 0x00EE:
                            Description = "RET";
                            break;
                    }
                    break;
                case 0x1:
                    Description = $"JP {opcode.Nnn}";
                    break;
                case 0x2:
                    Description = $"CALL {opcode.Nnn}";
                    break;
                case 0x3:
                    Description = $"SE V{opcode.X}, {opcode.Kk}";
                    break;
                case 0x4:
                    Description = $"SNE V{opcode.X}, {opcode.Kk}";
                    break;
                case 0x5:
                    Description = $"SE V{opcode.X}, V{opcode.Y}";
                    break;
                case 0x6:
                    Description = $"LD V{opcode.X}, {opcode.Kk}";
                    break;
                case 0x7:
                    Description = $"ADD V{opcode.X}, {opcode.Kk}";
                    break;
                case 0x8:
                    switch (opcode.N)
                    {
                        case 0x0:
                            Description = $"LD V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0x1:
                            Description = $"OR V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0x2:
                            Description = $"AND V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0x3:
                            Description = $"XOR V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0x4:
                            Description = $"ADD V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0x5:
                            Description = $"SUB V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0x6:
                            Description = $"SHR V{opcode.X}";
                            break;
                        case 0x7:
                            Description = $"SUBN V{opcode.X}, V{opcode.Y}";
                            break;
                        case 0xE:
                            Description = $"SHL V{opcode.X}";
                            break;
                    }
                    break;
                case 0x9:
                    Description = $"SNE V{opcode.X}, V{opcode.Y}";
                    break;
                case 0xA:
                    Description = $"LD I, {opcode.Nnn}";
                    break;
                case 0xB:
                    Description = $"JP V0, {opcode.Nnn}";
                    break;
                case 0xC:
                    Description = $"RND V{opcode.X}, {opcode.Kk}";
                    break;
                case 0xD:
                    Description = $"DRW V{opcode.X}, {opcode.Kk}";
                    break;
                case 0xE:
                    switch (opcode.Kk)
                    {
                        case 0x9E:
                            Description = $"SKP V{opcode.X}";
                            break;
                        case 0xA1:
                            Description = $"SKNP V{opcode.X}";
                            break;
                    }
                    break;
                case 0xF:
                    switch (opcode.Kk)
                    {
                        case 0x7:
                            Description = $"LD V{opcode.X}, DT";
                            break;
                        case 0xA:
                            Description = $"LD V{opcode.X}, K";
                            break;
                        case 0x15:
                            Description = $"LD DT, V{opcode.X}";
                            break;
                        case 0x18:
                            Description = $"LD ST, V{opcode.X}";
                            break;
                        case 0x1E:
                            Description = $"ADD I, V{opcode.X}";
                            break;
                        case 0x29:
                            Description = $"LD F, V{opcode.X}";
                            break;
                        case 0x33:
                            Description = $"LD B, V{opcode.X}";
                            break;
                        case 0x55:
                            Description = $"LD [I], V{opcode.X}";
                            break;
                        case 0x65:
                            Description = $"LD V{opcode.X}, [I]";
                            break;
                    }
                    break;
            }
        }
    }
}
