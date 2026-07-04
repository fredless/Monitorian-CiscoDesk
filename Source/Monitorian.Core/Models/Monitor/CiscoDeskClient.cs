using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Client to access Cisco RoomOS Desk device by xAPI over HTTP/HTTPS
/// </summary>
internal class CiscoDeskClient : IDisposable
{
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;

	public string Host { get; }

	public CiscoDeskClient(string host, string username, string password, bool usesHttps, bool validatesCertificate)
	{
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));

		this.Host = host.Trim();
		_baseUrl = $"{(usesHttps ? "https" : "http")}://{this.Host}";

		var handler = new HttpClientHandler();

		// Cisco devices commonly use a self-signed certificate.
		if (usesHttps && !validatesCertificate)
		{
			handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
		}

		_httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

		var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
	}

	/// <summary>
	/// Gets the current backlight brightness of the device.
	/// </summary>
	/// <returns>result of accessing device and brightness (0 to 100) if succeeded</returns>
	/// <remarks>xStatus Video Output Monitor[n] Backlight</remarks>
	public async Task<(bool success, int brightness, string message)> GetBrightnessAsync()
	{
		try
		{
			using var response = await _httpClient.GetAsync($"{_baseUrl}/getxml?location=/Status/Video/Output/Monitor").ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
				return (false, -1, $"HTTP {(int)response.StatusCode} {response.StatusCode}");

			var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			var match = Regex.Match(body, @"<Backlight[^>]*>(\d+)</Backlight>", RegexOptions.IgnoreCase);
			if (match.Success && int.TryParse(match.Groups[1].Value, out int brightness))
				return (true, brightness, null);

			return (false, -1, "Failed to find backlight value in response.");
		}
		catch (Exception ex)
		{
			return (false, -1, GetMessage(ex));
		}
	}

	/// <summary>
	/// Sets the backlight brightness of the device.
	/// </summary>
	/// <param name="brightness">Brightness (0 to 100)</param>
	/// <returns>result of accessing device</returns>
	/// <remarks>xCommand Video Output Monitor Backlight Set Value: n</remarks>
	public async Task<(bool success, string message)> SetBrightnessAsync(int brightness)
	{
		brightness = Math.Min(100, Math.Max(0, brightness));

		var xml = $@"<?xml version=""1.0""?>
<Command>
  <Video>
    <Output>
      <Monitor>
        <Backlight>
          <Set>
            <Value>{brightness}</Value>
          </Set>
        </Backlight>
      </Monitor>
    </Output>
  </Video>
</Command>";

		try
		{
			using var content = new StringContent(xml, Encoding.UTF8, "text/xml");
			using var response = await _httpClient.PostAsync($"{_baseUrl}/putxml", content).ConfigureAwait(false);

			return response.IsSuccessStatusCode
				? (true, null)
				: (false, $"HTTP {(int)response.StatusCode} {response.StatusCode}");
		}
		catch (Exception ex)
		{
			return (false, GetMessage(ex));
		}
	}

	private static string GetMessage(Exception ex) => ex switch
	{
		TaskCanceledException => "Timed out.",
		HttpRequestException { InnerException: not null } hre => hre.InnerException.Message,
		_ => ex.Message
	};

	#region IDisposable

	private bool _isDisposed = false;

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_httpClient.Dispose();
		_isDisposed = true;
	}

	#endregion
}
