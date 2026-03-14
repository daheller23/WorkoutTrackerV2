
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        #region "PRIVATE VARIABLES"
        private bool isLoading;
        private string errorMessage;
        #endregion

        #region "PUBLIC PROPERTIES"
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }
        #endregion

        #region "SET PROPERTY"
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region "ON PROPERTY CHANGED"
        public void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}
