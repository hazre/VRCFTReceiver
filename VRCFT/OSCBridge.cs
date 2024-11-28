using System.Net;
using Rug.Osc;

namespace VRCFTReceiver;

public class OSCBridge
{
    public bool Listening { get; private set; }
    public int Port => VRCFTReceiver.Config!.GetValue(VRCFTReceiver.Port_Config);
    public EventHandler<OscPacket>? ReceivedPacket;
    private Thread? listenThread;
    private CancellationTokenSource tkSrc = new();

    public bool TryStartListen()
    {
        VRCFTReceiver.Msg("Trying to start OSC listener");
        if (listenThread != null && listenThread.ThreadState == ThreadState.Running)
            return false;

        VRCFTReceiver.Msg("Starting OSC listening thread");
        tkSrc = new();
        try
        {
            VRCFTReceiver.Msg("Creating receiver");
            OscReceiver recv = new(IPAddress.Any, Port);
            VRCFTReceiver.Msg("Creating thread loop");
            listenThread = new(new ThreadStart(() => ListenLoop(recv, tkSrc.Token)));
            VRCFTReceiver.Msg("Connecting receiver");
            recv.Connect();
            VRCFTReceiver.Msg("Starting thread");
            listenThread.Start();
            VRCFTReceiver.Msg("Thread started, listening!");
            Listening = true;
            return true;
        }
        catch (Exception ex)
        {
            VRCFTReceiver.Msg($"Exception initializing OSCBridge: {ex}");
            Listening = false;
            return false;
        }
    }

    public void StopListen()
    {
        tkSrc.Cancel();
        tkSrc.Dispose();
    }

    void ListenLoop(OscReceiver recv, CancellationToken token)
    {
        Listening = true;
        AutoResetEvent ev = new(false);
        try
        {
            while (recv.State != OscSocketState.Closed)
            {

                if (token.IsCancellationRequested)
                {
                    VRCFTReceiver.Msg($"OSCListener on {recv.LocalEndPoint} was closed by request.");
                    break;
                }
                var packet = recv.Receive();

                ReceivedPacket?.Invoke(recv, packet);
            }
        }
        catch (Exception ex)
        {
            if (recv.State == OscSocketState.Connected)
            {
                VRCFTReceiver.Msg($"Exception in listener loop: {ex}");
                Listening = false;
            }
        }
    }
}