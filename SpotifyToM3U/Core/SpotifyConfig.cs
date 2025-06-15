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
        public string RedirectUri { get; set; } = "http://localhost:5000/callback";
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
        event EventHandler<bool> AuthenticationStateChanged;

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
        private EmbedIOAuthServer? _server;
        private SpotifyAuthToken? _currentToken;

        public bool IsAuthenticated => _spotify != null && _currentToken != null && !_currentToken.IsExpired;
        public string CurrentUserName { get; private set; } = string.Empty;

        public event EventHandler<bool>? AuthenticationStateChanged;

        public SpotifyService()
        {
            _config = LoadConfig();
            _tokenFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpotifyToM3U",
                "spotify_token.json"
            );

            // Try to load existing token
            _ = Task.Run(async () => await TryLoadStoredTokenAsync());
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
                catch
                {
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
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpotifyToM3U",
                "spotify_config.json"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
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
                catch
                {
                    CurrentUserName = "Spotify User";
                }

                // Save token
                await SaveTokenAsync(_currentToken);

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
                    return false;

                string json = await File.ReadAllTextAsync(_tokenFilePath);
                _currentToken = JsonSerializer.Deserialize<SpotifyAuthToken>(json);

                if (_currentToken?.IsExpired != false)
                    return false;

                AuthorizationCodeTokenResponse tokenResponse = new()
                {
                    AccessToken = _currentToken.AccessToken,
                    RefreshToken = _currentToken.RefreshToken,
                    ExpiresIn = (int)(_currentToken.ExpiresAt - DateTime.UtcNow).TotalSeconds,
                    TokenType = _currentToken.TokenType
                };

                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(new AuthorizationCodeAuthenticator(_config.ClientId, _config.ClientSecret, tokenResponse));

                _spotify = new SpotifyClient(config);

                // Test the connection
                PrivateUser user = await _spotify.UserProfile.Current();
                CurrentUserName = user.DisplayName ?? user.Id;

                AuthenticationStateChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Token loading error: {ex.Message}");
                await LogoutAsync();
                return false;
            }
        }

        private async Task SaveTokenAsync(SpotifyAuthToken token)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_tokenFilePath)!);
                string json = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tokenFilePath, json);
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
                if (_server != null)
                {
                    await _server.Stop();
                    _server = null;
                }

                _spotify = null;
                _currentToken = null;
                CurrentUserName = string.Empty;

                if (File.Exists(_tokenFilePath))
                    File.Delete(_tokenFilePath);

                AuthenticationStateChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logout error: {ex.Message}");
            }
        }

        public async Task<IEnumerable<FullPlaylist>> GetUserPlaylistsAsync()
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Not authenticated");

            try
            {
                // Use PaginateAll like in the official examples
                IList<FullPlaylist> playlists = await _spotify!.PaginateAll(await _spotify.Playlists.CurrentUsers());
                return playlists;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching playlists: {ex.Message}");
                throw;
            }
        }

        public async Task<FullPlaylist> GetPlaylistAsync(string playlistId)
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Not authenticated");

            try
            {
                return await _spotify!.Playlists.Get(playlistId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching playlist {playlistId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PlaylistTrack<IPlayableItem>>> GetAllPlaylistTracksAsync(string playlistId)
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Not authenticated");

            try
            {
                // Use PaginateAll for consistency with other methods
                IList<PlaylistTrack<IPlayableItem>> tracks = await _spotify!.PaginateAll(await _spotify.Playlists.GetItems(playlistId));
                return tracks.ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching playlist tracks {playlistId}: {ex.Message}");
                throw;
            }
        }

        public async Task<FullTrack?> GetTrackAsync(string trackId)
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Not authenticated");

            try
            {
                return await _spotify!.Tracks.Get(trackId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching track {trackId}: {ex.Message}");
                return null;
            }
        }
    }
}