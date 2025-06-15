using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using SpotifyToM3U.Core;
using SpotifyToM3U.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SpotifyToM3U.MVVM.ViewModel
{
    public partial class PlaylistInfo : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _imageUrl = string.Empty;

        [ObservableProperty]
        private int _trackCount = 0;

        [ObservableProperty]
        private string _owner = string.Empty;

        [ObservableProperty]
        private bool _isSelected = false;

        [ObservableProperty]
        private bool _isPublic = true;

        public PlaylistInfo() { }

        public PlaylistInfo(FullPlaylist playlist)
        {
            Id = playlist.Id!;
            Name = playlist.Name!;
            Description = playlist.Description ?? string.Empty;
            ImageUrl = playlist.Images?.FirstOrDefault()?.Url ?? string.Empty;
            TrackCount = playlist.Tracks?.Total ?? 0;
            Owner = playlist.Owner?.DisplayName ?? playlist.Owner?.Id ?? "Unknown";
            IsPublic = playlist.Public ?? false;
        }
    }

    internal partial class SpotifyVM : ViewModelObject
    {
        private readonly ISpotifyService _spotifyService;
        private readonly LibraryVM _libraryVM;

        [ObservableProperty]
        private string _playlistIDText = string.Empty;

        [ObservableProperty]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        private string _currentUser = "Not logged in";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isNext = false;

        [ObservableProperty]
        private string _statusMessage = "Connect to Spotify to get started";

        [ObservableProperty]
        private ObservableCollection<PlaylistInfo> _userPlaylists = new();

        [ObservableProperty]
        private PlaylistInfo? _selectedPlaylist;

        [ObservableProperty]
        private ObservableCollection<Track> _playlistTracks = new();

        [ObservableProperty]
        private bool _showPlaylists = false;

        [ObservableProperty]
        private bool _showManualInput = true;

        [ObservableProperty]
        private int _playlistFound = 0;

        [ObservableProperty]
        private int _playlistLength = 0;

        [ObservableProperty]
        private string _playlistName = "Unknown Playlist";

        public SpotifyVM(INavigationService navigation) : base(navigation)
        {
            _spotifyService = App.Current.ServiceProvider.GetRequiredService<ISpotifyService>();
            _libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();

            _spotifyService.AuthenticationStateChanged += OnAuthenticationStateChanged;
            _libraryVM.AudioFilesModifified += LibraryVM_AudioFilesModified;

            BindingOperations.EnableCollectionSynchronization(PlaylistTracks, new object());
            BindingOperations.EnableCollectionSynchronization(UserPlaylists, new object());

            // Initialize authentication state
            IsAuthenticated = _spotifyService.IsAuthenticated;
            CurrentUser = _spotifyService.CurrentUserName;

            if (IsAuthenticated)
            {
                StatusMessage = $"Connected as {CurrentUser}";
                _ = LoadUserPlaylistsAsync();
            }

            // Initialize playlist stats
            UpdatePlaylistStats();
        }

        [RelayCommand]
        private async Task AuthenticateAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Connecting to Spotify...";

                if (IsAuthenticated)
                {
                    await _spotifyService.LogoutAsync();
                }
                else
                {
                    bool success = await _spotifyService.AuthenticateAsync();
                    if (!success)
                    {
                        StatusMessage = "Failed to connect to Spotify. Please check your configuration.";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Authentication error: {ex.Message}";
                MessageBox.Show($"Authentication failed: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadUserPlaylistsAsync()
        {
            if (!IsAuthenticated)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Loading your playlists...";

                UserPlaylists.Clear();
                IEnumerable<FullPlaylist> playlists = await _spotifyService.GetUserPlaylistsAsync();

                foreach (FullPlaylist playlist in playlists)
                {
                    UserPlaylists.Add(new PlaylistInfo(playlist));
                }

                ShowPlaylists = UserPlaylists.Count > 0;
                StatusMessage = $"Loaded {UserPlaylists.Count} playlists";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading playlists: {ex.Message}";
                Debug.WriteLine($"Error loading playlists: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadPlaylistAsync(PlaylistInfo? playlist = null)
        {
            try
            {
                playlist ??= SelectedPlaylist;
                if (playlist == null)
                    return;

                IsLoading = true;
                IsNext = false;
                StatusMessage = $"Loading playlist: {playlist.Name}";

                PlaylistTracks.Clear();
                SelectedPlaylist = playlist;

                List<Track> tracks = await LoadAllPlaylistTracksAsync(playlist.Id);

                foreach (Track track in tracks)
                {
                    PlaylistTracks.Add(track);
                }

                // Update stats after loading tracks
                UpdatePlaylistStats();

                // Find local matches
                await FindTracksLocalAsync(tracks);

                // Update stats after finding local matches
                UpdatePlaylistStats();

                if (PlaylistTracks.Any(x => x.IsLocal))
                {
                    IsNext = true;
                    StatusMessage = $"Loaded {PlaylistLength} tracks, found {PlaylistFound} locally";
                }
                else
                {
                    StatusMessage = $"Loaded {PlaylistLength} tracks, no local matches found";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading playlist: {ex.Message}";
                MessageBox.Show($"Error loading playlist: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadPlaylistByIdAsync()
        {
            if (string.IsNullOrWhiteSpace(PlaylistIDText))
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Loading playlist...";

                string playlistId = ExtractPlaylistId(PlaylistIDText);
                if (string.IsNullOrEmpty(playlistId))
                {
                    StatusMessage = "Invalid playlist URL or ID";
                    return;
                }

                FullPlaylist playlist = await _spotifyService.GetPlaylistAsync(playlistId);
                PlaylistInfo playlistInfo = new(playlist);

                await LoadPlaylistAsync(playlistInfo);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading playlist: {ex.Message}";
                MessageBox.Show($"Error loading playlist: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ToggleInputMode()
        {
            ShowManualInput = !ShowManualInput;
        }

        [RelayCommand]
        private void Next() => Navigation.NavigateTo<ExportVM>();

        private void UpdatePlaylistStats()
        {
            PlaylistLength = PlaylistTracks.Count;
            PlaylistFound = PlaylistTracks.Count(x => x.IsLocal);
            PlaylistName = SelectedPlaylist?.Name ?? "Unknown Playlist";
        }

        private async Task<List<Track>> LoadAllPlaylistTracksAsync(string playlistId)
        {
            List<Track> tracks = new();

            List<PlaylistTrack<IPlayableItem>> playlistTracks = await _spotifyService.GetAllPlaylistTracksAsync(playlistId);

            foreach (PlaylistTrack<IPlayableItem> item in playlistTracks)
            {
                if (item.Track is FullTrack fullTrack)
                {
                    Track track = ConvertToTrack(fullTrack);
                    tracks.Add(track);
                }
            }

            return tracks;
        }

        private Track ConvertToTrack(FullTrack spotifyTrack)
        {
            Track track = new()
            {
                Id = spotifyTrack.Id,
                Title = spotifyTrack.Name,
                CutTitle = IOManager.CutString(spotifyTrack.Name),
                Uri = spotifyTrack.Uri,
                IsLocal = false,
                Path = string.Empty
            };

            // Set album
            track.Album = new Album
            {
                Id = spotifyTrack.Album.Id,
                Name = spotifyTrack.Album.Name,
                Uri = spotifyTrack.Album.Uri,
                Images = spotifyTrack.Album.Images?.Select(img => new Model.Image
                {
                    Url = new Uri(img.Url),
                    Height = img.Height,
                    Width = img.Width
                }).ToArray() ?? Array.Empty<Model.Image>(),
                TotalTracks = spotifyTrack.Album.TotalTracks
            };

            // Set artists
            track.Artists = spotifyTrack.Artists?.Select(artist => new Owner
            {
                Id = artist.Id,
                Name = artist.Name,
                CutName = IOManager.CutString(artist.Name),
                Uri = artist.Uri,
                DisplayName = artist.Name
            }).ToArray() ?? Array.Empty<Owner>();

            return track;
        }

        private async Task FindTracksLocalAsync(List<Track> spotifyTracks)
        {
            await Task.Run(() =>
            {
                foreach (Track track in spotifyTracks)
                {
                    IEnumerable<AudioFile> found = _libraryVM.AudioFiles.Where(audio =>
                    {
                        double title = CalculateSimilarity(audio.CutTitle, track.CutTitle);
                        if (title < 0.5)
                            return false;

                        string audioFirstArtist = audio.CutArtists.FirstOrDefault() ?? "";
                        string trackFirstArtist = track.Artists.FirstOrDefault()?.CutName ?? "";

                        double firstArtist = CalculateSimilarity(audioFirstArtist, trackFirstArtist);
                        if (title + firstArtist > 1.5)
                        {
                            audio.TrackValueDictionary.TryAdd(track, title + firstArtist + 0.8);
                            return true;
                        }

                        string audioSecondArtist = audio.CutArtists.Length > 1 ? audio.CutArtists[1] : "";
                        string trackSecondArtist = track.Artists.Length > 1 ? track.Artists[1].CutName : "";

                        double secondArtist = Math.Max(
                            Math.Max(CalculateSimilarity(audioSecondArtist, trackSecondArtist),
                                    CalculateSimilarity(audioSecondArtist, trackFirstArtist)),
                            CalculateSimilarity(audioFirstArtist, trackSecondArtist));

                        if (title + secondArtist > 1.5)
                        {
                            audio.TrackValueDictionary.TryAdd(track, title + secondArtist + 0.8);
                            return true;
                        }

                        double album = CalculateSimilarity(audio.Album?.ToLower(), track.Album?.Name?.ToLower());
                        if (title + album + Math.Max(firstArtist, secondArtist) > 2.2)
                        {
                            audio.TrackValueDictionary.TryAdd(track, album + title + Math.Max(firstArtist, secondArtist));
                            return true;
                        }

                        return false;
                    });

                    if (found.Any())
                    {
                        AudioFile bestMatch = found.OrderBy(x =>
                            x.TrackValueDictionary.TryGetValue(track, out double value) ? -value : 0).First();

                        track.IsLocal = true;
                        track.Path = bestMatch.Location;
                    }
                }
            });
        }

        private string ExtractPlaylistId(string input)
        {
            input = input.Trim();

            // Try to extract from Spotify URL
            Match match = new Regex(@"(?:open\.spotify\.com/playlist/|playlist:)([a-zA-Z0-9]+)", RegexOptions.IgnoreCase)
                .Match(input);

            if (match.Success)
                return match.Groups[1].Value;

            // If it's already just an ID
            if (input.Length >= 20 && input.All(c => char.IsLetterOrDigit(c)))
                return input;

            return string.Empty;
        }

        private static double CalculateSimilarity(string? source, string? target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return 0.0;
            if (source == target)
                return 1.0;

            int stepsToSame = LevenshteinDistance(source, target);
            return 1.0 - (stepsToSame / (double)Math.Max(source.Length, target.Length));
        }

        private static int LevenshteinDistance(string source, string target)
        {
            if (source == target) return 0;
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            int[] v0 = new int[target.Length + 1];
            int[] v1 = new int[target.Length + 1];

            for (int i = 0; i < v0.Length; i++)
                v0[i] = i;

            for (int i = 0; i < source.Length; i++)
            {
                v1[0] = i + 1;

                for (int j = 0; j < target.Length; j++)
                {
                    int cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }

                for (int j = 0; j < v0.Length; j++)
                    v0[j] = v1[j];
            }

            return v1[target.Length];
        }

        private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsAuthenticated = isAuthenticated;
                CurrentUser = _spotifyService.CurrentUserName;

                if (isAuthenticated)
                {
                    StatusMessage = $"Connected as {CurrentUser}";
                    _ = LoadUserPlaylistsAsync();
                }
                else
                {
                    StatusMessage = "Not connected to Spotify";
                    UserPlaylists.Clear();
                    ShowPlaylists = false;
                    SelectedPlaylist = null;
                    PlaylistTracks.Clear();
                    IsNext = false;

                    // Reset playlist stats
                    UpdatePlaylistStats();
                }
            });
        }

        private void LibraryVM_AudioFilesModified(object? sender, EventArgs e)
        {
            if (PlaylistTracks.Any())
            {
                Task.Run(async () =>
                {
                    IsLoading = true;
                    IsNext = false;

                    await FindTracksLocalAsync(PlaylistTracks.ToList());

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdatePlaylistStats();

                        if (PlaylistTracks.Any(x => x.IsLocal))
                            IsNext = true;

                        IsLoading = false;
                    });
                });
            }
        }

        partial void OnSelectedPlaylistChanged(PlaylistInfo? value)
        {
            if (value != null)
            {
                _ = LoadPlaylistAsync(value);
            }
        }
    }
}