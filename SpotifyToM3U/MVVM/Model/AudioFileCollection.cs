using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SpotifyToM3U.MVVM.Model
{
    public class AudioFileCollection : ObservableCollection<AudioFile>
    {
        private readonly object _lockObj = new();
        public AudioFileCollection()
        {
            files = new HashSet<string>();
        }
        protected override void InsertItem(int index, AudioFile item)
        {
            if (item == null)
                return;
            lock (_lockObj)
            {
                files.Add(item.Location);
                base.InsertItem(index, item);
            }
        }

        public new void Clear()
        {
            base.Clear();
            files.Clear();
        }
        protected override void RemoveItem(int index)
        {
            lock (_lockObj)
            {
                files.Remove(Items[index].Location);
                base.RemoveItem(index);
            }
        }
        public bool ContainsFile(string location)
        {
            return files.Contains(location);
        }

        private readonly HashSet<string> files;
    }

    public class SortedObservableCollection<T> : ObservableCollection<T>
    where T : IComparable
    {
        protected override void InsertItem(int index, T item)
        {
            for (int i = 0; i < Count; i++)
            {
                switch (Math.Sign(this[i].CompareTo(item)))
                {
                    case 0:
                    case 1:
                        base.InsertItem(i, item);
                        return;
                    case -1:
                        break;

                }
            }
            base.InsertItem(Count, item);
        }


    }

}
