using System;
using System.Security.Cryptography;
using System.Text;

namespace Monitorian.Core.Helper;

/// <summary>
/// Utility methods to protect/unprotect a string by DPAPI for the current user
/// </summary>
internal static class StringProtection
{
	public static string Protect(string source)
	{
		if (string.IsNullOrEmpty(source))
			return null;

		try
		{
			var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(source), null, DataProtectionScope.CurrentUser);
			return Convert.ToBase64String(bytes);
		}
		catch (CryptographicException)
		{
			return null;
		}
	}

	public static string Unprotect(string protectedSource)
	{
		if (string.IsNullOrEmpty(protectedSource))
			return null;

		try
		{
			var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedSource), null, DataProtectionScope.CurrentUser);
			return Encoding.UTF8.GetString(bytes);
		}
		catch (Exception ex) when (ex is CryptographicException or FormatException)
		{
			return null;
		}
	}
}
