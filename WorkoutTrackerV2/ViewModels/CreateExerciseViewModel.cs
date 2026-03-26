using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class CreateExerciseViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupColor))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupEmoji))]
        private string _exerciseName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupColor))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupEmoji))]
        [NotifyPropertyChangedFor(nameof(MuscleGroupPills))]
        private string _selectedMuscleGroup = string.Empty;

        // NEW: Observable properties for the dynamic sub-muscle region
        [ObservableProperty] private ObservableCollection<string> _subMuscleGroups = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        private string _selectedSubMuscleGroup = string.Empty;

        [ObservableProperty] private string _nameError = string.Empty;
        [ObservableProperty] private string _muscleGroupError = string.Empty;
        [ObservableProperty] private bool _hasNameError;
        [ObservableProperty] private bool _hasMuscleGroupError;
        #endregion

        #region "COMPUTED PROPERTIES"
        public bool HasValidInput =>
            !string.IsNullOrWhiteSpace(ExerciseName) &&
            !string.IsNullOrWhiteSpace(SelectedMuscleGroup);

        public string PreviewMuscleGroupColor => SelectedMuscleGroup switch
        {
            "Chest" => "#4A90D9",
            "Back" => "#27AE60",
            "Legs" => "#E67E22",
            "Shoulders" => "#8E44AD",
            "Arms" => "#E74C3C",
            "Core" => "#5DADE2",
            _ => "#999999"
        };

        public string PreviewMuscleGroupEmoji => SelectedMuscleGroup switch
        {
            "Chest" => "🔵",
            "Back" => "🟢",
            "Legs" => "🟠",
            "Shoulders" => "🟣",
            "Arms" => "🔴",
            "Core" => "🩵",
            _ => "⭐"
        };

        public IReadOnlyList<MuscleGroupPillViewModel> MuscleGroupPills =>
        [
            new() { Key = "Chest",     Label = "🔵 Chest",     IsSelected = SelectedMuscleGroup == "Chest" },
            new() { Key = "Back",      Label = "🟢 Back",      IsSelected = SelectedMuscleGroup == "Back" },
            new() { Key = "Legs",      Label = "🟠 Legs",      IsSelected = SelectedMuscleGroup == "Legs" },
            new() { Key = "Shoulders", Label = "🟣 Shoulders", IsSelected = SelectedMuscleGroup == "Shoulders" },
            new() { Key = "Arms",      Label = "🔴 Arms",      IsSelected = SelectedMuscleGroup == "Arms" },
            new() { Key = "Core",      Label = "🩵 Core",      IsSelected = SelectedMuscleGroup == "Core" },
        ];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnExerciseNameChanged(string value)
        {
            if (HasNameError)
            {
                HasNameError = false;
                NameError = string.Empty;
            }
        }

        partial void OnSelectedMuscleGroupChanged(string value)
        {
            if (HasMuscleGroupError)
            {
                HasMuscleGroupError = false;
                MuscleGroupError = string.Empty;
            }

            // NEW: Instantly populate the Sub-Muscle dropdown/pills based on the main selection
            SubMuscleGroups.Clear();
            if (string.IsNullOrEmpty(value)) return;

            var subs = value switch
            {
                "Chest" => new[] { "Upper Chest", "Mid Chest", "Lower Chest" },
                "Back" => new[] { "Lats", "Mid Back", "Traps", "Lower Back" },
                "Legs" => new[] { "Quads", "Hamstrings", "Glutes", "Calves" },
                "Shoulders" => new[] { "Front Delt", "Side Delt", "Rear Delt" },
                "Arms" => new[] { "Biceps", "Triceps", "Forearms" },
                "Core" => new[] { "Abs", "Obliques" },
                _ => new[] { "General" }
            };

            foreach (var sub in subs)
            {
                SubMuscleGroups.Add(sub);
            }

            // Auto-select the first option so the user doesn't submit a blank string
            SelectedSubMuscleGroup = SubMuscleGroups.FirstOrDefault() ?? "General";
        }
        #endregion

        #region "SELECT MUSCLE GROUP"
        [RelayCommand]
        private void SelectMuscleGroup(string muscleGroup)
        {
            SelectedMuscleGroup = muscleGroup;
        }
        #endregion

        #region "SAVE EXERCISE"
        [RelayCommand]
        private async Task SaveExercise()
        {
            HasNameError = false;
            HasMuscleGroupError = false;

            if (string.IsNullOrWhiteSpace(ExerciseName))
            {
                NameError = "Please enter an exercise name.";
                HasNameError = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
            {
                MuscleGroupError = "Please select a muscle group.";
                HasMuscleGroupError = true;
                return;
            }

            try
            {
                IsLoading = true;
                var exercise = new Exercise
                {
                    Name = ExerciseName.Trim(),
                    MuscleGroup = SelectedMuscleGroup,
                    // NEW: Pass the Sub-Region to the database
                    SubMuscleGroup = SelectedSubMuscleGroup,
                    CreatedDate = DateTime.Now,
                    IsCustom = true
                };

                await workoutService.SaveExerciseAsync(exercise);

                await Shell.Current.GoToAsync("..", new Dictionary<string, object>
                {
                    { "SelectedExercise",     exercise },
                    { "EditSelectedExercise", exercise }
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "CANCEL"
        [RelayCommand]
        private static Task Cancel() => Shell.Current.GoToAsync(Routes.Back);
        #endregion
    }
}