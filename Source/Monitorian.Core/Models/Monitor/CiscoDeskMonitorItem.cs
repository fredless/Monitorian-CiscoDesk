using System;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Cisco RoomOS Desk device accessed by xAPI over the network
/// </summary>
internal class CiscoDeskMonitorItem : MonitorItem
{
	private readonly CiscoDeskClient _client;

	public CiscoDeskMonitorItem(
		string deviceInstanceId,
		string description,
		CiscoDeskClient client) : base(
			deviceInstanceId: deviceInstanceId,
			description: description,
			displayIndex: byte.MaxValue,
			monitorIndex: byte.MaxValue,
			monitorRect: new Rect(short.MaxValue, short.MaxValue, 0, 0), // Provisional rect to be sorted last
			connection: ConnectionType.Unknown,
			isInternal: false,
			isReachable: true)
	{
		this._client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public override AccessResult UpdateBrightness(int brightness = -1)
	{
		// This method is expected to be called on a thread pool thread.
		var (success, value, message) = _client.GetBrightnessAsync().GetAwaiter().GetResult();
		if (success)
		{
			this.Brightness = value;
			return AccessResult.Succeeded;
		}
		return new AccessResult(AccessStatus.Failed, $"Cisco Desk ({_client.Host}): {message}");
	}

	public override AccessResult SetBrightness(int brightness)
	{
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be from 0 to 100.");

		this.Brightness = brightness;

		bool starts;
		lock (_sendLock)
		{
			_pendingBrightness = brightness;
			starts = !_isSending;
			_isSending = true;
		}
		if (starts)
			Task.Run(SendPendingAsync);

		return !_hasSendFailed
			? AccessResult.Succeeded
			: new AccessResult(AccessStatus.Failed, $"Cisco Desk ({_client.Host}): {_lastSendMessage}");
	}

	// Sending brightness is performed asynchronously (only the latest pending value is sent)
	// so as not to block the calling thread by network access while the slider is moved.
	// A failure of sending will be reflected in the result of a subsequent call.
	private readonly object _sendLock = new();
	private int _pendingBrightness = -1;
	private bool _isSending;
	private volatile bool _hasSendFailed;
	private string _lastSendMessage;

	private async Task SendPendingAsync()
	{
		while (true)
		{
			int value;
			lock (_sendLock)
			{
				if (_pendingBrightness < 0)
				{
					_isSending = false;
					return;
				}
				value = _pendingBrightness;
				_pendingBrightness = -1;
			}

			var (success, message) = await _client.SetBrightnessAsync(value).ConfigureAwait(false);
			_lastSendMessage = message;
			_hasSendFailed = !success;
		}
	}

	#region IDisposable

	private bool _isDisposed = false;

	protected override void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			_client.Dispose();
		}

		_isDisposed = true;

		base.Dispose(disposing);
	}

	#endregion
}
