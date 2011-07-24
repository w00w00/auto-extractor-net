using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace AutoExtrator
{
	public class IniFile
	{
		private readonly string _filePath;
		readonly Dictionary<string, IniFileSection> _sections = new Dictionary<string, IniFileSection>();

		public class IniFileSection:IEnumerable<string>
		{
			private readonly Dictionary<string, string> _keys = new Dictionary<string, string>();
			private readonly List<string> _values = new List<string>();

			public IniFileSection(string name)
			{
				Name = name;
			}

			public string Name { get; private set; }

			public string this[string key]
			{
				get { return _keys.TryGet(key); }
				set { _keys[key] = value; }
			}

			public void Add(string value)
			{
				_values.Add(value);
			}

			public void Remove(string value)
			{
				_values.Remove(value);
			}

			public IEnumerable<string> Values
			{
				get { return _values; }
			}

			public IEnumerable<KeyValuePair<string, string>> Keys
			{
				get { return _keys; }
			}

			public IEnumerator<string> GetEnumerator()
			{
				return Values.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public IniFile()
		{

		}

		public IniFileSection this[string name]
		{
			get { return _sections.TryGet(name,()=> new IniFileSection(name)); }
		}

		public IniFile(string filePath)
		{
			_filePath = filePath;
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException("Config file not found", filePath);
			}

			var currentKey = string.Empty;
			foreach (var line in File.ReadAllLines(filePath))
			{
				if (line.StartsWith("[") && line.EndsWith("]"))
				{
					currentKey = line.Substring(1, line.Length - 2).Trim();
					continue;
				}

				if (string.IsNullOrEmpty(currentKey) || string.IsNullOrEmpty(line))
				{
					continue;
				}

				var pair = line.Split('=');
				if(pair.Length>1)
				{
					this[currentKey][pair[0].Trim()] = pair[1].Trim();
				}
				else
				{
					this[currentKey].Add(line);
				}
				
			}
		}

		public void Save()
		{
			using (var file = File.CreateText(_filePath))
			{
				foreach (var section in _sections.Values)
				{
					file.WriteLine("[{0}]", section.Name);
					section.Keys.ForEach(key=>file.WriteLine("{0}={1}", key.Key, key.Value));
					section.ForEach(file.WriteLine);
				}
			}
		}
	}	 
}
