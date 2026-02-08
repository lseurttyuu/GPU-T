using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GPU_T.ViewModels;
using System.Collections.Generic;

namespace GPU_T.Views;

/// <summary>
/// Hosts sensor-related UI elements and mediates user interactions for graph hovering and log file selection.
/// </summary>
/// <remarks>
/// Graph pointer events delegate hover coordinates to the corresponding SensorItemViewModel so that history display
/// is handled at the view-model layer. File logging is initiated via the platform StorageProvider obtained from the top-level window.
/// </remarks>
public partial class SensorsView : UserControl
{
	/// <summary>
	/// Initializes a new instance of <see cref="SensorsView"/>.
	/// </summary>
	public SensorsView()
	{
		InitializeComponent();
	}

	private void Graph_PointerMoved(object? sender, PointerEventArgs e)
	{
		// Ensure the sender is a visual control whose DataContext is a SensorItemViewModel before forwarding pointer coordinates.
		if (sender is Control control && control.DataContext is SensorItemViewModel vm)
		{
			var point = e.GetPosition(control);
			// Provide X coordinate and control width to the view model so it can map pointer position to historical data indices.
			vm.ShowHistoryAt(point.X, control.Bounds.Width);
		}
	}

	private void Graph_PointerExited(object? sender, PointerEventArgs e)
	{
		// Ensure DataContext is sensor VM before instructing it to stop hover behavior.
		if (sender is Control control && control.DataContext is SensorItemViewModel vm)
		{
			vm.StopHovering();
		}
	}

	private async void LogCheckBox_Checked(object? sender, RoutedEventArgs e)
	{
		var checkbox = sender as CheckBox;
		
		// The UserControl's DataContext is inherited from the window, which provides logging control via MainWindowViewModel.
		if (DataContext is MainWindowViewModel vm)
		{
			// If logging is not yet active, prompt the user to select a destination file.
			if (!vm.IsLogEnabled)
			{
				// Acquire top-level to obtain the platform StorageProvider; this can be null in detached visual trees.
				var topLevel = TopLevel.GetTopLevel(this);
				if (topLevel == null) return;

				// Prompt the user to pick a file for saving logs using the platform's SaveFilePicker.
				var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
				{
					Title = "Save Sensor Log",
					SuggestedFileName = "GPU-T Sensor Log.txt",
					DefaultExtension = "txt",
					FileTypeChoices = new List<FilePickerFileType>
					{
						new("Text Files") { Patterns = new[] { "*.txt" } },
						new("All Files") { Patterns = new[] { "*.*" } }
					}
				});

				if (file != null)
				{
					// Convert the selected storage item to a local path for the ViewModel to use; this is platform-dependent.
					string path = file.Path.LocalPath;
					vm.StartLogging(path);
				}
				else
				{
					// User cancelled the dialog; revert the checkbox UI state to reflect no active logging.
					if (checkbox != null) checkbox.IsChecked = false;
				}
			}
		}
	}

	private void LogCheckBox_Unchecked(object? sender, RoutedEventArgs e)
	{
		// Instruct the ViewModel to stop logging when the checkbox is cleared.
		if (DataContext is MainWindowViewModel vm)
		{
			vm.StopLogging();
		}
	}
}