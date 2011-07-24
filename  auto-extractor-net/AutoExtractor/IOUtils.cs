using System;
using System.IO;
using System.Reflection;

namespace AutoExtrator
{
	public static class IOUtils
	{
		/// <summary>
		/// Wraps sharing violations that could occur on a file IO operation.
		/// </summary>
		/// <param name="action">The action to execute. May not be null.</param>
		/// <param name="exceptionsCallback">The exceptions callback. May be null.</param>
		/// <param name="retryCount">The retry count.</param>
		/// <param name="waitTime">The wait time in milliseconds.</param>
		public static void WrapSharingViolations(WrapSharingViolationsCallback action, WrapSharingViolationsExceptionsCallback exceptionsCallback = null, int retryCount = 10, int waitTime = 100)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			for (int i = 0; i < retryCount; i++)
			{
				try
				{
					action();
					return;
				}
				catch (IOException ioe)
				{
					if ((IsSharingViolation(ioe) && (i < (retryCount - 1))) || (ioe is FileNotFoundException))
					{
						var wait = true;
						if (exceptionsCallback != null)
						{
							wait = exceptionsCallback(ioe, i, retryCount, waitTime);
						}
						if (wait)
						{
							System.Threading.Thread.Sleep(waitTime);
						}
					}
					else
					{
						throw;
					}
				}
			}
		}

		/// <summary>
		/// Defines a sharing violation wrapper delegate.
		/// </summary>
		public delegate void WrapSharingViolationsCallback();

		/// <summary>
		/// Defines a sharing violation wrapper delegate for handling exception.
		/// </summary>
		public delegate bool WrapSharingViolationsExceptionsCallback(IOException ioe, int retry, int retryCount, int waitTime);

		/// <summary>
		/// Determines whether the specified exception is a sharing violation exception.
		/// </summary>
		/// <param name="exception">The exception. May not be null.</param>
		/// <returns>
		///     <c>true</c> if the specified exception is a sharing violation exception; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsSharingViolation(IOException exception)
		{
			if (exception == null)
				throw new ArgumentNullException("exception");

			int hr = GetHResult(exception, 0);
			return (hr == -2147024864); // 0x80070020 ERROR_SHARING_VIOLATION

		}

		/// <summary>
		/// Gets the HRESULT of the specified exception.
		/// </summary>
		/// <param name="exception">The exception to test. May not be null.</param>
		/// <param name="defaultValue">The default value in case of an error.</param>
		/// <returns>The HRESULT value.</returns>
		private static int GetHResult(IOException exception, int defaultValue)
		{
			if (exception == null)
				throw new ArgumentNullException("exception");

			try
			{
				return (int)exception.GetType().GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(exception, null);
			}
			catch
			{
				return defaultValue;
			}
		}
	}
}