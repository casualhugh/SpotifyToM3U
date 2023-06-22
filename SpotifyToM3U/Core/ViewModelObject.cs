using CommunityToolkit.Mvvm.ComponentModel;

namespace SpotifyToM3U.Core
{
    public partial class ViewModelObject : ObservableObject
    {
        private INavigationService _navigation;

        public INavigationService Navigation => _navigation;

        public ViewModelObject(INavigationService navigation) => _navigation = navigation;
    }
}
