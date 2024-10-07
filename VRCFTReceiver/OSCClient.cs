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
      UniLog.Log("[VRCFTReceiver] start OSCClient");
      var listenPort = port ?? DefaultPort;
      receiver = new OscReceiver(ip, listenPort);
      UniLog.Log("[VRCFTReceiver] created OscReceiver");

      foreach (var address in Expressions.AllAddresses)
      {
        FTData[address] = 0f;
      }
      UniLog.Log("[VRCFTReceiver] assigned AllAddresses");

      _oscSocketState = true;
      receiver.Connect();
      UniLog.Log("[VRCFTReceiver] connect OscReceiver");

      UniLog.Log("[VRCFTReceiver] start cancellationTokenSource");
      cancellationTokenSource = new CancellationTokenSource();
      UniLog.Log("[VRCFTReceiver] new receiveThread");
      receiveThread = new Thread(ListenLoop);
      UniLog.Log("[VRCFTReceiver] start receiveThread");
      receiveThread.Start(cancellationTokenSource.Token);
    }

    private void ListenLoop(object obj)
    {
      UniLog.Log("[VRCFTReceiver] Started OSCClient ListenLoop");
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
          UniLog.Log($"[VRCFTReceiver] OscReceiver received packet");
          if (packet is OscBundle bundle)
          {
            foreach (var message in bundle)
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
          UniLog.Log($"[VRCFTReceiver] Error in OSCClient ListenLoop: {ex.Message}");
        }
      }

      UniLog.Log("[VRCFTReceiver] OSCClient ListenLoop ended");
    }

    private void ProcessOscMessage(OscMessage message)
    {
      UniLog.Log($"[VRCFTReceiver] start ProcessOscMessage");
      if (message == null || !FTData.ContainsKey(message.Address))
        return;

      if (message.Count > 0)
      {
        var value = message[0];
        if (value is float floatValue)
        {
          FTData[message.Address] = floatValue;

          // Update tracking timestamps
          if (message.Address.StartsWith(EYE_PREFIX))
          {
            LastEyeTracking = DateTime.UtcNow;
          }
          else if (message.Address.StartsWith(MOUTH_PREFIX))
          {
            LastFaceTracking = DateTime.UtcNow;
          }
        }
        else
        {
          UniLog.Log($"[VRCFTReceiver] Unknown OSC type for address {message.Address}: {value.GetType()}");
        }
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