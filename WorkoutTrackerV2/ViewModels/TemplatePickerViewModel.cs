using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class TemplatePickerViewModel(IWorkoutService workoutService, ITemplateService templateService) : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<WorkoutTemplate> _templates = [];

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
                var templates = await workoutService.GetAllTemplatesAsync();
                Templates = new ObservableCollection<WorkoutTemplate>(templates);
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
                Templates.Remove(template);
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
