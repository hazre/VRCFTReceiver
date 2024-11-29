using System.Net;
using System.Reflection;
using Rug.Osc;
using VRC.OSCQuery;

namespace VRCFTReceiver
{
  public class OSCQuery
  {
    public OSCQueryService? service { get; private set; }
    public readonly List<OSCQueryServiceProfile> profiles = [];
    private CancellationTokenSource _cancellationTokenSource;
    private Thread _oscQueryThread;

    public OSCQuery(int udpPort, int tcpPort)
    {
      _cancellationTokenSource = new CancellationTokenSource();
      _oscQueryThread = new Thread(() => RunOSCQuery(udpPort, tcpPort));
      _oscQueryThread.Start();
    }

    private void RunOSCQuery(int udpPort, int tcpPort)
    {
      service = new OSCQueryServiceBuilder()
        .WithDiscovery(new MeaModDiscovery())
        .WithTcpPort(tcpPort)
        .WithUdpPort(udpPort)
        .WithServiceName($"VRChat-Client-VRCFTReceiver-{RandomString()}")
        .StartHttpServer()
        .AdvertiseOSCQuery()
        .AdvertiseOSC()
        .Build();

      VRCFTReceiver.Msg($"Started OSCQueryService {service.ServerName} at TCP {tcpPort}, UDP {udpPort}, HTTP http://{service.HostIP}:{tcpPort}");

      service.AddEndpoint<string>("/avatar/change", Attributes.AccessValues.ReadWrite, ["default"]);

      AddParametersToEndpoint();

      service.OnOscQueryServiceAdded += AddProfileToList;

      StartAutoRefreshServices(5000, _cancellationTokenSource.Token);
    }

    private void AddProfileToList(OSCQueryServiceProfile profile)
    {
      if (profiles.Any(p => p.name == profile.name) || profile.port == service!.TcpPort)
      {
        return;
      }
      lock (profiles)
      {
        profiles.Add(profile);
      }
      VRCFTReceiver.Msg($"Added {profile.name} to list of OSCQuery profiles, at address http://{profile.address}:{profile.port}");
    }

    private IEnumerable<string> GetAllOSCParameters()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var types = assembly.GetTypes();

      var parameters = new HashSet<string>();
      foreach (var type in types)
      {
        // Get both fields and properties
        var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property);

        foreach (var member in members)
        {
          var attr = member.GetCustomAttribute<OSCMapAttribute>();
          if (attr != null)
          {
            parameters.UnionWith(attr.Paths);
          }
        }
      }

      return parameters;
    }

    private void AddParametersToEndpoint()
    {
      foreach (var parameter in GetAllOSCParameters())
      {
        service!.AddEndpoint<float>(parameter, Attributes.AccessValues.ReadWrite, [0f]);
      }
    }

    private void StartAutoRefreshServices(double interval, CancellationToken cancellationToken)
    {
      VRCFTReceiver.Msg("OSCQuery start StartAutoRefreshServices");
      Task.Run(async () =>
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          try
          {
            service!.RefreshServices();
            VRCFTReceiver.Msg("OSCQuery RefreshedServices");
            await Task.Delay(TimeSpan.FromMilliseconds(interval), cancellationToken);
          }
          catch (OperationCanceledException)
          {
            break;
          }
          catch (Exception ex)
          {
            VRCFTReceiver.Msg($"Error in AutoRefreshServices: {ex.Message}");
          }
        }
      }, cancellationToken);
    }

    public void Teardown()
    {
      VRCFTReceiver.Msg("OSCQuery teardown called");
      _cancellationTokenSource.Cancel();
      _oscQueryThread.Join(); // Wait for the thread to finish
      _cancellationTokenSource.Dispose();
      service!.Dispose();
      VRCFTReceiver.Msg("OSCQuery teardown completed");
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
        VRCFTReceiver.Msg($"Sent OSC message to {ipAddress}:{port} - Address: {address}, Value: {value}");
      }
      catch (Exception ex)
      {
        VRCFTReceiver.Msg($"Error sending OSC message: {ex.Message}");
      }
    }
  }
}
