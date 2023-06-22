using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.MVVM.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows.Data;
using System.Xml.Serialization;

namespace SpotifyToM3U.MVVM.Model
{
    internal class IOManager
    {
        private LibraryVM _libraryVM;
        private SpotifyVM _spotifyVM;
        private ExportVM _exportVM;
        private string SaveDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpotifyToM3U\\");
        private string AudioSaveFilePath => Path.Combine(SaveDirectory, "audio_files.xml");
        private string SettingsSaveFilePath => Path.Combine(SaveDirectory, "settings.xml");

        public IOManager()
        {

            _libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            _spotifyVM = App.Current.ServiceProvider.GetRequiredService<SpotifyVM>();
            _exportVM = App.Current.ServiceProvider.GetRequiredService<ExportVM>();
            LoadAudioFilesData();
            _libraryVM.AudioFilesModifified += OnAudioFilesModifified;
        }

        /// <summary>
        /// Removes all invalid Characters for a filename out of a string
        /// </summary>
        /// <param name="name">input filename</param>
        /// <returns>Clreared filename</returns>
        public static string RemoveInvalidFileNameChars(string name)
        {
            StringBuilder fileBuilder = new(name);
            foreach (char c in Path.GetInvalidFileNameChars())
                fileBuilder.Replace(c.ToString(), string.Empty);
            return fileBuilder.ToString();
        }
        /// <summary>
        /// Removes all invalid Characters for a filename out of a string
        /// </summary>
        /// <param name="name">input filename</param>
        /// <returns>Clreared filename</returns>
        public static string RemoveInvalidPathChars(string name)
        {
            StringBuilder fileBuilder = new(name);
            foreach (char c in Path.GetInvalidPathChars())
                fileBuilder.Replace(c.ToString(), string.Empty);
            return fileBuilder.ToString();
        }

        /// <summary>
        /// Returns the absolute path for the specified path string. A return
        /// value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain absolute
        /// path information.
        /// </param>
        /// <param name="result">When this method returns, contains the absolute
        /// path representation of <paramref name="path"/>, if the conversion
        /// succeeded, or <see cref="String.Empty"/> if the conversion failed.
        /// The conversion fails if <paramref name="path"/> is null or
        /// <see cref="String.Empty"/>, or is not of the correct format. This
        /// parameter is passed uninitialized; any value originally supplied
        /// in <paramref name="result"/> will be overwritten.
        /// </param>
        /// <returns><c>true</c> if <paramref name="path"/> was converted
        /// to an absolute path successfully; otherwise, false.
        /// </returns>
        /// <seealso cref="Path.GetFullPath(string)"/>
        /// <seealso cref="IsValidPath"/>
        public static bool TryGetFullPath(string path, out string result)
        {
            result = string.Empty;
            if (string.IsNullOrWhiteSpace(path) || path[1] != ':')
                return false;
            bool status = false;

            try
            {
                result = RemoveInvalidPathChars(path);
                result = Path.GetFullPath(result);
                status = true;
            }
            catch (ArgumentException) { }
            catch (SecurityException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }

            return status;
        }

        private void OnAudioFilesModifified(object? sender, EventArgs e) => SaveAudioFilesData();

        private void LoadAudioFilesData()
        {
            if (!File.Exists(AudioSaveFilePath)) return;
            XmlSerializer serializer = new(typeof(AudioFileCollection));
            using TextReader reader = new StreamReader(AudioSaveFilePath);
            _libraryVM.AudioFiles = (serializer.Deserialize(reader) as AudioFileCollection) ?? new();
            serializer = new(typeof(string[]));

            BindingOperations.EnableCollectionSynchronization(_libraryVM.AudioFiles, new object());
            if (_libraryVM.AudioFiles.Any())
                _libraryVM.IsNext = true;

            if (!File.Exists(SettingsSaveFilePath)) return;
            _libraryVM.RootPathes = new();
            using TextReader dirReader = new StreamReader(SettingsSaveFilePath);
            string[] list = (serializer.Deserialize(dirReader) as string[]) ?? Array.Empty<string>();
            Array.ForEach(list, (x) => _libraryVM.RootPathes.Add(x));
            _exportVM.LibraryVM_AudioFilesModifified(this, null);

        }

        private void SaveAudioFilesData()
        {

            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            XmlSerializer serializer = new(typeof(AudioFileCollection));
            using TextWriter writer = new StreamWriter(AudioSaveFilePath);
            serializer.Serialize(writer, _libraryVM.AudioFiles);
            serializer = new(typeof(string[]));
            using TextWriter dirWriter = new StreamWriter(SettingsSaveFilePath);
            serializer.Serialize(dirWriter, _libraryVM.RootPathes.ToArray());
        }
    }
}
