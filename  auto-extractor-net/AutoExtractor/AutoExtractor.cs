using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AutoExtrator
{
	public class AutoExtractor
	{
		private string _winrarPath;
		private IEnumerable<string> _extentions;
		private bool _autoDelete;
		private List<FileSystemWatcher> _watchers;
		private ConcurrentDictionary<uint,DateTime> _unpackFilesHashes = new ConcurrentDictionary<uint, DateTime>();
		private Timer _cleanUpTimer;

		public AutoExtractor(string configPath)
		{
			var iniFile = new IniFile(configPath);

			var configMonitor = new FileSystemWatcher(Path.GetDirectoryName(configPath),Path.GetFileName(configPath));
			
			var readConfig = FunctionTools.Recreate(() => new
			                                      	{
			                                      		Folders = iniFile["Folders"],
														WinRar = iniFile["WinRar"].First(),
														Extentions = iniFile["Extentions"].FirstOrDefault().With(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)),
														AutoDelete = iniFile["Options"]["AutoDelete"].With(FunctionTools.ToBoolean)
													});

			configMonitor.Changed += delegate
			{
				Stop();
				iniFile = new IniFile(configPath);
				var newConfig = readConfig();
				Init(newConfig.Folders, newConfig.WinRar, newConfig.Extentions,newConfig.AutoDelete);
				Start();
			};
			var config = readConfig();
			Init(config.Folders, config.WinRar, config.Extentions, config.AutoDelete);
			configMonitor.EnableRaisingEvents = true;
		}

		public AutoExtractor(IEnumerable<string> monitoringFolders, string winrarPath, IEnumerable<string> extentions = null, bool autoDelete = false)
		{
			Init(monitoringFolders, winrarPath, extentions,autoDelete);
		}

		private void Init(IEnumerable<string> monitoringFolders, string winrarPath, IEnumerable<string> extentions, bool autoDelete)
		{
			_winrarPath = winrarPath;
			_extentions = extentions ?? new[] {".zip", ".rar"};
			_watchers = new List<FileSystemWatcher>();
			_autoDelete = autoDelete;
			_cleanUpTimer = new Timer(delegate
			                          	{
			                          		var now = DateTime.Now;
			                          		var keysForDelete = _unpackFilesHashes.Where(pair => (now - pair.Value).Hours > 1).Select(pair => pair.Key).ToArray();
											keysForDelete.ForEach(x=>_unpackFilesHashes.TryRemove(x, out now));
			                          	},null,TimeSpan.Zero,new TimeSpan(0,0,10,0));
			foreach (var watcher in monitoringFolders.Where(Directory.Exists).Select(folder => new FileSystemWatcher(folder) {IncludeSubdirectories = true}))
			{
				watcher.Created +=  WatcherHandler;
				_watchers.Add(watcher);
			}

		}

		private void WatcherHandler(object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			((Action<string>)Unpack).BeginInvoke(fileSystemEventArgs.FullPath,null,null);
		}

		private void Unpack(string file)
		{
			if (!File.Exists(file) || !_extentions.Any(file.EndsWith))
			{
				return;
			}

			byte[] fileContent = null; 
			IOUtils.WrapSharingViolations(() => fileContent = File.ReadAllBytes(file), null, int.MaxValue, 1000);
			var hash = Crc32.Compute(fileContent);

			var fileName = Path.GetFileNameWithoutExtension(file);
			var parentDirectory = Path.GetDirectoryName(file);

			if (parentDirectory == null || fileName == null || _unpackFilesHashes.ContainsKey(hash))
			{
				return;
			}

			var winRarProcess = new Process
			                    	{
			                    		StartInfo = {FileName = _winrarPath, Arguments = string.Format("x -ad -o+ \"{0}\" \"{1}\"", file, parentDirectory)},
			                    		EnableRaisingEvents = true
			                    	};

			winRarProcess.Exited += (sender, args) => ProccessWinrarExit(file, hash, winRarProcess);

			winRarProcess.Start();
		}

		private void ProccessWinrarExit(string file, uint hash, Process winRarProcess)
		{
			if (winRarProcess.ExitCode != 0)
			{
				return;
			}

			_unpackFilesHashes[hash] = DateTime.Now;
			if (_autoDelete)
			{
				((Action<string>) File.Delete).Safe()(file);
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
