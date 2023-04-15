using SteamKit2;

namespace SteamKit.Repro;

/*
 * IHostedService To Execute SteamKit2 Callbacks & Manage Connection
 */
public class SteamService : IHostedService
{
    private readonly ILogger _logger;
    
    private readonly SteamClient _client;
    private readonly CallbackManager _manager;
    private readonly SteamUser _steamUser;
    private readonly SteamUserStats _steamUserStats;

    public SteamService(ILogger<SteamService> logger)
    {
        _logger = logger;
        
        _client = new SteamClient();
        _manager = new CallbackManager(_client);
        
        _steamUser = _client?.GetHandler<SteamUser>() ?? throw new Exception("Unable to get User Handler");
        _steamUserStats = _client?.GetHandler<SteamUserStats>() ?? throw new Exception("Unable to get User Stats Handler");
        
        _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        
        // Simple callback to log user stats
        _manager.Subscribe<SteamUserStats.NumberOfPlayersCallback>(OnUserStatsFetched);
    }

    void OnUserStatsFetched(SteamUserStats.NumberOfPlayersCallback callback)
    {
        _logger.LogInformation("Steam user stats fetched: {Players}", callback.NumPlayers);   
    }
    
    private async Task FetchUserStats()
    {
        while (_client.IsConnected && _steamUser.SteamID != null)
        {
            try
            {
                _logger.LogInformation("Attempting to fetch Dota 2 User Stats...");
                
                const uint dota2AppId = 570;
                var userStats = await _steamUserStats.GetNumberOfCurrentPlayers(dota2AppId);
            
                _logger.LogInformation("Dota 2 User Stats: {0}", userStats);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to fetch user stats");
                throw;
            }
        }
        
        _logger.LogWarning("Stop fetching user stats");
    }
    
    // IHosted Service
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Connect();
        
        // Main Execution Loop
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Executing Steam Callbacks...");
            _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SteamService stopped");
        return Task.CompletedTask;
    }
    
    // Connection Handlers
    void OnConnected(SteamClient.ConnectedCallback callback)
    {
        _logger.LogInformation($"Connected to Steam network. Logging in...");
        _steamUser.LogOnAnonymous();
    }

    void OnDisconnected(SteamClient.DisconnectedCallback callback) => _logger.LogInformation("Disconnected from Steam network.");

    // Auth Handlers
    void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        switch (callback.Result)
        {
            case EResult.OK:
                // On Successful login, begin to fetch user stats
                _logger.LogInformation($"Successfully logged on");
                Task.Run(FetchUserStats);
                return;

            case EResult.AccountLogonDenied:
                _logger.LogInformation("Unable to log in");
                break;
            
            default:
                _logger.LogInformation($"Unable to Login: {callback.Result} | {callback.ExtendedResult}");
                return;
        }
    }
    
    void OnLoggedOff(SteamUser.LoggedOffCallback callback) => _logger.LogInformation("User has been logged off.");
}
