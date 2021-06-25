## Why not add this dependency from NuGet?

The System.Net.WebSockets.Client.Managed assembly in NuGet is not signed, it is a weakly named assembly.

So, I generated the il file based on the assembly in NuGet, and finally signed it, and got this strong-named assembly.

https://stackoverflow.com/a/331555/7771913