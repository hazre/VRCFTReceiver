using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elements.Core;
using ResoniteModLoader;
using VRC.OSCQuery;

namespace VRCFTReceiver;

public class OscQuery : IDisposable
{
    public readonly List<OSCQueryServiceProfile> Profiles = new List<OSCQueryServiceProfile>();
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Thread _oscQueryThread;
    private bool _disposed;

    public OscQuery(int udpPort)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _oscQueryThread = new Thread(() => RunOscQuery(udpPort));
        _oscQueryThread.Start();
    }

    public OSCQueryService Service { get; private set; }

    private void RunOscQuery(int udpPort)
    {
        int tcpPort = Extensions.GetAvailableTcpPort();

        Service = new OSCQueryServiceBuilder()
                  .WithDiscovery(new MeaModDiscovery())
                  .WithTcpPort(tcpPort)
                  .WithUdpPort(udpPort)
                  .WithServiceName($"VRChat-Client-VRCFTReceiver-{Utils.RandomString()}") // Yes this has to start with "VRChat-Client" https://github.com/benaclejames/VRCFaceTracking/blob/f687b143037f8f1a37a3aabf97baa06309b500a1/VRCFaceTracking.Core/mDNS/MulticastDnsService.cs#L195
                  .StartHttpServer()
                  .AdvertiseOSCQuery()
                  .AdvertiseOSC()
                  .Build();

        ResoniteMod.Msg($"Started OSCQueryService {Service.ServerName} at TCP {tcpPort}, UDP {udpPort}, HTTP http://{Service.HostIP}:{tcpPort}");

        string avatar = VrcftReceiver.Config.GetValue(VrcftReceiver.AvatarName);
        Service.AddEndpoint<string>("/avatar/change", Attributes.AccessValues.ReadWrite, [avatar]);
        DebugLogger.Msg($"Added Service - /avatar/change, rw, {avatar}");
        Service.AddEndpoint<string>("/avatar/name", Attributes.AccessValues.ReadWrite, [avatar]);
        DebugLogger.Msg($"Added Service - /avatar/name, rw, {avatar}");
        Service.AddEndpoint<string>("/avatar/id", Attributes.AccessValues.ReadWrite, [avatar]);
        DebugLogger.Msg($"Added Service - /avatar/id, rw, {avatar}");
        Service.AddEndpoint<string>("/avatar/url", Attributes.AccessValues.ReadWrite, [""]);
        DebugLogger.Msg("Added Service - /avatar/url, rw, \"\"");
        Service.AddEndpoint<bool>("/avatar/loaded", Attributes.AccessValues.ReadWrite, [false]);
        DebugLogger.Msg("Added Service - /avatar/loaded, rw, false");

        AddParametersToEndpoint();

        Service.OnOscQueryServiceAdded += AddProfileToList;

        StartAutoRefreshServices(30000, _cancellationTokenSource.Token);
    }

    private void AddProfileToList(OSCQueryServiceProfile profile)
    {
        if (Profiles.Contains(profile) || profile.port == Service.TcpPort)
            return;

        Profiles.Add(profile);

        ResoniteMod.Msg($"Added {profile.name} to list of OSCQuery profiles, at address http://{profile.address}:{profile.port}");
    }

    private void AddParametersToEndpoint()
    {
        foreach (string parameter in Expressions.AllAddresses)
        {
            Service.AddEndpoint<float>(parameter, Attributes.AccessValues.ReadWrite, [0f]);
            DebugLogger.Msg($"Added Service - {parameter}, rw, 0f");
        }
    }

    private void StartAutoRefreshServices(double interval, CancellationToken cancellationToken)
    {
        ResoniteMod.Msg("OSCQuery start StartAutoRefreshServices");
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Service.RefreshServices();
                    DebugLogger.Msg("OSCQuery RefreshedServices");
                    await Task.Delay(TimeSpan.FromMilliseconds(interval), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ResoniteMod.Msg($"Error in AutoRefreshServices: {ex.Message}");
                }
            }
        }, cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch { }

        try
        {
            if (!_oscQueryThread.Join(TimeSpan.FromSeconds(2)))
                _oscQueryThread.Interrupt();
        }
        catch { }

        if (disposing)
        {
            try
            {
                Service?.Dispose();
            }
            catch { }

            _cancellationTokenSource.Dispose();
            Profiles.Clear();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~OscQuery()
    {
        Dispose(false);
    }
}