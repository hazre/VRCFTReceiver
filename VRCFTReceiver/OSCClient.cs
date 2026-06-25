using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Elements.Core;
using ResoniteModLoader;
using Rug.Osc;

namespace VRCFTReceiver;

public class OscClient : IDisposable
{
    private const int DefaultPort = 9000;
    private const string EyePrefix = "/avatar/parameters/FT/v2/Eye";
    private const string MouthPrefix = "/avatar/parameters/FT/v2/Mouth";

    public static readonly Dictionary<string, float> FtData = new Dictionary<string, float>();
    private bool _oscSocketState;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Thread _receiveThread;
    private bool _disposed;

    public OscClient(IPAddress ip, int? port = null)
    {
        int listenPort = port ?? DefaultPort;
        Receiver = new OscReceiver(ip, listenPort);

        foreach (string address in Expressions.AllAddresses)
        {
            FtData[address] = 0f;
        }

        _oscSocketState = true;
        Receiver.Connect();

        _cancellationTokenSource = new CancellationTokenSource();
        _receiveThread = new Thread(ListenLoop);
        _receiveThread.Start(_cancellationTokenSource.Token);
    }

    public OscReceiver Receiver { get; }
    public static DateTime? LastEyeTracking { get; private set; }
    public static DateTime? LastFaceTracking { get; private set; }

    private void ListenLoop(object obj)
    {
        ResoniteMod.Debug("Started OSCClient Listen Loop");
        CancellationToken cancellationToken = (CancellationToken)obj;

        while (!cancellationToken.IsCancellationRequested && _oscSocketState)
        {
            try
            {
                if (Receiver.State != OscSocketState.Connected)
                {
                    ResoniteMod.Warn($"OscReceiver state {Receiver.State}, breaking..");
                    break;
                }

                OscPacket packet = Receiver.Receive();
                if (packet is OscBundle bundle)
                {
                    foreach (OscPacket message in bundle)
                    {
                        ProcessOscMessage(message as OscMessage);
                    }
                }
                else if (packet is OscMessage message)
                {
                    ProcessOscMessage(message);
                }
            }
            catch (Exception ex)
            {
                ResoniteMod.Error($"Error in OSCClient ListenLoop: {ex.Message}");
            }
        }

        ResoniteMod.Debug("OSCClient ListenLoop ended");
    }

    private string _lastAddress;

    private void ProcessOscMessage(OscMessage message)
    {
        if (VrcftReceiver.Config.GetValue(VrcftReceiver.LogParamErrors) && (message == null || !FtData.ContainsKey(message.Address)))
        {
            ResoniteMod.Error($"Null message or unknown address {message?.Address}");
            return;
        }

        FtData[message.Address] = (float)message[0];

        if (message.Address.StartsWith(EyePrefix))
        {
            LastEyeTracking = DateTime.UtcNow;
        }
        else if (message.Address.StartsWith(MouthPrefix))
        {
            LastFaceTracking = DateTime.UtcNow;
        }

        if (_lastAddress != message.Address) DebugLogger.Debug($"Received OSC message: {message.Address} - {message[0]}");
        _lastAddress = message.Address;

        Wizard.OscValues = FtData;
    }

    public static void SendMessage(IPAddress ipAddress, int port, string address, string value)
    {
        try
        {
            using (OscSender sender = new OscSender(ipAddress, port))
            {
                sender.Connect();
                sender.Send(new OscMessage(address, value));
            }

            ResoniteMod.Msg($"Sent OSC message to {ipAddress}:{port} - Address: {address}, Value: {value}");
        }
        catch (Exception ex)
        {
            ResoniteMod.Error($"Error sending OSC message: {ex.Message}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _oscSocketState = false;
            _cancellationTokenSource.Cancel();
        }
        catch { }

        try
        {
            if (!_receiveThread.Join(TimeSpan.FromSeconds(2)))
                _receiveThread.Interrupt();
        }
        catch { }

        if (disposing)
        {
            try
            {
                Receiver.Close();
                Receiver.Dispose();
            }
            catch { }

            _cancellationTokenSource.Dispose();
        }

        LastEyeTracking = null;
        LastFaceTracking = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~OscClient()
    {
        Dispose(false);
    }
}
