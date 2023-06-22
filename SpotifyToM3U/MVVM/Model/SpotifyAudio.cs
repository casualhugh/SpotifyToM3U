namespace SpotifyToM3U.MVVM.Model
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public partial class SpotifyPlaylist : ObservableObject
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("href")]
        public Uri? Href { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public Image[] Images { get; set; } = Array.Empty<Image>();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; } = new();

        [JsonPropertyName("tracks")]
        public Tracks Tracks { get; set; } = new();

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;
    }

    public partial class Image : ObservableObject
    {
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("url")]
        public Uri? Url { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

    public partial class Owner : ObservableObject
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("href")]
        public Uri? Href { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }



    public partial class Tracks : ObservableObject
    {
        [JsonPropertyName("href")]
        public Uri? Href { get; set; }

        [JsonPropertyName("items")]
        public Item[] Items { get; set; } = Array.Empty<Item>();

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public partial class Item : ObservableObject
    {
        [JsonPropertyName("track")]
        public Track Track { get; set; } = new();
    }

    public partial class Track : ObservableObject
    {
        [JsonPropertyName("album")]
        public Album Album { get; set; } = new();

        [JsonIgnore]
        public string? ImageURL => Album.Images.FirstOrDefault()?.Url?.AbsoluteUri;

        [JsonIgnore]
        public bool ShowImage => ImageURL != null;

        [JsonPropertyName("artists")]
        public Owner[] Artists { get; set; } = Array.Empty<Owner>();

        [JsonPropertyName("href")]
        public Uri? Href { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        public bool IsLocal { get => _isLocal; set => SetProperty(ref _isLocal, value); }
        private bool _isLocal = false;
        public string Path { get => _path; set => SetProperty(ref _path, value); }
        private string _path = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;
    }

    public partial class Album : ObservableObject
    {
        [JsonPropertyName("artists")]
        public Owner[] Artists { get; set; } = Array.Empty<Owner>();

        [JsonPropertyName("href")]
        public Uri? Href { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public Image[] Images { get; set; } = Array.Empty<Image>();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;
    }

    public partial class SpotifyPlaylist
    {
        public static SpotifyPlaylist? FromJson(string json) => JsonSerializer.Deserialize<SpotifyPlaylist>(json);
    }

    public partial class Tracks
    {
        public static Tracks? FromJson(string json) => JsonSerializer.Deserialize<Tracks>(json);
    }

}
