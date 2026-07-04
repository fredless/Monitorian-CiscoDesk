using System;
using System.Threading.Tasks;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Properties;

namespace Monitorian.Core.ViewModels;

public class CiscoDeskSectionViewModel : ViewModelBase
{
	private readonly AppControllerCore _controller;
	public SettingsCore Settings => _controller.Settings;

	public CiscoDeskSectionViewModel(AppControllerCore controller)
	{
		this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
	}

	/// <summary>
	/// Password to access Cisco RoomOS Desk device
	/// </summary>
	/// <remarks>
	/// This property is accessed from code-behind because PasswordBox does not support binding.
	/// </remarks>
	public string Password
	{
		get => Settings.CiscoDeskPassword;
		set => Settings.CiscoDeskPassword = value;
	}

	public bool CanTest
	{
		get => _canTest;
		private set => SetProperty(ref _canTest, value);
	}
	private bool _canTest = true;

	public string TestResult
	{
		get => _testResult;
		private set => SetProperty(ref _testResult, value);
	}
	private string _testResult;

	public void PerformTest()
	{
		if (string.IsNullOrWhiteSpace(Settings.CiscoDeskHost))
		{
			TestResult = Invariant.CiscoDeskTestFailed;
			return;
		}

		CanTest = false;
		TestResult = null;

		Task.Run(async () =>
		{
			try
			{
				using var client = new CiscoDeskClient(
					host: Settings.CiscoDeskHost,
					username: Settings.CiscoDeskUsername,
					password: Settings.CiscoDeskPassword,
					usesHttps: Settings.CiscoDeskUsesHttps,
					validatesCertificate: Settings.CiscoDeskValidatesCertificate);

				var (success, brightness, message) = await client.GetBrightnessAsync();

				TestResult = success
					? $"{Invariant.CiscoDeskTestSucceeded} ({brightness}%)"
					: $"{Invariant.CiscoDeskTestFailed}: {message}";
			}
			finally
			{
				CanTest = true;
			}
		});
	}
}
