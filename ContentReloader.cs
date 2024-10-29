﻿using System;
using System.IO;
using System.Threading;

public class ContentReloader
{
	private FileSystemWatcher _watcher;

	NuTiled.Game1 game;

	public ContentReloader(string pathToWatch, NuTiled.Game1 _game) {
		game = _game;
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


	//For publish builds, copy not necessary, only ReloadMap should be necessary.
	private void ReloadAsset(string path) {
		Console.WriteLine($"Reloading asset: {path}");
		Thread.Sleep(500);
		string path_folder = Path.GetDirectoryName(path);
		string filename = Path.GetFileNameWithoutExtension(path);
		//Console.WriteLine(filename);
		File.Copy(Path.Combine(path_folder, filename), Path.Combine("Content/tiled/", filename), true);
		game.ReloadMap();
	}


}