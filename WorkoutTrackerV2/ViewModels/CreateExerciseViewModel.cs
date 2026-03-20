using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class CreateExerciseViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty]
        // FIX 2: NotifyPropertyChangedFor replaces the manual UpdateHasValidInput()
        // call — HasValidInput is now recomputed automatically whenever ExerciseName
        // or SelectedMuscleGroup changes, with no extra method needed.
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupColor))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupEmoji))]
        private string _exerciseName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        // FIX 3: PreviewMuscleGroupColor and PreviewMuscleGroupEmoji are computed
        // properties notified here — eliminates MuscleGroupColorConverter,
        // MuscleGroupEmojiConverter, MuscleGroupFilterColorConverter, and
        // MuscleGroupFilterTextColorConverter from the XAML entirely.
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupColor))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupEmoji))]
        [NotifyPropertyChangedFor(nameof(MuscleGroupPills))]
        private string _selectedMuscleGroup = string.Empty;

        [ObservableProperty] private string _nameError = string.Empty;
        [ObservableProperty] private string _muscleGroupError = string.Empty;
        [ObservableProperty] private bool _hasNameError;
        [ObservableProperty] private bool _hasMuscleGroupError;
        #endregion

        #region "COMPUTED PROPERTIES"
        // FIX 2: Computed — no backing field, no UpdateHasValidInput() method.
        public bool HasValidInput =>
            !string.IsNullOrWhiteSpace(ExerciseName) &&
            !string.IsNullOrWhiteSpace(SelectedMuscleGroup);

        // FIX 3: Pre-computed color and emoji for the preview card — replaces
        // MuscleGroupColorConverter and MuscleGroupEmojiConverter bindings.
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

        // FIX 3: Pill VMs with pre-computed IsSelected state — replaces
        // MuscleGroupFilterColorConverter and MuscleGroupFilterTextColorConverter
        // (2 converter calls × 6 pills = 12 calls per tap).
        // Returns a new list each time SelectedMuscleGroup changes (only on tap,
        // not during scroll) — cheap since it's 6 objects.
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
            // FIX 1: Collapse two property assignments into one — HasNameError = false
            // and NameError = string.Empty both notify; clear NameError only when
            // it's set, to avoid an unnecessary notification.
            if (HasNameError)
            {
                HasNameError = false;
                NameError = string.Empty;
            }
            // HasValidInput notified automatically via [NotifyPropertyChangedFor].
        }

        partial void OnSelectedMuscleGroupChanged(string value)
        {
            if (HasMuscleGroupError)
            {
                HasMuscleGroupError = false;
                MuscleGroupError = string.Empty;
            }
            // HasValidInput, PreviewMuscleGroupColor, PreviewMuscleGroupEmoji, and
            // MuscleGroupPills all notified automatically via [NotifyPropertyChangedFor].
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
