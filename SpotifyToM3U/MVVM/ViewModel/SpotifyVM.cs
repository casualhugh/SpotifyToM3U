using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloaderLibrary.Web;
using Microsoft.Extensions.DependencyInjection;
using Requests;
using SpotifyToM3U.Core;
using SpotifyToM3U.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SpotifyToM3U.MVVM.ViewModel
{
    internal partial class SpotifyVM : ViewModelObject
    {
        [ObservableProperty]
        private string _playlistIDText = string.Empty;
        private string _playlistIDTextOld = string.Empty;

        [ObservableProperty]
        private bool _isLoadButtonEnabeld = true;

        [ObservableProperty]
        private bool _isInfoVisible = false;
        public int PlaylistFound => PlaylistTracks.Where(x => x.IsLocal)?.Count() ?? 0;

        public int PlaylistLength => PlaylistTracks?.Count ?? 0;

        public string PlaylistName => _playlist.Name;


        [ObservableProperty]
        private bool _isNext = false;


        private string _token = string.Empty;
        private DateTime _time = DateTime.Now.AddMinutes(-21);

        [ObservableProperty]
        private ObservableCollection<Track> _playlistTracks = new();
        private SpotifyPlaylist _playlist = new();
        private LibraryVM _libraryVM;

        public SpotifyVM(INavigationService navigation) : base(navigation)
        {
            _libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            _libraryVM.AudioFilesModifified += LibraryVM_AudioFilesModifified;
            BindingOperations.EnableCollectionSynchronization(PlaylistTracks, new object());
        }

        [RelayCommand]
        private void Next() => Navigation.NavigateTo<ExportVM>();

        [RelayCommand]
        private void Load()
        {
            if (IsLoadButtonEnabeld == false || _playlistIDTextOld == PlaylistIDText)
                return;
            IsLoadButtonEnabeld = false;
            IsNext = false;
            _libraryVM.IsNext = false;
            IsInfoVisible = false;

            _playlistIDTextOld = PlaylistIDText;
            if (PlaylistIDText.Length <= 21)
            {
                _libraryVM.IsNext = true;
                IsLoadButtonEnabeld = true;
                return;
            }
            LoadPlaylist();
        }

        public bool TryGetID(out string id)
        {
            id = PlaylistIDText.Trim();
            if (new Regex("(?<=.open\\.spotify\\.com/playlist/)(.*)(?=/?)", RegexOptions.IgnoreCase).Match(id) is Match match && match.Success)
            {
                Debug.WriteLine(match.Groups[0].Value);
                id = match.Groups[1].Value;

            }
            if (id.Length < 21) return false;
            return true;
        }

        private void LoadPlaylist()
        {
            try
            {
                string id = string.Empty;
                if (!TryGetID(out id))
                {
                    MessageBox.Show("Playlist id does not have the right format", "Info", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    _libraryVM.IsNext = true;
                    IsLoadButtonEnabeld = true;
                    return;
                }
                PlaylistTracks.Clear();
                Search(id);
            }
            catch (Exception)
            {
                _libraryVM.IsNext = true;
                IsLoadButtonEnabeld = true;
            }
        }

        private void Search(string id)
        {
            Task.Run(async () =>
            {
                await Task.Yield();
                try
                {
                    if (_token == string.Empty || DateTime.Now.AddMinutes(-20) > _time)
                        await CreateToken();
                    Track[] tracks = await DownloadPlaylist(id);

                    PlaylistTracks = new(tracks);
                    OnPropertyChanged(nameof(PlaylistLength));
                    OnPropertyChanged(nameof(PlaylistName));
                    IsInfoVisible = true;
                    await FindTracksLocal(tracks);
                    _libraryVM.IsNext = true;
                    if (tracks.Any(x => x.IsLocal))
                        IsNext = true;
                    IsLoadButtonEnabeld = true;
                    OnPropertyChanged(nameof(PlaylistFound));
                }
                catch (Exception)
                {
                    MessageBox.Show("Can not fetch token. Please try again later", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _libraryVM.IsNext = true;
                    IsLoadButtonEnabeld = true;
                }
            });
        }

        private async Task FindTracksLocal(Track[] spotifyTracks)
        {
            RequestContainer<OwnRequest> container = new();

            foreach (Track track in spotifyTracks)
            {
                container.Add((OwnRequest)new((t) =>
                {
                    IEnumerable<AudioFile> found = _libraryVM.AudioFiles.Where(audio =>
                    {
                        double title = CalculateSimilarity(audio.CutTitle, track.CutTitle);
                        if (title < 0.5)
                            return false;

                        string audioFirstArtistRemove = audio.CutArtists.FirstOrDefault() ?? "";
                        string trackFirstArtistRemove = track.Artists.FirstOrDefault()?.CutName ?? "";

                        double firstArtist = CalculateSimilarity(audioFirstArtistRemove, trackFirstArtistRemove);
                        if (title + firstArtist > 1.5)
                        {
                            audio.TrackValueDictionary.TryAdd(track, title + firstArtist + 0.8);
                            return true;
                        }

                        string audioSecondArtistRemove = string.Empty;
                        if (audio.CutArtists.Length > 1)
                            audioSecondArtistRemove = audio.CutArtists[1];

                        string trackSecondArtistRemove = string.Empty;
                        if (track.Artists.Length > 1)
                            trackSecondArtistRemove = GetRemovedString(track.Artists[1].CutName);

                        double secondArtist;

                        secondArtist = Math.Max(Math.Max(CalculateSimilarity(audioSecondArtistRemove, trackSecondArtistRemove),
                            CalculateSimilarity(audioSecondArtistRemove, trackFirstArtistRemove)),
                            CalculateSimilarity(audioFirstArtistRemove, trackSecondArtistRemove));


                        if (title + secondArtist > 1.5)
                        {
                            audio.TrackValueDictionary.TryAdd(track, title + secondArtist + 0.8);
                            return true;
                        }
                        double album = CalculateSimilarity(audio.Album?.ToLower(), track.Album?.Name.ToLower());
                        if (title + album + double.Max(firstArtist, secondArtist) > 2.2)
                        {
                            audio.TrackValueDictionary.TryAdd(track, album + title + double.Max(firstArtist, secondArtist));
                            return true;
                        }
                        return false;
                    });
                    if (found.Any())
                    {
                        track.IsLocal = true;
                        track.Path = found.OrderBy(x => x.TrackValueDictionary.TryGetValue(track, out double value) ? value : 0).Last().Location;
                    }
                    return Task.FromResult(true);
                }, new()
                {
                    Handler = _libraryVM.RequestHandler
                }));

            }
            await Task.Delay(100);
            container.Task.Wait();
        }

        private string GetRemovedString(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;
            IEnumerable<int> s = new int[] { title.LastIndexOf("-"), title.LastIndexOf("feat."), title.LastIndexOf("featuring"), title.LastIndexOf("(") }.Where(x => x > 5);
            if (s.Any())
                return title.Remove(s.Min()).ToLower();
            else return title.ToLower();
        }

        private async Task<Track[]> DownloadPlaylist(string id)
        {
            List<Track> tracks = new();

            try
            {
                int limit = 0;
                int offset = 0;
                SpotifyPlaylist spotifyPlaylist = new();
                do
                {
                    limit = limit + 100;
                    HttpRequestMessage req = new(HttpMethod.Get, "https://api.spotify.com/v1/playlists/" + id + (offset > 0 ? $"/tracks?offset={offset}" : ""));
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                    HttpGet getter = new(req);
                    string resp = await (await getter.LoadResponseAsync()).Content.ReadAsStringAsync();

                    if (offset == 0)
                    {
                        spotifyPlaylist = SpotifyPlaylist.FromJson(resp)!;
                        foreach (Item item in spotifyPlaylist.Tracks.Items)
                            tracks.Add(item.Track);
                    }
                    else
                    {
                        foreach (Item item in Tracks.FromJson(resp)!.Items)
                            tracks.Add(item.Track);
                    }

                    offset = limit;

                }
                while (spotifyPlaylist.Tracks.Total > limit);
                _playlist = spotifyPlaylist;
            }

            catch (Exception)
            {
                MessageBox.Show("Playlist not found", "Not found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return tracks.ToArray();
        }
        private async Task CreateToken()
        {
            HttpGet getter = new(new(HttpMethod.Get, "https://open.spotify.com/get_access_token?reason=transport&productType=web_player"));
            _token = await (await getter.LoadResponseAsync()).Content.ReadAsStringAsync();
            JsonElement dynamicObject = JsonSerializer.Deserialize<JsonElement>(_token)!;
            _token = dynamicObject.GetProperty("accessToken").ToString();
            _time = DateTime.Now;
        }

        public static double CalculateSimilarity(string? source, string? target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = LevenshteinDistance(source, target);
            return (1.0 - (stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }
        public static int LevenshteinDistance(string source, string target)
        {
            // degenerate cases
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
        private void LibraryVM_AudioFilesModifified(object? sender, EventArgs e)
        {
            Track[] tracks = PlaylistTracks.ToArray();
            Task.Run(async () =>
           {
               if (tracks.Any())
               {
                   IsLoadButtonEnabeld = false;
                   IsNext = false;
                   await FindTracksLocal(tracks);
                   if (tracks.Any(x => x.IsLocal))
                       IsNext = true;
                   IsLoadButtonEnabeld = true;
                   OnPropertyChanged(nameof(PlaylistFound));
               }
           });
        }
    }

}
