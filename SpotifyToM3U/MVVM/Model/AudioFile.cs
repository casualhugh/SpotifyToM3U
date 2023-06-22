using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SpotifyToM3U.MVVM.Model
{
    public partial class AudioFile : ObservableObject
    {
        public AudioFile() { }

        public AudioFile(string location)
        {
            Location = location;
            Tags = new List<int>();
            try
            {
                TagLib.File file = TagLib.File.Create(Location);
                _title = file.Tag.Title;
                _artists = file.Tag.Performers.Length > 0 ? file.Tag.Performers : new string[] { "" };
                _album = file.Tag.Album;
                _genre = file.Tag.Genres.Length > 0 ? file.Tag.Genres[0] : "";
                _year = file.Tag.Year;
                int index = location.LastIndexOf('.');
                if (index != -1)
                {
                    Extension = location.Substring(index + 1).ToUpper();
                }
            }
            catch (TagLib.UnsupportedFormatException)
            {

            }
            catch (TagLib.CorruptFileException)
            {

            }
        }

        public string Extension { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public List<int> Tags { get; set; } = new();

        [ObservableProperty]
        private string _title = string.Empty;
        [ObservableProperty]
        private string[] _artists = new string[] { "" };
        [ObservableProperty]
        private string _genre = string.Empty;
        [ObservableProperty]
        private uint _year;
        [ObservableProperty]
        private string _album = string.Empty;

        [XmlIgnore]
        public ConcurrentDictionary<Track, double> TrackValueDictionary { get; } = new();

    }
}
