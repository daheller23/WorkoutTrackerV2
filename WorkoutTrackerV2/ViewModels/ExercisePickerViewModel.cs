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
        #endregion

        #region "PRIVATE VARIABLES"
        private List<Exercise> _allExercises = [];
        #endregion

        #region "ON SEARCH TEXT CHANGED"
        partial void OnSearchTextChanged(string value) => FilterExercises();
        #endregion

        #region "LOAD EXERCISES"
        [RelayCommand]
        private async Task LoadExercises()
        {
            if (IsLoading)
            {
                return;
            }
                
            // Only reload if list is empty
            if (_allExercises.Count > 0)
            {
                FilterExercises();
                return;
            }

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
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allExercises.Take(50).ToList()
                : _allExercises
                    .Where(e =>
                        e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        e.MuscleGroup.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .Take(50)
                    .ToList();

            // Only update if results actually changed
            if (filtered.SequenceEqual(FilteredExercises))
            {
                return;
            }

            FilteredExercises = new ObservableCollection<Exercise>(filtered);
        }
        #endregion

        #region "SELECT EXERCISE"
        [RelayCommand]
        private static async Task SelectExercise(Exercise exercise)
        {
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                { "SelectedExercise", exercise }
            });
        }
        #endregion

        #region "CANCEL"
        [RelayCommand]
        private static async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
        #endregion
    }
}