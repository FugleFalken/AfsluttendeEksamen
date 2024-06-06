namespace Chip8EmuServer
{
    public class GameState
    {
        public byte[] Display { get; private set; }
        public bool PlaySound { get; private set; }

        public GameState(byte[] display, bool playSound)
        {
            Display = display.ToArray();
            PlaySound = playSound;
        }

        public byte[] GetAsByteArray()
        {
            byte playSoundByte = (byte)(PlaySound ? 1 : 0);
            return new byte[] { playSoundByte }.Concat(Display).ToArray();
        }

        public override bool Equals(object? obj)
        {
            return obj is GameState gsu &&
                   EqualityComparer<byte[]>.Default.Equals(Display, gsu.Display) &&
                   PlaySound == gsu.PlaySound;
        }

    }
}
