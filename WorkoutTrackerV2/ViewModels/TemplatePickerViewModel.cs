using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class TemplatePickerViewModel(IWorkoutService workoutService, ITemplateService templateService) : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<TemplateFolderGroup> _groupedTemplates = [];

        // Search Properties
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _hasSearchText;

        // Cache the raw database list so we can filter instantly in memory
        private List<WorkoutTemplate> _allTemplates = [];

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnSearchTextChanged(string value)
        {
            HasSearchText = !string.IsNullOrWhiteSpace(value);
            ApplyFilter();
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private async Task LoadTemplates()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;

                // Fetch the flat list of templates from the database into our memory cache
                _allTemplates = await workoutService.GetAllTemplatesAsync();

                // Run the grouping and filtering logic
                ApplyFilter();
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

        private void ApplyFilter()
        {
            // 1. Filter the cached templates by Name OR FolderName
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allTemplates
                : _allTemplates.Where(t =>
                    t.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.FolderName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            // 2. Group the filtered results
            var groupedData = filtered
                .GroupBy(t => string.IsNullOrWhiteSpace(t.FolderName) ? "Uncategorized" : t.FolderName)
                .Select(g => new TemplateFolderGroup(g.Key, g.ToList()))
                .OrderBy(g => g.FolderName == "Uncategorized" ? 1 : 0) // Push 'Uncategorized' to the bottom
                .ThenBy(g => g.FolderName) // Alphabetize the rest
                .ToList();

            GroupedTemplates = new ObservableCollection<TemplateFolderGroup>(groupedData);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty; // This automatically triggers OnSearchTextChanged
        }

        [RelayCommand]
        private Task SelectTemplate(WorkoutTemplate template)
        {
            templateService.PendingTemplate = template;
            return Shell.Current.GoToAsync(Routes.Back);
        }

        [RelayCommand]
        private async Task MoveTemplate(WorkoutTemplate template)
        {
            string currentFolder = template.FolderName == "Uncategorized" ? "" : template.FolderName;

            string newFolder = await Shell.Current.DisplayPromptAsync(
                "Move Template",
                $"Enter a folder name for '{template.Name}':\n(Leave blank to remove from folders)",
                initialValue: currentFolder,
                accept: "Move",
                cancel: "Cancel");

            if (newFolder == null)
            {
                return;
            }

            try
            {
                template.FolderName = string.IsNullOrWhiteSpace(newFolder) ? "Uncategorized" : newFolder.Trim();
                await workoutService.UpdateTemplateAsync(template);

                // Reload keeps the current search filter active!
                await LoadTemplates();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteTemplate(WorkoutTemplate template)
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync("Delete Template", $"Are you sure you want to delete '{template.Name}'?", "Yes", "No");

            if (!confirmed)
            {
                return;
            }

            try
            {
                await workoutService.DeleteTemplateAsync(template.Id);
                await LoadTemplates();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private Task Cancel()
        {
            templateService.PendingTemplate = null;
            return Shell.Current.GoToAsync(Routes.Back);
        }
    }
}