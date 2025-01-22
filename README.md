This is an example Monogame project using the DotTiled library.

To use this project, clone this repo, and either add DotTiled via Nuget, or clone that repository into your projects directory, next to this project (not the NuTiled directory).

As of now, this doesn't function as its own library, but rather as an entire example Monogame project. I am considering refactoring it as a library, but there are difficulties there. Check out https://github.com/differenceclouds/NuTiledLibrary to see that progress. 

I am not using the Monogame Content Pipeline, but instead loading assets at runtime. Take a look at `#region asset loading` in TiledMap.cs to see how I am doing this.
