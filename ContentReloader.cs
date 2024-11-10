using System;
using System.IO;
using System.Threading;

namespace NuTiled;
public class ContentReloader
{
	private FileSystemWatcher _watcher;
	string WritePath;
	public bool ReloadFlag;

	public ContentReloader(string pathToWatch, string pathToWrite) {
		WritePath = pathToWrite;
		_watcher = new FileSystemWatcher(pathToWatch);
		_watcher.Changed += OnChanged;
		_watcher.EnableRaisingEvents = true;
	}
	private void OnChanged(object sender, FileSystemEventArgs e) {
		string path = e.FullPath;
		if(path.Contains(".tmx") || path.Contains(".tsx") || path.Contains(".tx")) {
			ReloadAsset(path);
		}
	}

	//For debug, copy asset from solution directory, for release, just set flag.
	private void ReloadAsset(string path) {
		Thread.Sleep(500); //if it throws here due to huge map etc, use some try/catch on a loop etc. 
#if (DEBUG)
		Console.WriteLine($"file changed: {path}");
		string path_folder = Path.GetDirectoryName(path);
		string filename = Path.GetFileNameWithoutExtension(path); //it actually registers a temp file with a random extension.
		File.Copy(Path.Combine(path_folder, filename), Path.Combine(WritePath, filename), true);
		Console.WriteLine($"wrote {filename}");
#endif
		ReloadFlag = true;
	}


}
