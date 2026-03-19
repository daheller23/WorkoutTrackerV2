using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    // ─────────────────────────────────────────────────────────────────────────
    // MuscleGroupPillViewModel
    // Replaces the two-converter-per-pill pattern. The XAML uses a single
    // DataTrigger on IsSelected — zero value converters per pill per tap.
    // ─────────────────────────────────────────────────────────────────────────
    public partial class MuscleGroupPillViewModel : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string? SubLabel { get; init; }
        public bool HasSubLabel => !string.IsNullOrEmpty(SubLabel);

        [ObservableProperty]
        private bool _isSelected;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ExercisePickerViewModel
    // ─────────────────────────────────────────────────────────────────────────
    public partial class ExercisePickerViewModel(
        IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"

        // Single source + IsGrouped drives one CollectionView in both flat and
        // grouped modes, eliminating the double-construction cost of the original.
        [ObservableProperty] private IEnumerable _exerciseSource = Enumerable.Empty<Exercise>();
        [ObservableProperty] private bool _isGrouped;

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedMuscleGroup = "All";
        [ObservableProperty] private string _exerciseCountLabel = string.Empty;
        [ObservableProperty] private bool _hasSearchText;

        // Pill ViewModels — constructed once at startup; only IsSelected toggles thereafter.
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

        #endregion

        #region "PRIVATE STATE"

        // FIX 3: Dictionary keyed by Id gives O(1) delete instead of O(n) List.Remove.
        private Dictionary<int, Exercise> _exerciseMap = [];
        private HashSet<int> _recentExerciseIds = [];

        // Single CTS covers both the debounce delay and the background filter work.
        // One Cancel() call aborts both simultaneously.
        private CancellationTokenSource? _filterCts;

        // FIX 4: While true, partial property callbacks skip ScheduleFilter so that
        // ResetFilter() setting two properties only triggers one filter pass.
        private bool _suppressFilter;

        #endregion

        #region "PARTIAL METHODS"

        partial void OnSearchTextChanged(string value)
        {
            HasSearchText = !string.IsNullOrEmpty(value);
            if (!_suppressFilter) ScheduleFilter(debounceMs: 250);
        }

        partial void OnSelectedMuscleGroupChanged(string value)
        {
            // Toggle pill selection — cheap bool flip on 9 items, zero converters.
            foreach (var pill in FilterPills)
                pill.IsSelected = pill.Key == value;

            if (!_suppressFilter) ScheduleFilter(debounceMs: 0);
        }

        #endregion

        #region "LOAD EXERCISES"

        [RelayCommand]
        private async Task LoadExercises()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                FilterPills[0].IsSelected = true;

                // FIX 1: Fire both DB queries concurrently — Task.WhenAll instead of
                // two sequential awaits. Halves load time when queries take similar time.
                var exercisesTask = workoutService.GetAllExercisesAsync();
                var recentIdsTask = workoutService.GetRecentExerciseIdsAsync(30);
                await Task.WhenAll(exercisesTask, recentIdsTask);

                // FIX 2: No upfront OrderBy here — ScheduleFilter always re-sorts the
                // result set anyway (sort order varies by filter), so sorting at load
                // time was redundant work.
                // FIX 3: Store in Dictionary for O(1) lookup and delete.
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

        #endregion

        #region "FILTER — debounce + background execution"

        private void ScheduleFilter(int debounceMs)
        {
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    // Debounce: wait before doing any work so rapid keystrokes collapse
                    // into a single filter pass after the user pauses typing.
                    if (debounceMs > 0)
                        await Task.Delay(debounceMs, token);

                    if (token.IsCancellationRequested) return;

                    // Snapshot all observable state before leaving the UI thread.
                    // Never touch [ObservableProperty] fields from a background thread.
                    var searchText = SearchText;
                    var muscleGroup = SelectedMuscleGroup;
                    var exercises = _exerciseMap.Values;  // Dictionary.Values is read-safe
                    var recentIds = _recentExerciseIds;

                    // ── All LINQ, sorting, and grouping on the thread pool ────────
                    var filtered = exercises.AsEnumerable();

                    if (muscleGroup == "Recent")
                        filtered = filtered.Where(e => recentIds.Contains(e.Id));
                    else if (muscleGroup == "Custom")
                        filtered = filtered.Where(e => e.IsCustom);
                    else if (muscleGroup != "All")
                        filtered = filtered.Where(e => e.MuscleGroup == muscleGroup);

                    if (!string.IsNullOrWhiteSpace(searchText))
                        filtered = filtered.Where(e =>
                            e.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            e.MuscleGroup.Contains(searchText, StringComparison.OrdinalIgnoreCase));

                    bool showGrouped = muscleGroup == "All" && string.IsNullOrWhiteSpace(searchText);

                    List<Exercise> result;
                    List<AlphaExerciseGroup>? groups = null;

                    if (showGrouped)
                    {
                        result = filtered
                            .OrderByDescending(e => recentIds.Contains(e.Id))
                            .ThenBy(e => e.Name)
                            .ToList();

                        groups = result
                            .GroupBy(e => recentIds.Contains(e.Id)
                                ? "🕐 Recent"
                                : e.Name[0].ToString().ToUpper())
                            .OrderBy(g => g.Key == "🕐 Recent" ? "!" : g.Key)
                            .Select(g => new AlphaExerciseGroup(g.Key, g.ToList()))
                            .ToList();
                    }
                    else
                    {
                        result = filtered.OrderBy(e => e.Name).ToList();
                    }

                    if (token.IsCancellationRequested) return;

                    // ── Marshal only UI assignments back to the main thread ───────
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        IsGrouped = showGrouped;

                        // FIX 5: Assign plain List<T>/List<AlphaExerciseGroup> directly
                        // instead of wrapping in a new ObservableCollection<T> on every
                        // filter. CollectionView only needs IEnumerable — change
                        // notifications on the collection itself are unnecessary since
                        // we replace the entire source reference each time.
                        ExerciseSource = showGrouped
                            ? (IEnumerable)groups!
                            : result;

                        var count = result.Count;
                        ExerciseCountLabel = count == 1 ? "· 1 exercise" : $"· {count} exercises";
                    });
                }
                catch (TaskCanceledException)
                {
                    // Expected — a newer filter request cancelled this one. Do nothing.
                }
            }, token);
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

        #region "RESET FILTER"

        [RelayCommand]
        private void ResetFilter()
        {
            // FIX 4: Suppress the two intermediate ScheduleFilter calls that would
            // otherwise fire independently from OnSelectedMuscleGroupChanged and
            // OnSearchTextChanged. Only one filter pass fires — after both are set.
            _suppressFilter = true;
            SelectedMuscleGroup = "All";
            SearchText = string.Empty;
            _suppressFilter = false;
            ScheduleFilter(debounceMs: 0);
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
