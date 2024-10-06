using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elements.Core;
using VRC.OSCQuery;

namespace VRCFTReceiver
{
  public class OSCQuery
  {
    public OSCQueryService service { get; private set; }
    public readonly List<OSCQueryServiceProfile> profiles = [];
    private CancellationTokenSource _cancellationTokenSource;

    public OSCQuery(int udpPort)
    {
      var tcpPort = Extensions.GetAvailableTcpPort();

      service = new OSCQueryServiceBuilder()
        .WithDiscovery(new MeaModDiscovery())
        .WithTcpPort(tcpPort)
        .WithUdpPort(udpPort)
        .WithServiceName($"VRChat-Client-VRCFTReceiver-{Utils.RandomString()}") // Yes this has to start with "VRChat-Client" https://github.com/benaclejames/VRCFaceTracking/blob/f687b143037f8f1a37a3aabf97baa06309b500a1/VRCFaceTracking.Core/mDNS/MulticastDnsService.cs#L195
        .StartHttpServer()
        .AdvertiseOSCQuery()
        .AdvertiseOSC()
        .Build();

      UniLog.Log($"[VRCFTReceiver] Started OSCQueryService {service.ServerName} at TCP {tcpPort}, UDP {udpPort}, HTTP http://{service.HostIP}:{tcpPort}");

      service.AddEndpoint<string>("/avatar/change", Attributes.AccessValues.ReadWrite, ["default"]);

      AddParametersToEndpoint();

      service.OnOscQueryServiceAdded += AddProfileToList;

      _cancellationTokenSource = new CancellationTokenSource();
      StartAutoRefreshServices(5000, _cancellationTokenSource.Token);
    }

    private void AddProfileToList(OSCQueryServiceProfile profile)
    {
      if (profiles.Contains(profile) || profile.port == service.TcpPort)
      {
        return;
      }
      profiles.Add(profile);
      UniLog.Log($"[VRCFTReceiver] Added {profile.name} to list of OSCQuery profiles, at address http://{profile.address}:{profile.port}");
    }

    private void AddParametersToEndpoint()
    {
      foreach (var parameter in Expressions.AllAddresses)
      {
        service.AddEndpoint<float>(parameter, Attributes.AccessValues.ReadWrite, [0f]);
      }
    }

    private void StartAutoRefreshServices(double interval, CancellationToken cancellationToken)
    {
      Task.Run(async () =>
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          try
          {
            service.RefreshServices();
            await Task.Delay(TimeSpan.FromMilliseconds(interval), cancellationToken);
          }
          catch (OperationCanceledException)
          {
            break;
          }
          catch (Exception ex)
          {
            UniLog.Log($"[VRCFTReceiver] Error in AutoRefreshServices: {ex.Message}");
          }
        }
      }, cancellationToken);
    }

    public void Teardown()
    {
      UniLog.Log("[VRCFTReceiver] OSCQuery teardown called");
      _cancellationTokenSource.Cancel();
      _cancellationTokenSource.Dispose();
      service.Dispose();
      UniLog.Log("[VRCFTReceiver] OSCQuery teardown completed");
    }
  }
}