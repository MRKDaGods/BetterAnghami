using MRK;
using MRK.Models;
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

rpc.SetSong(new Song(0,
                     "test",
                     "elsors",
                     "https://artwork.anghcdn.co/webp/?id=133275738&size=512",
                     "buffering",
                     "0:00",
                     "3:00"));

ReadLine();

rpc.Clear();