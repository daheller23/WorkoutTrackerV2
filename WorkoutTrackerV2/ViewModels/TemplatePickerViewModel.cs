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
        // Holds our folder groups
        [ObservableProperty] private ObservableCollection<TemplateFolderGroup> _groupedTemplates = [];

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void ToggleFolder(TemplateFolderGroup folder)
        {
            folder?.ToggleExpanded();
        }

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

                // Fetch the flat list of templates from the database
                var templates = await workoutService.GetAllTemplatesAsync();

                // Group them by their FolderName safely
                var groupedData = templates
                    .GroupBy(t => string.IsNullOrWhiteSpace(t.FolderName) ? "Uncategorized" : t.FolderName)
                    .Select(g => new TemplateFolderGroup(g.Key, g.ToList()))
                    .OrderBy(g => g.FolderName == "Uncategorized" ? 1 : 0) // Push 'Uncategorized' to the bottom
                    .ThenBy(g => g.FolderName) // Alphabetize the rest
                    .ToList();

                GroupedTemplates = new ObservableCollection<TemplateFolderGroup>(groupedData);
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
        private Task SelectTemplate(WorkoutTemplate template)
        {
            templateService.PendingTemplate = template;
            return Shell.Current.GoToAsync(Routes.Back);
        }

        [RelayCommand]
        private async Task MoveTemplate(WorkoutTemplate template)
        {
            string currentFolder = template.FolderName == "Uncategorized" ? "" : template.FolderName;

            // Ask the user for the new folder name
            string newFolder = await Shell.Current.DisplayPromptAsync(
                "Move Template",
                $"Enter a folder name for '{template.Name}':\n(Leave blank to remove from folders)",
                initialValue: currentFolder,
                accept: "Move",
                cancel: "Cancel");

            // If they pressed Cancel, the result is null. Abort.
            if (newFolder == null)
            {
                return;
            }

            try
            {
                // Update the template's folder property
                template.FolderName = string.IsNullOrWhiteSpace(newFolder) ? "Uncategorized" : newFolder.Trim();

                // Save it to your database
                await workoutService.UpdateTemplateAsync(template);

                // Reload the UI to snap the template into its new folder group
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
                // Delete from database
                await workoutService.DeleteTemplateAsync(template.Id);

                // Reload the templates to rebuild the UI safely without crashing MAUI's group engine
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