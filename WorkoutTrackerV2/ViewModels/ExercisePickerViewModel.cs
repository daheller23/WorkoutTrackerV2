using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class ExercisePickerViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<Exercise> _filteredExercises = [];
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedMuscleGroup = "All";
        [ObservableProperty] private bool _hasSearchText;
        #endregion

        #region "PRIVATE VARIABLES"
        private List<Exercise> _allExercises = [];
        #endregion

        [RelayCommand]
        private static Task CreateExercise() => Shell.Current.GoToAsync(Routes.CreateExercise);

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

            if (SelectedMuscleGroup != "All")
                filtered = filtered.Where(e => e.MuscleGroup == SelectedMuscleGroup);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(e =>
                    e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.MuscleGroup.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            var result = filtered.Take(50).ToList();

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