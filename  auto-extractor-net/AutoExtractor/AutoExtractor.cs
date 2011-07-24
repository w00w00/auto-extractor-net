using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AutoExtrator
{
	public class AutoExtractor
	{
		private string _winrarPath;
		private IEnumerable<string> _extentions;
		private List<FileSystemWatcher> _watchers;

		public AutoExtractor(string configPath)
		{
			var iniFile = new IniFile(configPath);

			var configMonitor = new FileSystemWatcher(Path.GetDirectoryName(configPath),Path.GetFileName(configPath));
			
			var readConfig = FunctionTools.Recreate(() => new
			                                      	{
			                                      		Folders = iniFile["Folders"],
														WinRar = iniFile["WinRar"].First(),
														Extentions = iniFile["Extentions"].FirstOrDefault().With(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
													});

			configMonitor.Changed += delegate
			{
				Stop();
				iniFile = new IniFile(configPath);
				var newConfig = readConfig();
				Init(newConfig.Folders, newConfig.WinRar, newConfig.Extentions);
				Start();
			};
			var config = readConfig();
			Init(config.Folders, config.WinRar, config.Extentions);
			configMonitor.EnableRaisingEvents = true;
		}

		public AutoExtractor(IEnumerable<string> monitoringFolders, string winrarPath, IEnumerable<string> extentions = null)
		{
			Init(monitoringFolders, winrarPath, extentions);
		}

		private void Init(IEnumerable<string> monitoringFolders, string winrarPath, IEnumerable<string> extentions)
		{
			_winrarPath = winrarPath;
			_extentions = extentions ?? new[] {".zip", ".rar"};
			_watchers = new List<FileSystemWatcher>();
			foreach (var watcher in monitoringFolders.Where(Directory.Exists).Select(folder => new FileSystemWatcher(folder) {IncludeSubdirectories = true}))
			{
				watcher.Created += WatcherHandler;
				_watchers.Add(watcher);
			}
		}

		private void WatcherHandler(object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			var file = fileSystemEventArgs.FullPath;
			if (!File.Exists(file) || !_extentions.Any(file.EndsWith))
			{
				return;
			}

			IOUtils.WrapSharingViolations(() => { using (File.OpenRead(file));}, null, int.MaxValue, 1000);

			var fileName = Path.GetFileNameWithoutExtension(file);
			var parentDirectory = Path.GetDirectoryName(file);

			if(parentDirectory == null || fileName == null)
			{
			   return;
			}

			var winRarProcess = Process.Start(_winrarPath, string.Format("x -ad -o+ \"{0}\" \"{1}\"", file, parentDirectory));
			if (winRarProcess != null)
			{
				winRarProcess.WaitForExit(3000);
			}
		}

		public void Start()
		{
			_watchers.ForEach(x=>x.EnableRaisingEvents=true);
		}

		public void Stop()
		{
			_watchers.ForEach(x=>x.EnableRaisingEvents=false);
		}
	}
}
