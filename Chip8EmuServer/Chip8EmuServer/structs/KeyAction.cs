namespace Chip8EmuServer.structs
{
    public struct KeyAction
    {
        public int Key { get; set; }
        public bool IsPressed { get; set; }

        public KeyAction(byte[] input)
        {
            Key = input[0];
            IsPressed = input[1] > 0;
        }
    }
}
