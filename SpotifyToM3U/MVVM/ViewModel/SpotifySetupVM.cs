using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyToM3U.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SpotifyToM3U.MVVM.ViewModel
{
    public partial class SpotifySetupVM : ObservableObject
    {
        private readonly string _configPath;

        [ObservableProperty]
        private string _clientId = string.Empty;

        [ObservableProperty]
        private string _clientSecret = string.Empty;

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        private string _statusMessage = "Enter your Spotify API credentials to get started.";

        [ObservableProperty]
        private bool _hasValidationErrors = false;

        public bool ConfigurationSaved { get; private set; } = false;

        public event EventHandler? RequestClose;

        public SpotifySetupVM()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpotifyToM3U",
                "spotify_config.json"
            );

            LoadExistingConfiguration();
        }

        [RelayCommand]
        private void OpenDashboard()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://developer.spotify.com/dashboard",
                    UseShellExecute = true
                });
                StatusMessage = "Browser opened. Create your app and come back with the credentials.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open browser: {ex.Message}";
                Debug.WriteLine($"Failed to open browser: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CopyRedirectUri()
        {
            try
            {
                Clipboard.SetText("http://localhost:5000/callback");
                StatusMessage = "Redirect URI copied to clipboard!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to copy to clipboard: {ex.Message}";
                Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveConfigurationAsync()
        {
            try
            {
                IsSaving = true;
                HasValidationErrors = false;
                StatusMessage = "Validating credentials...";

                // Validate inputs
                if (!ValidateInputs())
                {
                    HasValidationErrors = true;
                    return;
                }

                // Create configuration
                SpotifyConfig config = new()
                {
                    ClientId = ClientId.Trim(),
                    ClientSecret = ClientSecret.Trim(),
                    RedirectUri = "http://localhost:5000/callback",
                    Scopes = new()
                    {
                        SpotifyAPI.Web.Scopes.PlaylistReadPrivate,
                        SpotifyAPI.Web.Scopes.PlaylistReadCollaborative,
                        SpotifyAPI.Web.Scopes.UserLibraryRead,
                        SpotifyAPI.Web.Scopes.UserReadPrivate
                    }
                };

                // Save configuration
                await SaveConfigAsync(config);

                ConfigurationSaved = true;
                StatusMessage = "Configuration saved successfully!";

                // Close window after a short delay
                await Task.Delay(1000);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save configuration: {ex.Message}";
                HasValidationErrors = true;
                Debug.WriteLine($"Failed to save configuration: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ConfigurationSaved = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateInputs()
        {
            string clientId = ClientId.Trim();
            string clientSecret = ClientSecret.Trim();

            if (string.IsNullOrEmpty(clientId))
            {
                StatusMessage = "Please enter your Client ID.";
                return false;
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                StatusMessage = "Please enter your Client Secret.";
                return false;
            }

            // Validate Client ID format (basic validation)
            if (clientId.Length < 20 || !Regex.IsMatch(clientId, @"^[a-zA-Z0-9]+$"))
            {
                StatusMessage = "Client ID format appears to be invalid. Please check and try again.";
                return false;
            }

            // Validate Client Secret format (basic validation)
            if (clientSecret.Length < 20 || !Regex.IsMatch(clientSecret, @"^[a-zA-Z0-9]+$"))
            {
                StatusMessage = "Client Secret format appears to be invalid. Please check and try again.";
                return false;
            }

            return true;
        }

        private void LoadExistingConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    SpotifyConfig? config = JsonSerializer.Deserialize<SpotifyConfig>(json);

                    if (config != null)
                    {
                        if (!string.IsNullOrEmpty(config.ClientId) && config.ClientId != "your_client_id_here")
                        {
                            ClientId = config.ClientId;
                        }

                        if (!string.IsNullOrEmpty(config.ClientSecret) && config.ClientSecret != "your_client_secret_here")
                        {
                            ClientSecret = config.ClientSecret;
                        }

                        if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret))
                        {
                            StatusMessage = "Existing configuration loaded. You can update the credentials if needed.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading existing configuration: {ex.Message}";
                Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        private async Task SaveConfigAsync(SpotifyConfig config)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configPath, json);
        }
    }
}