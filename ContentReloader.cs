using System;
using System.IO;
using System.Threading;

public class ContentReloader
{
	private FileSystemWatcher _watcher;

	NuTiled.Game1 game;

	public ContentReloader(string pathToWatch, string pathToWrite, NuTiled.Game1 _game) {
		game = _game;
		WritePath = Path.GetDirectoryName(pathToWrite);
		_watcher = new FileSystemWatcher(pathToWatch);
		_watcher.Changed += OnChanged;
		_watcher.EnableRaisingEvents = true;
	}

	string WritePath;

	private void OnChanged(object sender, FileSystemEventArgs e) {
		string path = e.FullPath;
		if(path.Contains(".tmx") || path.Contains(".tsx") || path.Contains(".tx")) {
			ReloadAsset(path);
		}
	}


	//For publish builds, copy not necessary, only ReloadMap should be necessary.
	private void ReloadAsset(string path) {
		Console.WriteLine($"file changed: {path}");
		Thread.Sleep(500); //if it throws here due to huge map etc, use some try/catch on a loop etc, I can't be bothered
		string path_folder = Path.GetDirectoryName(path);
		string filename = Path.GetFileNameWithoutExtension(path); //it actually registers a temp file with a random extension.
		//Console.WriteLine(filename);
		File.Copy(Path.Combine(path_folder, filename), Path.Combine(WritePath, filename), true);
		game.ReloadMap();
		Console.WriteLine($"wrote {filename}");
	}


}
