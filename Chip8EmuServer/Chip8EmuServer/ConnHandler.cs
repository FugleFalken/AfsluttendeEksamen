using Chip8EmuServer.structs;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace Chip8EmuServer
{
    public class ConnHandler
    {
        public enum SendType
        {
            GameState,
            Debugger
        };
        public enum RecieveType
        {
            Program,
            Input,
            Command
        };

        public WebSocket WebSocket { get; private set; }
        public Debugger Debugger { get; set; }

        public ConnHandler(WebSocket ws)
        {
            WebSocket = ws;
        }

        public async Task Listen()
        {
            byte[] buffer = new byte[1024 * 4];
            while (WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    byte[] data = new byte[result.Count];
                    Array.Copy(buffer, data, result.Count);

                    switch((RecieveType)data[0])
                    {
                        case RecieveType.Program:
                            {
                                if (Debugger != null)
                                {
                                    Debugger.Close();
                                }
                                Debugger = new Debugger(data.Skip(1).ToArray(), Send);
                                await Send(Debugger.DebuggerSaysHello(), SendType.Debugger);
                                break;
                            }
                        case RecieveType.Input:
                            {
                                KeyAction input = new KeyAction(data.Skip(1).ToArray());
                                if(Debugger != null)
                                {
                                    Debugger.Chip8Input(input);
                                }
                                break;
                            }
                        case RecieveType.Command:
                            {
                                string command = Encoding.UTF8.GetString(data.Skip(1).ToArray());
                                if(Debugger != null)
                                {
                                    string answer = Debugger.ExecuteCommand(command);
                                    if (!string.IsNullOrEmpty(answer))
                                    {
                                        await Send(Encoding.UTF8.GetBytes(answer), SendType.Debugger);
                                    }
                                }
                                break;
                            }
                    }
                }
            }
            if (Debugger != null)
            {
                Debugger.Close();
            }
        }

        public async Task Send(byte[] toSend, SendType type)
        {
            if (WebSocket.State == WebSocketState.Open)
            {
                byte[] bytes = new byte[] { (byte)type }.Concat(toSend).ToArray();
                ArraySegment<byte> arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                await WebSocket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
    }
}
