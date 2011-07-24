using System;
using System.IO;
using System.ServiceProcess;
using AutoExtrator;

namespace AE.Service
{
	public partial class ExtractorService : ServiceBase
	{
		private readonly AutoExtractor _extractor;

		public ExtractorService()
		{
			InitializeComponent();
			var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitors.cfg");
			
			_extractor = new AutoExtractor(configPath);
		}

		protected override void OnStart(string[] args)
		{
			_extractor.Start();
		}

		protected override void OnStop()
		{
			_extractor.Stop();
		}
	}
}
