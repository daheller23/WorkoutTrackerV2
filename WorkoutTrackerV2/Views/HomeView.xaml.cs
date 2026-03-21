using WorkoutTrackerV2.Controls;
using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class HomeView : ContentPage
{
    private ConfettiView? _confettiView;

    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        _confettiView = this.FindByName<ConfettiView>("ConfettiCanvas");

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(HomeViewModel.IsPrVisible)) return;
            if (vm.IsPrVisible)
                _ = AnimatePrBannerIn(vm);
            else
                ResetPrBanner();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
            _ = OnAppearingAsync(vm);
    }

    private async Task OnAppearingAsync(HomeViewModel vm)
    {
        // Fire data load and PR check simultaneously — don't wait for data
        // before showing the celebration. The PR banner appears instantly
        // while the home stats load in the background.
        var loadTask = vm.LoadDataCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(vm.PrMessage))
            vm.IsPrVisible = true;

        await loadTask;
    }

    private void ResetPrBanner()
    {
        PrBanner.TranslationY = -120;
        PrBanner.Opacity = 0;
    }

    private async Task AnimatePrBannerIn(HomeViewModel vm)
    {
        PrBanner.TranslationY = -120;
        PrBanner.Opacity = 0;

        _confettiView?.Start();

        await Task.WhenAll(
            PrBanner.TranslateToAsync(0, 0, 320, Easing.SpringOut),
            PrBanner.FadeToAsync(1, 200, Easing.Linear)
        );

        await Task.Delay(5000);
        await PrBanner.FadeToAsync(0, 400, Easing.Linear);

        // Reset VM state and clear the message so it doesn't re-trigger on
        // the next OnAppearing.
        vm.IsPrVisible = false;
        vm.PrMessage = string.Empty;
    }
}
