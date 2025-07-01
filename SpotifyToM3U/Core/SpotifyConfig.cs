using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpotifyToM3U.Core
{
    public class SpotifyConfig
    {
        public string ClientId { get; set; } = "your_client_id_here";
        public string ClientSecret { get; set; } = "your_client_secret_here";
        public string RedirectUri { get; set; } = "http://127.0.0.1:5000/callback";
        public List<string> Scopes { get; set; } = new()
        {
            SpotifyAPI.Web.Scopes.PlaylistReadPrivate,
            SpotifyAPI.Web.Scopes.PlaylistReadCollaborative,
            SpotifyAPI.Web.Scopes.UserLibraryRead,
            SpotifyAPI.Web.Scopes.UserReadPrivate
        };
    }

    public class SpotifyAuthToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
    }

    public interface ISpotifyService
    {
        bool IsAuthenticated { get; }
        string CurrentUserName { get; }
        bool IsInitialized { get; }
        event EventHandler<bool> AuthenticationStateChanged;

        Task InitializeAsync();
        Task<bool> AuthenticateAsync();
        Task LogoutAsync();
        Task<IEnumerable<FullPlaylist>> GetUserPlaylistsAsync();
        Task<FullPlaylist> GetPlaylistAsync(string playlistId);
        Task<List<PlaylistTrack<IPlayableItem>>> GetAllPlaylistTracksAsync(string playlistId);
        Task<FullTrack?> GetTrackAsync(string trackId);
    }

    public class SpotifyService : ISpotifyService
    {
        private SpotifyConfig _config;
        private readonly string _tokenFilePath;
        private SpotifyClient? _spotify;
        private SpotifyClient? _publicSpotify; // For public access without user authentication
        private EmbedIOAuthServer? _server;
        private SpotifyAuthToken? _currentToken;
        private readonly object _initializationLock = new();
        private volatile bool _isInitialized = false;
        private Task? _initializationTask;

        public bool IsAuthenticated => _spotify != null && _currentToken != null && !_currentToken.IsExpired;
        public string CurrentUserName { get; private set; } = string.Empty;
        public bool IsInitialized => _isInitialized;

        public event EventHandler<bool>? AuthenticationStateChanged;

        public SpotifyService()
        {
            _config = LoadConfig();
            _tokenFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpotifyToM3U",
                "spotify_token.json"
            );
        }

        public async Task InitializeAsync()
        {
            // Use double-checked locking pattern for thread safety
            if (_isInitialized)
                return;

            lock (_initializationLock)
            {
                if (_isInitialized)
                    return;

                // If no task is running, start one
                if (_initializationTask == null)
                {
                    _initializationTask = InitializeInternalAsync();
                }
            }

            // Wait for the initialization task to complete
            await _initializationTask;
        }

        private async Task InitializeInternalAsync()
        {
            // Double check if already initialized
            if (_isInitialized)
                return;

            try
            {
                Debug.WriteLine("Starting Spotify service initialization...");

                // Initialize public client for accessing public playlists
                await InitializePublicClientAsync();

                // Try to restore user authentication
                bool authRestored = await TryLoadStoredTokenAsync();

                Debug.WriteLine($"Spotify service initialization completed. Auth restored: {authRestored}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initialization error: {ex.Message}");
            }
            finally
            {
                // Always mark as initialized, even if there was an error
                _isInitialized = true;
            }
        }

        private async Task InitializePublicClientAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.ClientId) || _config.ClientId == "your_client_id_here" ||
                    string.IsNullOrEmpty(_config.ClientSecret) || _config.ClientSecret == "your_client_secret_here")
                {
                    Debug.WriteLine("Spotify credentials not configured, public access disabled");
                    return;
                }

                // Use Client Credentials flow for public access
                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault();
                ClientCredentialsRequest request = new(_config.ClientId, _config.ClientSecret);
                ClientCredentialsTokenResponse response = await new OAuthClient(config).RequestToken(request);

                _publicSpotify = new SpotifyClient(config.WithToken(response.AccessToken));
                Debug.WriteLine("Public Spotify client initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize public Spotify client: {ex.Message}");
                _publicSpotify = null;
            }
        }

        private SpotifyConfig LoadConfig()
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpotifyToM3U",
                "spotify_config.json"
            );

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<SpotifyConfig>(json) ?? new SpotifyConfig();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading config: {ex.Message}");
                    return new SpotifyConfig();
                }
            }

            // Create default config
            SpotifyConfig config = new();
            SaveConfig(config);
            return config;
        }

        private void SaveConfig(SpotifyConfig config)
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SpotifyToM3U",
                    "spotify_config.json"
                );

                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                await InitializeAsync(); // Ensure initialization is complete

                if (string.IsNullOrEmpty(_config.ClientId) || _config.ClientId == "your_client_id_here")
                {
                    // Show setup window
                    MVVM.View.Windows.SpotifySetupWindow setupWindow = new();
                    setupWindow.ShowDialog();

                    if (!setupWindow.ConfigurationSaved)
                    {
                        return false;
                    }

                    // Reload configuration
                    _config = LoadConfig();

                    if (string.IsNullOrEmpty(_config.ClientId) || _config.ClientId == "your_client_id_here")
                    {
                        throw new InvalidOperationException("Spotify API configuration is still incomplete.");
                    }

                    // Reinitialize public client with new credentials
                    await InitializePublicClientAsync();
                }

                _server = new EmbedIOAuthServer(new Uri(_config.RedirectUri), 5000);
                await _server.Start();

                _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
                _server.ErrorReceived += OnErrorReceived;

                LoginRequest request = new(_server.BaseUri, _config.ClientId, LoginRequest.ResponseType.Code)
                {
                    Scope = _config.Scopes
                };

                BrowserUtil.Open(request.ToUri());
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authentication error: {ex.Message}");
                await LogoutAsync();
                throw;
            }
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            try
            {
                await _server!.Stop();

                AuthorizationCodeTokenRequest tokenRequest = new(_config.ClientId, _config.ClientSecret, response.Code, _server.BaseUri);
                AuthorizationCodeTokenResponse tokenResponse = await new OAuthClient().RequestToken(tokenRequest);

                _currentToken = new SpotifyAuthToken
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    TokenType = tokenResponse.TokenType
                };

                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(new AuthorizationCodeAuthenticator(_config.ClientId, _config.ClientSecret, tokenResponse));

                _spotify = new SpotifyClient(config);

                // Get user info
                try
                {
                    PrivateUser user = await _spotify.UserProfile.Current();
                    CurrentUserName = user.DisplayName ?? user.Id;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting user info: {ex.Message}");
                    CurrentUserName = "Spotify User";
                }

                // Save token
                await SaveTokenAsync(_currentToken);

                Debug.WriteLine($"Authentication successful for user: {CurrentUserName}");
                AuthenticationStateChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Token processing error: {ex.Message}");
                await LogoutAsync();
            }
        }

        private async Task OnErrorReceived(object sender, string error, string? state)
        {
            Debug.WriteLine($"Authentication error: {error}");
            await _server!.Stop();
            AuthenticationStateChanged?.Invoke(this, false);
        }

        private async Task<bool> TryLoadStoredTokenAsync()
        {
            try
            {
                if (!File.Exists(_tokenFilePath))
                {
                    Debug.WriteLine("No stored token found");
                    return false;
                }

                Debug.WriteLine("Found stored token file, attempting to load...");
                string json = await File.ReadAllTextAsync(_tokenFilePath);

                _currentToken = JsonSerializer.Deserialize<SpotifyAuthToken>(json);

                if (_currentToken == null)
                {
                    Debug.WriteLine("Failed to deserialize stored token");
                    return false;
                }

                Debug.WriteLine($"Token IsExpired: {_currentToken.IsExpired}");

                if (_currentToken.IsExpired)
                {
                    Debug.WriteLine("Stored token is expired, attempting refresh");
                    return await TryRefreshTokenAsync();
                }

                Debug.WriteLine("Token appears valid, testing with Spotify API...");

                // Create token response from stored token
                AuthorizationCodeTokenResponse tokenResponse = new()
                {
                    AccessToken = _currentToken.AccessToken,
                    RefreshToken = _currentToken.RefreshToken,
                    ExpiresIn = (int)Math.Max(1, (_currentToken.ExpiresAt - DateTime.UtcNow).TotalSeconds),
                    TokenType = _currentToken.TokenType
                };

                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(new AuthorizationCodeAuthenticator(_config.ClientId, _config.ClientSecret, tokenResponse));

                _spotify = new SpotifyClient(config);

                // Test the connection and get user info
                try
                {
                    PrivateUser user = await _spotify.UserProfile.Current();
                    CurrentUserName = user.DisplayName ?? user.Id;
                    Debug.WriteLine($"Successfully restored authentication for user: {CurrentUserName}");
                    AuthenticationStateChanged?.Invoke(this, true);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Token validation failed: {ex.Message}");
                    // Try refreshing the token
                    return await TryRefreshTokenAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Token loading error: {ex.Message}");
                await LogoutAsync();
                return false;
            }
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            try
            {
                if (_currentToken?.RefreshToken == null)
                {
                    Debug.WriteLine("No refresh token available");
                    return false;
                }

                Debug.WriteLine("Attempting to refresh token");

                AuthorizationCodeRefreshRequest refreshRequest = new(_config.ClientId, _config.ClientSecret, _currentToken.RefreshToken);
                AuthorizationCodeRefreshResponse refreshResponse = await new OAuthClient().RequestToken(refreshRequest);

                _currentToken.AccessToken = refreshResponse.AccessToken;
                _currentToken.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn);
                if (!string.IsNullOrEmpty(refreshResponse.RefreshToken))
                {
                    _currentToken.RefreshToken = refreshResponse.RefreshToken;
                }

                // Create a token response object for the authenticator
                AuthorizationCodeTokenResponse tokenResponse = new()
                {
                    AccessToken = refreshResponse.AccessToken,
                    RefreshToken = _currentToken.RefreshToken,
                    ExpiresIn = refreshResponse.ExpiresIn,
                    TokenType = refreshResponse.TokenType
                };

                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(new AuthorizationCodeAuthenticator(_config.ClientId, _config.ClientSecret, tokenResponse));

                _spotify = new SpotifyClient(config);

                // Test the refreshed token
                PrivateUser user = await _spotify.UserProfile.Current();
                CurrentUserName = user.DisplayName ?? user.Id;

                // Save the refreshed token
                await SaveTokenAsync(_currentToken);

                Debug.WriteLine($"Token refreshed successfully for user: {CurrentUserName}");
                AuthenticationStateChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Token refresh failed: {ex.Message}");
                await LogoutAsync();
                return false;
            }
        }

        private async Task SaveTokenAsync(SpotifyAuthToken token)
        {
            try
            {
                string directory = Path.GetDirectoryName(_tokenFilePath)!;
                Directory.CreateDirectory(directory);
                string json = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tokenFilePath, json);
                Debug.WriteLine("Token saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Token saving error: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                Debug.WriteLine("Logging out...");

                if (_server != null)
                {
                    await _server.Stop();
                    _server = null;
                }

                _spotify = null;
                _currentToken = null;
                CurrentUserName = string.Empty;

                if (File.Exists(_tokenFilePath))
                {
                    File.Delete(_tokenFilePath);
                    Debug.WriteLine("Stored token deleted");
                }

                AuthenticationStateChanged?.Invoke(this, false);
                Debug.WriteLine("Logout completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logout error: {ex.Message}");
            }
        }

        public async Task<IEnumerable<FullPlaylist>> GetUserPlaylistsAsync()
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Not authenticated - user playlists require authentication");

            try
            {
                // Use PaginateAll like in the official examples
                IList<FullPlaylist> playlists = await _spotify!.PaginateAll(await _spotify.Playlists.CurrentUsers());
                return playlists;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching user playlists: {ex.Message}");
                throw;
            }
        }

        public async Task<FullPlaylist> GetPlaylistAsync(string playlistId)
        {
            await InitializeAsync(); // Ensure initialization is complete

            try
            {
                // Try with authenticated client first if available
                if (IsAuthenticated)
                {
                    try
                    {
                        return await _spotify!.Playlists.Get(playlistId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error fetching playlist with authenticated client: {ex.Message}");
                        // Fall through to public client
                    }
                }

                // Try with public client for public playlists
                if (_publicSpotify != null)
                {
                    try
                    {
                        return await _publicSpotify.Playlists.Get(playlistId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error fetching playlist with public client: {ex.Message}");
                        throw new InvalidOperationException($"Cannot access playlist {playlistId}. It may be private or the playlist ID is invalid.", ex);
                    }
                }

                throw new InvalidOperationException("No Spotify client available. Please check your API configuration.");
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                Debug.WriteLine($"Error fetching playlist {playlistId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PlaylistTrack<IPlayableItem>>> GetAllPlaylistTracksAsync(string playlistId)
        {
            await InitializeAsync(); // Ensure initialization is complete

            try
            {
                // Try with authenticated client first if available
                if (IsAuthenticated)
                {
                    try
                    {
                        IList<PlaylistTrack<IPlayableItem>> tracks = await _spotify!.PaginateAll(await _spotify.Playlists.GetItems(playlistId));
                        return tracks.ToList();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error fetching playlist tracks with authenticated client: {ex.Message}");
                        // Fall through to public client
                    }
                }

                // Try with public client for public playlists
                if (_publicSpotify != null)
                {
                    try
                    {
                        IList<PlaylistTrack<IPlayableItem>> tracks = await _publicSpotify.PaginateAll(await _publicSpotify.Playlists.GetItems(playlistId));
                        return tracks.ToList();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error fetching playlist tracks with public client: {ex.Message}");
                        throw new InvalidOperationException($"Cannot access tracks for playlist {playlistId}. It may be private or the playlist ID is invalid.", ex);
                    }
                }

                throw new InvalidOperationException("No Spotify client available. Please check your API configuration.");
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                Debug.WriteLine($"Error fetching playlist tracks {playlistId}: {ex.Message}");
                throw;
            }
        }

        public async Task<FullTrack?> GetTrackAsync(string trackId)
        {
            await InitializeAsync(); // Ensure initialization is complete

            try
            {
                // Try with authenticated client first if available
                if (IsAuthenticated)
                {
                    try
                    {
                        return await _spotify!.Tracks.Get(trackId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error fetching track with authenticated client: {ex.Message}");
                        // Fall through to public client
                    }
                }

                // Try with public client
                if (_publicSpotify != null)
                {
                    try
                    {
                        return await _publicSpotify.Tracks.Get(trackId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error fetching track with public client: {ex.Message}");
                        return null;
                    }
                }

                Debug.WriteLine("No Spotify client available for track fetching");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching track {trackId}: {ex.Message}");
                return null;
            }
        }
    }
}
