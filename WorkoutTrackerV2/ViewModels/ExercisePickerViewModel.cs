using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class MuscleGroupPillViewModel : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string? SubLabel { get; init; }
        public bool HasSubLabel => !string.IsNullOrEmpty(SubLabel);

        [ObservableProperty]
        private bool _isSelected;
    }

    public partial class ExercisePickerViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        private Dictionary<int, Exercise> _exerciseMap = [];
        private HashSet<int> _recentExerciseIds = [];
        private CancellationTokenSource? _filterCts;
        private bool _suppressFilter;

        public List<MuscleGroupPillViewModel> FilterPills { get; } =
        [
            new() { Key = "All",       Label = "All" },
            new() { Key = "Recent",    Label = "🕐 Recent", SubLabel = "last 30 days" },
            new() { Key = "Custom",    Label = "⭐ Custom" },
            new() { Key = "Chest",     Label = "🔵 Chest" },
            new() { Key = "Back",      Label = "🟢 Back" },
            new() { Key = "Legs",      Label = "🟠 Legs" },
            new() { Key = "Shoulders", Label = "🟣 Shoulders" },
            new() { Key = "Arms",      Label = "🔴 Arms" },
            new() { Key = "Core",      Label = "🩵 Core" },
        ];

        [ObservableProperty] private bool _hasSearchText;
        [ObservableProperty] private bool _isGrouped;

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedMuscleGroup = "All";
        [ObservableProperty] private string _exerciseCountLabel = string.Empty;

        [ObservableProperty] private IEnumerable _exerciseSource = Enumerable.Empty<Exercise>();

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnSearchTextChanged(string value)
        {
            HasSearchText = !string.IsNullOrEmpty(value);
            if (!_suppressFilter) ScheduleFilter(debounceMs: 250);
        }

        partial void OnSelectedMuscleGroupChanged(string value)
        {
            foreach (var pill in FilterPills)
                pill.IsSelected = pill.Key == value;

            if (!_suppressFilter) ScheduleFilter(debounceMs: 0);
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private async Task LoadExercises()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                FilterPills[0].IsSelected = true;

                var exercisesTask = workoutService.GetAllExercisesAsync();
                var recentIdsTask = workoutService.GetRecentExerciseIdsAsync(30);
                await Task.WhenAll(exercisesTask, recentIdsTask);

                _exerciseMap = exercisesTask.Result.ToDictionary(e => e.Id);
                _recentExerciseIds = recentIdsTask.Result.ToHashSet();

                ScheduleFilter(debounceMs: 0);
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
        private void FilterByMuscleGroup(string muscleGroup)
        {
            SelectedMuscleGroup = muscleGroup;
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void ResetFilter()
        {
            _suppressFilter = true;
            SelectedMuscleGroup = "All";
            SearchText = string.Empty;
            _suppressFilter = false;
            ScheduleFilter(debounceMs: 0);
        }

        [RelayCommand]
        private static async Task SelectExercise(Exercise exercise)
        {
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                { "SelectedExercise", exercise },
                { "EditSelectedExercise", exercise }
            });
        }

        [RelayCommand]
        private async Task DeleteExercise(Exercise exercise)
        {
            try
            {
                var history = await workoutService.GetExerciseHistoryAsync(exercise.Id, 0);
                if (history.Count > 0)
                {
                    bool proceed = await Shell.Current.DisplayAlertAsync(
                        "Exercise In Use",
                        $"'{exercise.Name}' has been used in {history.Count} sets across your workout history. " +
                        "Deleting it will not remove those sets but they will lose their exercise reference. Continue?",
                        "Delete Anyway", "Cancel");
                    if (!proceed) return;
                }
                else
                {
                    bool confirmed = await Shell.Current.DisplayAlertAsync(
                        "Delete Exercise",
                        $"Are you sure you want to delete '{exercise.Name}'? This cannot be undone.",
                        "Yes", "No");
                    if (!confirmed) return;
                }

                await workoutService.DeleteExerciseAsync(exercise.Id);

                // FIX 3: O(1) Dictionary removal instead of O(n) List.Remove scan.
                _exerciseMap.Remove(exercise.Id);
                ScheduleFilter(debounceMs: 0);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private static Task CreateExercise() => Shell.Current.GoToAsync(Routes.CreateExercise);

        [RelayCommand]
        private static async Task Cancel()
        {
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                { "SelectedExercise", null! },
                { "EditSelectedExercise", null! }
            });
        }

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private void ScheduleFilter(int debounceMs)
        {
            _filterCts?.Cancel();
            _filterCts?.Dispose();
            _filterCts = new CancellationTokenSource();

            var token = _filterCts.Token;

            var searchText = SearchText;
            var muscleGroup = SelectedMuscleGroup;

            Task.Run(async () =>
            {
                try
                {
                    if (debounceMs > 0)
                    {
                        await Task.Delay(debounceMs, token);
                    }
                        
                    token.ThrowIfCancellationRequested();

                    var exercises = _exerciseMap.Values.ToList();
                    var recentIds = _recentExerciseIds.ToHashSet();

                    var filtered = exercises.AsEnumerable();

                    if (muscleGroup == "Recent")
                    {
                        filtered = filtered.Where(e => recentIds.Contains(e.Id));
                    }
                    else if (muscleGroup == "Custom")
                    {
                        filtered = filtered.Where(e => e.IsCustom);
                    }
                    else if (muscleGroup != "All")
                    {
                        filtered = filtered.Where(e => e.MuscleGroup == muscleGroup);
                    }
                        

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        filtered = filtered.Where(e =>
                            e.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            e.MuscleGroup.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                    }

                    bool showGrouped = muscleGroup == "All" && string.IsNullOrWhiteSpace(searchText);

                    List<Exercise> resultList;
                    List<AlphaExerciseGroup>? groupedResults = null;

                    if (showGrouped)
                    {
                        resultList = filtered
                            .OrderByDescending(e => recentIds.Contains(e.Id))
                            .ThenBy(e => e.Name)
                            .ToList();

                        groupedResults = resultList
                            .GroupBy(e => recentIds.Contains(e.Id)
                                ? "🕐 Recent"
                                : (char.IsDigit(e.Name[0]) ? "#" : e.Name[0].ToString().ToUpper()))
                            .OrderBy(g => g.Key == "🕐 Recent" ? "!" : (g.Key == "#" ? "Ω" : g.Key))
                            .Select(g => new AlphaExerciseGroup(g.Key, g.ToList()))
                            .ToList();
                    }
                    else
                    {
                        resultList = filtered.OrderBy(e => e.Name).ToList();
                    }

                    token.ThrowIfCancellationRequested();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        IsGrouped = showGrouped;

                        ExerciseSource = showGrouped ? groupedResults : resultList;

                        var count = resultList.Count;
                        ExerciseCountLabel = count == 1 ? "· 1 exercise" : $"· {count} exercises";
                    });
                }
                catch (OperationCanceledException)
                {
                    // Expected when a newer search cancels this one.
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Filtering Error: {ex.Message}");
                }
            }, token);
        }

    }
}
