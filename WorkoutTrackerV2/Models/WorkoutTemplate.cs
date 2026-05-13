using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutTemplate
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string FolderName { get; set; } = "Uncategorized";
    }

    public class TemplateFolderGroup : ObservableCollection<WorkoutTemplate>, INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private readonly List<WorkoutTemplate> _hiddenItems = [];

        public string FolderName { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public TemplateFolderGroup(string name, IEnumerable<WorkoutTemplate> templates) : base(templates)
        {
            FolderName = name;
        }

        public void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;

            if (IsExpanded)
            {
                foreach (var item in _hiddenItems)
                {
                    this.Add(item);
                }
                _hiddenItems.Clear();
            }
            else
            {
                _hiddenItems.AddRange(this);
                this.Clear();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}