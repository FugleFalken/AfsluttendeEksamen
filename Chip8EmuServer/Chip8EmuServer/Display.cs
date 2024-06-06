using System.Drawing;

namespace Chip8EmuServer
{
    public class Display
    {
        #region "Locks"
        private readonly object pixelArrayLock = new object();
        private readonly object clippingOnLock = new object();
        #endregion

        #region "Fields"
        private byte[] pixelArray;
        private bool clippingOn;
        #endregion

        public int DisplayWidth { get; private set; }
        public int DisplayHeight { get; private set; }
        public Byte[] PixelArray
        {
            get
            {
                lock (pixelArrayLock)
                {
                    return pixelArray;
                }
            }
            private set
            {
                lock(pixelArrayLock)
                {
                    pixelArray = value;
                }
            }
        }
        public bool ClippingOn { get; set; }


        public Display()
        {
            DisplayWidth = 64;
            DisplayHeight = 32;
            PixelArray = new byte[DisplayWidth * DisplayHeight / 8];
            ClippingOn = false;
            ClippingOn = false;
        }

        public byte InsertSprite(int x, int y, byte[] sprite)
        {
            int spriteWidth = 8;
            bool pixelWasErased = false;
            if (ClippingOn)
            {
                x = x % DisplayWidth;
                y = y % DisplayHeight;
            }

            for (int i = 0; i < sprite.Length; i++)
            {
                int wrappedY = (y + i) % DisplayHeight;
                for (int j = 0; j < spriteWidth; j++)
                {
                    int wrappedX = (x + j) % DisplayWidth;
                    if (ClippingOn && (wrappedX < x + j || wrappedY < y + i))
                    {
                        break;
                    }
                    int pixelPos = DisplayWidth * wrappedY + wrappedX;

                    bool spriteBit = (sprite[i] & (0x80 >> j)) != 0;

                    int bytePos = pixelPos / 8;
                    int bitPos = pixelPos % 8;

                    if (spriteBit)
                    {
                        PixelArray[bytePos] ^= (byte)(0x80 >> bitPos);
                        if ((PixelArray[bytePos] & 0x80 >> bitPos) == 0) pixelWasErased = true;
                    }


                }
            }
            return Convert.ToByte(pixelWasErased ? 1 : 0);
        }
    }
}

