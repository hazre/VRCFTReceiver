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
    private static bool _oscSocketState;
    public static readonly Dictionary<string, float> FTDataWithAddress = [];

    private static OscReceiver _receiver;
    private static Thread _receiveThread;
    private static CancellationTokenSource _cancellationTokenSource;

    private const int DefaultPort = 9000;

    public static DateTime? LastEyeTracking { get; private set; }
    public static DateTime? LastFaceTracking { get; private set; }

    private const string EYE_PREFIX = "/avatar/parameters/v2/Eye";
    private const string MOUTH_PREFIX = "/avatar/parameters/v2/Mouth";

    public OSCClient(IPAddress ip, int? port = null)
    {
      if (_receiver != null)
      {
        return;
      }

      var listenPort = port ?? DefaultPort;
      _receiver = new OscReceiver(ip, listenPort);

      foreach (var address in Expressions.AllAddresses)
      {
        FTDataWithAddress[address] = 0f;
      }

      _oscSocketState = true;
      _receiver.Connect();

      _cancellationTokenSource = new CancellationTokenSource();
      _receiveThread = new Thread(ListenLoop);
      _receiveThread.Start(_cancellationTokenSource.Token);
    }

    private static void ListenLoop(object obj)
    {
      CancellationToken cancellationToken = (CancellationToken)obj;
      UniLog.Log("Started VRCFTReceiver loop");

      while (!cancellationToken.IsCancellationRequested && _oscSocketState)
      {
        try
        {
          if (_receiver.State != OscSocketState.Connected)
            break;

          OscPacket packet = _receiver.Receive();
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
          UniLog.Log($"Error in OSC receive loop: {ex.Message}");
        }
      }

      UniLog.Log("VRCFTReceiver loop ended");
    }

    private static void ProcessOscMessage(OscMessage message)
    {
      if (message == null || !FTDataWithAddress.ContainsKey(message.Address))
        return;

      if (message.Count > 0)
      {
        var value = message[0];
        if (value is float floatValue)
        {
          FTDataWithAddress[message.Address] = floatValue;

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
          UniLog.Log($"Unknown OSC type for address {message.Address}: {value.GetType()}");
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
        UniLog.Log($"Sent OSC message to {ipAddress}:{port} - Address: {address}, Value: {value}");
      }
      catch (Exception ex)
      {
        UniLog.Log($"Error sending OSC message: {ex.Message}");
      }
    }

    public void Teardown()
    {
      UniLog.Log("VRCFTReceiver teardown called");
      LastEyeTracking = null;
      LastFaceTracking = null;
      _oscSocketState = false;
      _cancellationTokenSource?.Cancel();
      _receiver?.Close();
      _receiveThread?.Join(TimeSpan.FromSeconds(5));
      UniLog.Log("VRCFTReceiver teardown completed");
    }
  }
}