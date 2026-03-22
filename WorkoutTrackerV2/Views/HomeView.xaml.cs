using WorkoutTrackerV2.Controls;
using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class HomeView : ContentPage
{
    private ConfettiView? _confetti;

    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _confetti = this.FindByName<ConfettiView>("ConfettiCanvas");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not HomeViewModel vm) return;

        // Read the static PR message synchronously — set by AddWorkoutViewModel
        // before GoToAsync so it is always populated when OnAppearing fires.
        // No QueryProperty, no PropertyChanged, no timing race.
        var pr = HomeViewModel.PendingPrMessage;
        HomeViewModel.PendingPrMessage = string.Empty; // consume immediately

        _ = vm.LoadDataCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(pr))
            _ = ShowPrBannerAsync(pr);
    }

    private async Task ShowPrBannerAsync(string message)
    {
        // Set the label text directly — no binding needed
        PrMessageLabel.Text = message;

        // Show overlay and reset banner position
        PrOverlay.IsVisible = true;
        PrBanner.IsVisible = true;
        PrBanner.Opacity = 0;
        PrBanner.TranslationY = -160;

        // Start confetti
        _confetti?.Start();

        // Spring slide down + fade in
        await Task.WhenAll(
            PrBanner.TranslateToAsync(0, 0, 320, Easing.SpringOut),
            PrBanner.FadeToAsync(1, 200, Easing.Linear)
        );

        // Hold for 2 seconds
        await Task.Delay(2000);

        // Fade out
        await PrBanner.FadeToAsync(0, 400, Easing.Linear);

        // Hide everything
        PrBanner.IsVisible = false;
        PrOverlay.IsVisible = false;
    }
}
