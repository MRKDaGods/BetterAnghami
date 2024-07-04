using MRK;
using static System.Console;

var rpc = AnghamiRPC.Instance;

if (!rpc.Initialize())
{
    WriteLine("Failed to initialize RPC");

    Task.Delay(2000)
        .ContinueWith((_) => Environment.Exit(-1))
        .GetAwaiter()
        .GetResult();
}

rpc.SetSong(0, "test song name", "elsors", "https://artwork.anghcdn.co/webp/?id=133275738&size=512");

ReadLine();

rpc.Clear();