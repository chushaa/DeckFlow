namespace DeckFlow.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Core.Planning.Contracts;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class StepViewerViewModel : ObservableObject, IQueryAttributable
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CurrentStep))]
	[NotifyPropertyChangedFor(nameof(StepProgress))]
	[NotifyPropertyChangedFor(nameof(StepProgressText))]
	[NotifyPropertyChangedFor(nameof(IsFirstStep))]
	[NotifyPropertyChangedFor(nameof(IsLastStep))]
	private int _currentStepIndex;

	[ObservableProperty]
	private bool _isNavigating;

	[ObservableProperty]
	private string _navigatingText = string.Empty;

	public MovementPlan? Plan { get; private set; }

	public PlanStep? CurrentStep =>
		Plan is not null && CurrentStepIndex >= 0 && CurrentStepIndex < Plan.Steps.Count
			? Plan.Steps[CurrentStepIndex]
			: null;

	public double StepProgress =>
		Plan is not null && Plan.Steps.Count > 1
			? (double)(CurrentStepIndex + 1) / Plan.Steps.Count
			: 1.0;

	public string StepProgressText =>
		Plan is not null
			? $"Step {CurrentStepIndex + 1} of {Plan.Steps.Count}"
			: string.Empty;

	public bool IsFirstStep => CurrentStepIndex <= 0;

	public bool IsLastStep =>
		Plan is null || CurrentStepIndex >= Plan.Steps.Count - 1;

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Plan", out var planObj) && planObj is MovementPlan plan)
		{
			Plan = plan;
			CurrentStepIndex = 0;
			OnPropertyChanged(nameof(Plan));
			OnPropertyChanged(nameof(CurrentStep));
			OnPropertyChanged(nameof(StepProgress));
			OnPropertyChanged(nameof(StepProgressText));
			OnPropertyChanged(nameof(IsFirstStep));
			OnPropertyChanged(nameof(IsLastStep));
		}
	}

	[RelayCommand]
	private async Task NextStepAsync()
	{
		if (IsLastStep)
			return;

		NavigatingText = GetStepDescription(CurrentStepIndex + 1);
		IsNavigating = true;
		await Task.Delay(1);
		CurrentStepIndex++;
		IsNavigating = false;
	}

	[RelayCommand]
	private async Task PreviousStepAsync()
	{
		if (IsFirstStep)
			return;

		NavigatingText = GetStepDescription(CurrentStepIndex - 1);
		IsNavigating = true;
		await Task.Delay(1);
		CurrentStepIndex--;
		IsNavigating = false;
	}

	private string GetStepDescription(int targetIndex)
	{
		if (Plan is null || targetIndex < 0 || targetIndex >= Plan.Steps.Count)
			return string.Empty;

		return Plan.Steps[targetIndex] switch
		{
			PreGameStep preGame => $"Preparing {preGame.TargetDeckName}",
			TransitionStep transition => $"{transition.FromDeckName} → {transition.ToDeckName}",
			FinalReturnStep => "Creating Final Return",
			_ => string.Empty
		};
	}

	[RelayCommand]
	private async Task DoneAsync()
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}