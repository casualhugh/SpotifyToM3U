using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotifyToM3U.MVVM.Model
{
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
        public string Name { get => _name; set { CutName = IOManager.CutString(value); _name = value; } }
        public string _name = string.Empty;
        public string CutName { get; set; } = string.Empty;
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

        [JsonPropertyName("name")]
        public string Title { get => _title; set { CutTitle = IOManager.CutString(value); _title = value; } }
        public string _title = string.Empty;
        public string CutTitle { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        // Local matching properties
        [JsonIgnore]
        public bool IsLocal { get => _isLocal; set => SetProperty(ref _isLocal, value); }
        private bool _isLocal = false;

        [JsonIgnore]
        public string Path { get => _path; set => SetProperty(ref _path, value); }
        private string _path = string.Empty;

        // Match confidence properties
        [JsonIgnore]
        public double MatchConfidence { get => _matchConfidence; set => SetProperty(ref _matchConfidence, value); }
        private double _matchConfidence = 0.0;

        [JsonIgnore]
        public string MatchType { get => _matchType; set => SetProperty(ref _matchType, value); }
        private string _matchType = string.Empty;

        // Selection properties for export
        [JsonIgnore]
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = true; // Default to selected for export

        // Computed properties
        [JsonIgnore]
        public bool IsIncludedInExport => IsLocal && IsSelected;

        [JsonIgnore]
        public string MatchDescription => GenerateMatchDescription();

        [JsonIgnore]
        public bool HasStrongMatch => MatchConfidence >= 0.85;

        [JsonIgnore]
        public bool HasWeakMatch => IsLocal && MatchConfidence < 0.75;

        [JsonIgnore]
        public bool HasPerfectMatch => MatchConfidence >= 0.95;

        [JsonIgnore]
        public string ConfidenceLevel => MatchConfidence switch
        {
            >= 0.95 => "Perfect",
            >= 0.85 => "Very Good",
            >= 0.75 => "Good",
            >= 0.65 => "Weak",
            _ => "Very Weak"
        };

        [JsonIgnore]
        public string StatusDescription => IsLocal switch
        {
            true when IsSelected => $"✅ Selected ({ConfidenceLevel})",
            true when !IsSelected => $"☐ Excluded ({ConfidenceLevel})",
            false => "❌ Missing"
        };

        private string GenerateMatchDescription()
        {
            if (!IsLocal)
                return "No local match found";

            return MatchConfidence switch
            {
                >= 0.95 => $"Perfect match ({MatchConfidence:P0}) - {MatchType}",
                >= 0.85 => $"Very good match ({MatchConfidence:P0}) - {MatchType}",
                >= 0.75 => $"Good match ({MatchConfidence:P0}) - {MatchType}",
                >= 0.65 => $"Weak match ({MatchConfidence:P0}) - {MatchType}",
                _ => $"Very weak match ({MatchConfidence:P0}) - May be incorrect"
            };
        }
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