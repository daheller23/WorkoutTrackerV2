using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class TemplatePickerView : ContentPage
    {
        public TemplatePickerView(TemplatePickerViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is TemplatePickerViewModel vm)
                vm.LoadTemplatesCommand.Execute(null);
        }
    }
}