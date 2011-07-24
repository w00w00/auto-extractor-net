using System;
using System.IO;
using AutoExtrator;

namespace AE.Console
{
	static class Program
	{
		static void Main()
		{
			var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitors.cfg");
			var extractor = new AutoExtractor(configPath);
			extractor.Start();

			System.Console.WriteLine("Press any key to exit");

			System.Console.ReadKey();
		}
	}
}
