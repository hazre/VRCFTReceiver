﻿using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Elements.Core;

namespace VRCFTReceiver
{
    // https://github.com/dfgHiatus/VRCFT-Module-Wrapper/blob/master/VRCFTModuleWrapper/OSC/VRCFTOSC.cs
    public class VRCFTOSC
    {
        private Socket _receiver;
        private bool _loop = true;
        private Thread _thread;
        public static Dictionary<World, Dictionary<string, ValueStream<float>>> VRCFTDictionary = new();

        public void Init(int port, int timeout)
        {
            Loader.Msg("Initializing VRCFTOSC Client");
            if (_receiver != null)
            {
                return;
            }

            _receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _receiver.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            _receiver.ReceiveTimeout = timeout;

            _loop = true;
            _thread = new Thread(() => ListenLoop(port, timeout));
            _thread.Start();
            Loader.Msg("VRCFTOSC Loop Started");
        }

        public void SendAvatarRequest(int avatarport)
        {
            _receiver.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), avatarport));

            string address = "/avatar/change";
            string type = "s";
            string value = "vrc_parameters";

            byte[] message = OscMessageToByteArray(address, type, value);
            _receiver.Send(message, message.Length, SocketFlags.None);
            Loader.Msg("Sent SendAvatarRequest");
        } 

        // Encode an OSC message
        byte[] OscMessageToByteArray(string address, string type, string value)
        {
            List<byte> data = new List<byte>();

            // Add address
            data.AddRange(Encoding.ASCII.GetBytes(address));
            data.Add(0); // OSC strings must be null-terminated

            // Pad to nearest 4 bytes
            while (data.Count % 4 != 0)
            {
                data.Add(0);
            }

            // Add type tag string
            data.AddRange(Encoding.ASCII.GetBytes("," + type));
            data.Add(0); // OSC strings must be null-terminated

            // Pad to nearest 4 bytes
            while (data.Count % 4 != 0)
            {
                data.Add(0);
            }

            // Add value
            data.AddRange(Encoding.ASCII.GetBytes(value));
            data.Add(0); // OSC strings must be null-terminated

            // Pad to nearest 4 bytes
            while (data.Count % 4 != 0)
            {
                data.Add(0);
            }

            return data.ToArray();
        }

        private struct Msg
        {
            public string address;
            public float value;
            public bool success;
        }

        // https://github.com/benaclejames/SimpleRustOSC/blob/master/src/lib.rs#L54
        private Msg ParseOSC(byte[] buffer, int length)
        {
            Msg msg = new Msg();
            msg.success = false;

            if (length < 4)
                return msg;

            int bufferPosition = 0;
            string address = ParseString(buffer, length, ref bufferPosition);
            if (address == "")
                return msg;

            msg.address = address;

            // checking for ',' char
            if (buffer[bufferPosition] != 44)
                return msg;
            bufferPosition++; // skipping ',' character

            char valueType = (char)buffer[bufferPosition]; // unused
            bufferPosition++;

            float value = ParesFloat(buffer, length, bufferPosition);

            msg.value = value;
            msg.success = true;

            return msg;
        }

        private string ParseString(byte[] buffer, int length, ref int bufferPosition)
        {
            string address = "";

            // first character must be '/'
            if (buffer[0] != 47)
                return address;

            for (int i = 0; i < length; i++)
            {
                if (buffer[i] == 0)
                {
                    bufferPosition = i + 1;

                    if (bufferPosition % 4 != 0)
                    {
                        bufferPosition += 4 - bufferPosition % 4;
                    }

                    break;
                }

                address += (char)buffer[i];
            }

            return address;
        }

        private float ParesFloat(byte[] buffer, int length, int bufferPosition)
        {
            var valueBuffer = new byte[length - bufferPosition];

            int j = 0;
            for (int i = bufferPosition; i < length; i++)
            {
                valueBuffer[j] = buffer[i];

                j++;
            }

            float value = bytesToFLoat(valueBuffer);
            return value;
        }

        private float bytesToFLoat(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes); // Convert big endian to little endian
            }

            float myFloat = BitConverter.ToSingle(bytes, 0);
            return myFloat;
        }

        private void ListenLoop(int port, int timeout)
        {
            var buffer = new byte[4096];

            while (_loop)
            {
                try
                {
                    if (_receiver.IsBound)
                    {
                        var length = _receiver.Receive(buffer);

                        Msg msg = ParseOSC(buffer, length);
                        if (msg.success)
                        {
                            var focus = Engine.Current.WorldManager?.FocusedWorld;
                            // If world is not available
                            if (focus != null)
                            {
                                // Get or create lookup for world
                                if (!VRCFTDictionary.TryGetValue(focus, out var lookup))
                                {
                                    lookup = new();
                                    VRCFTDictionary[focus] = lookup;
                                }

                                // user root if null
                                if (focus.LocalUser.Root == null)
                                {
                                    Loader.Warn("Root not Found");
                                    continue;
                                };

                                // Get or create stream for address
                                if (!lookup.TryGetValue(msg.address, out var stream) || (stream != null && stream.IsRemoved))
                                {

                                    var parameter = msg.address.Substring(msg.address.LastIndexOf('/') + 1);
                                    lookup[msg.address] = null;
                                    focus.RunInUpdates(0, () =>
                                    {
                                        var s = Loader.CreateStream(focus, parameter);
                                        lookup[msg.address] = s;
                                        Loader.Msg("Stream Created: " + msg.address);
                                    });
                                }
                                if (stream != null)
                                {
                                    stream.Value = msg.value;
                                    stream.ForceUpdate();
                                }
                            }
                        }
                    }
                    else
                    {
                        _receiver.Close();
                        _receiver.Dispose();
                        _receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        _receiver.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
                        _receiver.ReceiveTimeout = timeout;
                    }
                }
                catch (Exception e) {
                    Loader.Error(e.ToString());
                }
            }
        }

        public void Teardown()
        {
            _loop = false;
            _receiver.Close();
            _receiver.Dispose();
            _thread.Join();
        }
    }
}