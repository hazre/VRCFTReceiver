using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Elements.Core;
using Rug.Osc;

namespace VRCFTReceiver
{
  public class OSCClient
  {
    private bool _oscSocketState;
    public static readonly Dictionary<string, float> FTData = new Dictionary<string, float> { };

    public OscReceiver receiver { get; private set; }
    private Thread receiveThread;
    private CancellationTokenSource cancellationTokenSource;

    private const int DefaultPort = 9000;

    public static DateTime? LastEyeTracking { get; private set; }
    public static DateTime? LastFaceTracking { get; private set; }

    private const string EYE_PREFIX = "/avatar/parameters/v2/Eye";
    private const string MOUTH_PREFIX = "/avatar/parameters/v2/Mouth";

    public OSCClient(IPAddress ip, int? port = null)
    {
      var listenPort = port ?? DefaultPort;
      receiver = new OscReceiver(ip, listenPort);

      foreach (var address in Expressions.AllAddresses)
      {
        FTData[address] = 0f;
      }

      _oscSocketState = true;
      receiver.Connect();

      cancellationTokenSource = new CancellationTokenSource();
      receiveThread = new Thread(ListenLoop);
      receiveThread.Start(cancellationTokenSource.Token);
    }

    private void ListenLoop(object obj)
    {
      UniLog.Log("[VRCFTReceiver] Started OSCClient Listen Loop");
      CancellationToken cancellationToken = (CancellationToken)obj;

      while (!cancellationToken.IsCancellationRequested && _oscSocketState)
      {
        try
        {
          if (receiver.State != OscSocketState.Connected)
          {
            UniLog.Log($"[VRCFTReceiver] OscReceiver state {receiver.State}, breaking..");
            break;
          }

          OscPacket packet = receiver.Receive();
          if (packet is OscBundle bundle)
          {
            foreach (var message in bundle)
            {
              ProcessOscMessage(message as OscMessage);
            }
          }
          // else if (packet is OscMessage message)
          // {
          //   ProcessOscMessage(message);
          // }
        }
        catch (Exception ex)
        {
          UniLog.Log($"[VRCFTReceiver] Error in OSCClient ListenLoop: {ex.Message}");
        }
      }

      UniLog.Log("[VRCFTReceiver] OSCClient ListenLoop ended");
    }

    private void ProcessOscMessage(OscMessage message)
    {
      if (message == null || !FTData.ContainsKey(message.Address))
      {
        UniLog.Log($"[VRCFTReceiver] null message or unknown address {message.Address}");
        return;
      }

      FTData[message.Address] = (float)message[0];

      if (message.Address.StartsWith(EYE_PREFIX))
      {
        LastEyeTracking = DateTime.UtcNow;
      }
      else if (message.Address.StartsWith(MOUTH_PREFIX))
      {
        LastFaceTracking = DateTime.UtcNow;
      }
    }

    public static void SendMessage(IPAddress ipAddress, int port, string address, string value)
    {
      try
      {
        using (var sender = new OscSender(ipAddress, port))
        {
          sender.Connect();
          sender.Send(new OscMessage(address, value));
        }
        UniLog.Log($"[VRCFTReceiver] Sent OSC message to {ipAddress}:{port} - Address: {address}, Value: {value}");
      }
      catch (Exception ex)
      {
        UniLog.Log($"[VRCFTReceiver] Error sending OSC message: {ex.Message}");
      }
    }

    public void Teardown()
    {
      UniLog.Log("[VRCFTReceiver] OSCClient teardown called");
      LastEyeTracking = null;
      LastFaceTracking = null;
      _oscSocketState = false;
      cancellationTokenSource?.Cancel();
      receiver?.Close();
      receiveThread?.Join(TimeSpan.FromSeconds(5));
      UniLog.Log("[VRCFTReceiver] OSCClient teardown completed");
    }
  }
}