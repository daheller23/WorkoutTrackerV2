using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class CreateExerciseViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupColor))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupEmoji))]
        private string _exerciseName = string.Empty;

        [ObservableProperty] private string _muscleGroupError = string.Empty;
        [ObservableProperty] private string _nameError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupColor))]
        [NotifyPropertyChangedFor(nameof(PreviewMuscleGroupEmoji))]
        [NotifyPropertyChangedFor(nameof(MuscleGroupPills))]
        private string _selectedMuscleGroup = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidInput))]
        private string _selectedSubMuscleGroup = string.Empty;

        [ObservableProperty] private bool _hasMuscleGroupError;
        [ObservableProperty] private bool _hasNameError;

        [ObservableProperty] private ObservableCollection<string> _subMuscleGroups = [];

        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================

        public bool HasValidInput =>
            !string.IsNullOrWhiteSpace(ExerciseName) &&
            !string.IsNullOrWhiteSpace(SelectedMuscleGroup);

        public string PreviewMuscleGroupColor => ColorHelper.GetMuscleGroupColor(SelectedMuscleGroup);

        public string PreviewMuscleGroupEmoji => SelectedMuscleGroup switch
        {
            "Chest" => "🔵",
            "Back" => "🟢",
            "Legs" => "🟠",
            "Shoulders" => "🟣",
            "Biceps" => "🔴",
            "Triceps" => "🔴",
            "Forearms" => "🟡",
            "Core" => "⚪",
            _ => "⭐"
        };

        public IReadOnlyList<MuscleGroupPillViewModel> MuscleGroupPills =>
        [
            new() { Key = "Chest",     Label = "🔵 Chest",     IsSelected = SelectedMuscleGroup == "Chest" },
            new() { Key = "Back",      Label = "🟢 Back",      IsSelected = SelectedMuscleGroup == "Back" },
            new() { Key = "Legs",      Label = "🟠 Legs",      IsSelected = SelectedMuscleGroup == "Legs" },
            new() { Key = "Shoulders", Label = "🟣 Shoulders", IsSelected = SelectedMuscleGroup == "Shoulders" },
            new() { Key = "Biceps",    Label = "🔴 Biceps",    IsSelected = SelectedMuscleGroup == "Biceps" },
            new() { Key = "Triceps",   Label = "🔴 Triceps",   IsSelected = SelectedMuscleGroup == "Triceps" },
            new() { Key = "Forearms",  Label = "🟡 Forearms",  IsSelected = SelectedMuscleGroup == "Forearms" },
            new() { Key = "Core",      Label = "⚪ Core",      IsSelected = SelectedMuscleGroup == "Core" },
        ];

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

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

            SubMuscleGroups.Clear();
            if (string.IsNullOrEmpty(value)) return;

            var subs = value switch
            {
                "Chest" => new[] { "Upper Chest", "Mid Chest", "Lower Chest" },
                "Back" => new[] { "Lats", "Mid Back", "Traps", "Lower Back" },
                "Legs" => new[] { "Quads", "Hamstrings", "Glutes", "Calves" },
                "Shoulders" => new[] { "Front Delt", "Side Delt", "Rear Delt" },
                "Biceps" => new[] { "Long Head", "Short Head", "Brachialis" },
                "Triceps" => new[] { "Long Head", "Lateral Head", "Medial Head" },
                "Forearms" => new[] { "Flexors", "Extensors" },
                "Core" => new[] { "Abs", "Obliques" },
                _ => new[] { "General" }
            };

            foreach (var sub in subs)
            {
                SubMuscleGroups.Add(sub);
            }

            SelectedSubMuscleGroup = SubMuscleGroups.FirstOrDefault() ?? "General";
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void SelectMuscleGroup(string muscleGroup)
        {
            SelectedMuscleGroup = muscleGroup;
        }

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

        [RelayCommand]
        private static Task Cancel() => Shell.Current.GoToAsync(Routes.Back);
    }
}