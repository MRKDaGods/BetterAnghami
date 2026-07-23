using System.Text.Json;
using System.Windows;
using MRK.Models;

namespace MRK
{
    /// <summary>
    /// Handles all song and user data queries against the Anghami WebView.
    /// Implements <see cref="ISongHost"/> so it can be consumed directly by <see cref="AnghamiRPC"/>.
    /// </summary>
    public class SongService : ISongHost
    {
        private readonly Func<bool> _isRunning;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Last distinct scrape error, so a broken page logs once instead of every second
        /// </summary>
        private string? _lastScrapeError;

        public bool IsRunning => _isRunning();

        public SongService(Func<bool> isRunning)
        {
            _isRunning = isRunning;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        /// <summary>
        /// Gets the local Anghami user
        /// </summary>
        public async Task<User> GetLocalUser()
        {
            var json = await ActionManager.ExecuteActionRaw(
                """
                (function() {
                    var viewProfile = document.getElementsByClassName("viewprofile")[0];
                    var profileUrl = viewProfile.href;
                    var id = parseInt(profileUrl.substring(profileUrl.lastIndexOf('/') + 1));

                    // name is located in viewProfile's top sibling's text
                    var name = viewProfile.parentElement.firstChild.innerText;

                    return { Id: id, Name: name };
                })()
                """
            );

            if (json == "null")
            {
                throw new Exception("Cannot get local user");
            }

            return JsonSerializer.Deserialize<User>(json)!;
        }

        /// <summary>
        /// Gets the currently playing song regardless of play state
        /// </summary>
        public async Task<Song?> GetCurrentlyPlayingSong()
        {
            if (!IsRunning)
            {
                return null;
            }

            var json = await ActionManager.ExecuteActionRaw(
                """
                (function() {
                    // no player mounted means nothing is playing, that's not an error
                    const infoContainer = document.querySelector(".image-info-container");
                    if (!infoContainer) return null;

                    // anything past here throwing means the DOM shape changed under us, so hand
                    // back a marker instead of letting WebView2 turn the throw into a bare "null"
                    try {

                    // get image url
                    const bgImage = infoContainer.querySelector(".track-coverart").style.backgroundImage;
                    const imgUrlStart = bgImage.indexOf('"') + 1;
                    const imgUrlEnd = bgImage.lastIndexOf('"');
                    const imgUrl = bgImage.substring(imgUrlStart, imgUrlEnd);
                    
                    // get song name and id
                    const titleAnchor = infoContainer.querySelector(".action-title");
                    const name = titleAnchor.innerText;
                    const id = parseInt(titleAnchor.href.substring(titleAnchor.href.lastIndexOf('/') + 1)) || -1; // local files have no id

                    // get artist
                    const artistAnchor = infoContainer.querySelector(".action-artist");
                    const artist = artistAnchor.innerText;

                    // play details
                    const mainPlayer = document.querySelector(".main-player");
                    const playPauseCont = mainPlayer.querySelector(".play-pause-cont");
                    const playState = playPauseCont.children[0].classList[1]; // button name is the second class as of 12/7/2024

                    // durations
                    const durations = mainPlayer.querySelectorAll(".duration-text");
                    let durStart = "--", durEnd = "--";
                    if (durations.length == 2) {
                        durStart = durations[0].innerText;
                        durEnd = durations[1].innerText; // remaining time
                    }

                    return {
                        id,
                        name,
                        artist,
                        imgUrl,
                        playState,
                        durStart,
                        durEnd
                    };
                    } catch (e) {
                        return { __err: String((e && e.message) || e) };
                    }
                })()
                """
            );

            if (json == "null")
            {
                // nothing playing
                NoteScrapeOk();
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);

                // the scrape hands back { __err } when the page shape changed under it
                if (
                    doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("__err", out var err)
                )
                {
                    NoteScrapeError(err.GetString() ?? "unknown");
                    return null;
                }

                var song = doc.RootElement.Deserialize<Song>(_jsonOptions);
                NoteScrapeOk();
                return song;
            }
            catch (Exception ex)
            {
                NoteScrapeError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Logs a scrape failure once per distinct message (the RPC loop calls this every second)
        /// </summary>
        private void NoteScrapeError(string message)
        {
            if (message == _lastScrapeError)
            {
                return;
            }

            _lastScrapeError = message;
            Tracer.Warn(Tracer.Category.Rpc, $"Song scrape failed: {message}");
        }

        /// <summary>
        /// Logs recovery the first time the scrape works again after a failure
        /// </summary>
        private void NoteScrapeOk()
        {
            if (_lastScrapeError == null)
            {
                return;
            }

            _lastScrapeError = null;
            Tracer.Info(Tracer.Category.Rpc, "Song scrape recovered");
        }

        /// <summary>
        /// Synchronous <see cref="ISongHost"/> wrapper — marshals onto the UI thread as required by the interface.
        /// </summary>
        Song? ISongHost.GetCurrentlyPlayingSong() =>
            Application.Current.Dispatcher.Invoke(GetCurrentlyPlayingSong).GetAwaiter().GetResult();
    }
}
