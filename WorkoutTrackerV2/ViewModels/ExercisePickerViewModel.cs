using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class ExercisePickerViewModel(
        IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<Exercise> _filteredExercises = [];
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedMuscleGroup = "All";
        [ObservableProperty] private bool _hasSearchText;
        #endregion

        #region "PRIVATE VARIABLES"
        private List<Exercise> _allExercises = [];
        private HashSet<int> _recentExerciseIds = [];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSearchTextChanged(string value)
        {
            HasSearchText = !string.IsNullOrEmpty(value);
            FilterExercises();
        }

        partial void OnSelectedMuscleGroupChanged(string value) => FilterExercises();
        #endregion

        #region "LOAD EXERCISES"
        [RelayCommand]
        private async Task LoadExercises()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                _allExercises = await workoutService.GetAllExercisesAsync();
                _allExercises = [.. _allExercises.OrderBy(e => e.Name)];

                // Load recently used exercise IDs from last 30 days
                var recentSets = await workoutService.GetRecentExerciseIdsAsync(30);
                _recentExerciseIds = recentSets.ToHashSet();

                FilterExercises();
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

        #region "FILTER EXERCISES"
        private void FilterExercises()
        {
            var filtered = _allExercises.AsEnumerable();

            if (SelectedMuscleGroup == "Recent")
                filtered = filtered.Where(e => _recentExerciseIds.Contains(e.Id));
            else if (SelectedMuscleGroup == "Custom")
                filtered = filtered.Where(e => e.IsCustom);
            else if (SelectedMuscleGroup != "All")
                filtered = filtered.Where(e => e.MuscleGroup == SelectedMuscleGroup);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(e =>
                    e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.MuscleGroup.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            // Sort recent exercises to top when showing All
            if (SelectedMuscleGroup == "All" && string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered
                    .OrderByDescending(e => _recentExerciseIds.Contains(e.Id))
                    .ThenBy(e => e.Name);
            }

            var result = filtered.ToList();
            if (result.SequenceEqual(FilteredExercises)) return;
            FilteredExercises = new ObservableCollection<Exercise>(result);
        }
        #endregion

        #region "FILTER BY MUSCLE GROUP"
        [RelayCommand]
        private void FilterByMuscleGroup(string muscleGroup)
        {
            SelectedMuscleGroup = muscleGroup;
        }
        #endregion

        #region "CLEAR SEARCH"
        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }
        #endregion

        #region "SELECT EXERCISE"
        [RelayCommand]
        private static async Task SelectExercise(Exercise exercise)
        {
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                { "SelectedExercise", exercise },
                { "EditSelectedExercise", exercise }
            });
        }
        #endregion

        #region "DELETE EXERCISE"
        [RelayCommand]
        private async Task DeleteExercise(Exercise exercise)
        {
            if (!exercise.IsCustom) return;

            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Exercise",
                $"Are you sure you want to delete '{exercise.Name}'? This cannot be undone.",
                "Yes", "No");

            if (!confirmed) return;

            try
            {
                await workoutService.DeleteExerciseAsync(exercise.Id);
                _allExercises.Remove(exercise);
                FilterExercises();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        #endregion

        #region "CREATE EXERCISE"
        [RelayCommand]
        private static Task CreateExercise() => Shell.Current.GoToAsync(Routes.CreateExercise);
        #endregion

        #region "CANCEL"
        [RelayCommand]
        private static async Task Cancel()
        {
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                { "SelectedExercise", null! },
                { "EditSelectedExercise", null! }
            });
        }
        #endregion
    }
}