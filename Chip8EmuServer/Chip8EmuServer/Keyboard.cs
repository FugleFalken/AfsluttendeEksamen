using Chip8EmuServer.structs;
using Microsoft.AspNetCore.SignalR;

namespace Chip8EmuServer
{
    public class Keyboard
    {
        #region "Locks"
        private readonly object keysLock = new object();
        private readonly object latestPressLock = new object();
        private readonly object waitingLock = new object();
        #endregion

        #region "Fields"
        private Dictionary<int, bool> keys;
        private int latestPress;
        private bool waiting;
        private EventWaitHandle waitForkeyPress;
        #endregion

        public EventWaitHandle WaitForKeyPress
        {
            get
            {
                lock (waitingLock)
                {
                    return waitForkeyPress;
                }
            }
            private set
            {
                lock (waitingLock)
                {
                    waitForkeyPress = value;
                }
            }
        }
        public Dictionary<int, bool> Keys 
        {
            get
            {
                lock (keysLock)
                {
                    return keys;
                }
            }
            private set
            {
                lock (keysLock)
                {
                    keys = value;
                }
            }
        }
        public int LatestPress
        {
            get
            {
                lock (latestPressLock)
                {
                    return latestPress;
                }
            }
            private set
            {
                lock (latestPressLock)
                {
                    latestPress = value;
                }
            }
        }
        public bool Waiting
        {
            get
            {
                lock (waitingLock)
                {
                    return waiting;
                }
            }
            private set
            {
                lock (waitingLock)
                {
                    waiting = value;
                }
            }
        }

        public Keyboard()
        {
            Keys = new Dictionary<int, bool>();
            for (int i = 0x0; i <= 0xF; i++)
            {
                Keys.Add(i, false);
            }
            WaitForKeyPress = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public bool KeyAction(KeyAction keyAction)
        {
            if (!keyAction.IsPressed)
            {
                LatestPress = keyAction.Key;
                waitForkeyPress.Set();
            }
            Keys[keyAction.Key] = keyAction.IsPressed;
            return Keys[keyAction.Key];
        }

        public bool IsPressed(int key)
        {
            return Keys[key];
        }

        public int GetNextPress()
        {
            waitForkeyPress.Reset();
            waitForkeyPress.WaitOne();
            return LatestPress;
        }
    }
}
