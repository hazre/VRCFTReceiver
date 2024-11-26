using System.Net;
using Rug.Osc;
using VRC.OSCQuery;

namespace Impressive
{
  public class OSCQuery
  {
    public OSCQueryService? service { get; private set; }
    public readonly List<OSCQueryServiceProfile> profiles = [];
    private CancellationTokenSource _cancellationTokenSource;
    private Thread _oscQueryThread;

    public OSCQuery(int udpPort)
    {
      _cancellationTokenSource = new CancellationTokenSource();
      _oscQueryThread = new Thread(() => RunOSCQuery(udpPort));
      _oscQueryThread.Start();
    }

    private void RunOSCQuery(int udpPort)
    {
      var tcpPort = GetAvailableTcpPort();

      service = new OSCQueryServiceBuilder()
        .WithDiscovery(new MeaModDiscovery())
        .WithTcpPort(tcpPort)
        .WithUdpPort(udpPort)
        .WithServiceName($"VRChat-Client-VRCFTReceiver-{RandomString()}")
        .StartHttpServer()
        .AdvertiseOSCQuery()
        .AdvertiseOSC()
        .Build();

      Impressive.Msg($"Started OSCQueryService {service.ServerName} at TCP {tcpPort}, UDP {udpPort}, HTTP http://{service.HostIP}:{tcpPort}");

      service.AddEndpoint<string>("/avatar/change", Attributes.AccessValues.ReadWrite, ["default"]);

      AddParametersToEndpoint();

      service.OnOscQueryServiceAdded += AddProfileToList;

      StartAutoRefreshServices(5000, _cancellationTokenSource.Token);
    }

    private void AddProfileToList(OSCQueryServiceProfile profile)
    {
      if (profiles.Contains(profile) || profile.port == service!.TcpPort)
      {
        return;
      }
      lock (profiles)
      {
        profiles.Add(profile);
      }
      Impressive.Msg($"Added {profile.name} to list of OSCQuery profiles, at address http://{profile.address}:{profile.port}");
    }

    private void AddParametersToEndpoint()
    {
      foreach (var parameter in Expressions.AllAddresses)
      {
        service!.AddEndpoint<float>(parameter, Attributes.AccessValues.ReadWrite, [0f]);
      }
    }

    private void StartAutoRefreshServices(double interval, CancellationToken cancellationToken)
    {
      Impressive.Msg("OSCQuery start StartAutoRefreshServices");
      Task.Run(async () =>
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          try
          {
            service!.RefreshServices();
            Impressive.Msg("OSCQuery RefreshedServices");
            await Task.Delay(TimeSpan.FromMilliseconds(interval), cancellationToken);
          }
          catch (OperationCanceledException)
          {
            break;
          }
          catch (Exception ex)
          {
            Impressive.Msg($"Error in AutoRefreshServices: {ex.Message}");
          }
        }
      }, cancellationToken);
    }

    public void Teardown()
    {
      Impressive.Msg("OSCQuery teardown called");
      _cancellationTokenSource.Cancel();
      _oscQueryThread.Join(); // Wait for the thread to finish
      _cancellationTokenSource.Dispose();
      service!.Dispose();
      Impressive.Msg("OSCQuery teardown completed");
    }

    // Utility methods to replace VRCFTReceiver's Utils methods
    private static int GetAvailableTcpPort()
    {
      var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
      listener.Start();
      int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    private static string RandomString(int length = 8)
    {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
      var random = new Random();
      return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
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
        Impressive.Msg($"Sent OSC message to {ipAddress}:{port} - Address: {address}, Value: {value}");
      }
      catch (Exception ex)
      {
        Impressive.Msg($"Error sending OSC message: {ex.Message}");
      }
    }
  }
}
