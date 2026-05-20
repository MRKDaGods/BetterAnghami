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
                    // too lazy to use getxxx
                    const infoContainer = document.querySelector(".image-info-container");

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
                })()
                """
            );

            if (json == "null")
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Song>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Synchronous <see cref="ISongHost"/> wrapper — marshals onto the UI thread as required by the interface.
        /// </summary>
        Song? ISongHost.GetCurrentlyPlayingSong() =>
            Application.Current.Dispatcher.Invoke(GetCurrentlyPlayingSong).GetAwaiter().GetResult();
    }
}
