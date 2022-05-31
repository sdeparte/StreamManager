using System;
using System.IO;
using System.Windows.Media;
using TagLib;

namespace StreamManager.Services
{
    public class MusicPlayer
    {
        private const string DEFAULT_AUTHOR = "Sylvain D";
        private const string DEFAULT_SONG = "The silence";
        private const string DEFAULT_ALBUM_IMG = "black";

        private readonly LiveManager _liveManager;

        private readonly MediaPlayer _mediaPlayer;

        private string _musicFolder;
        private string _currentSong;
        private bool _isPaused;

        public string CurrentSong
        {
            get { return _currentSong; }
        }

        public event EventHandler<string> NewSongPlaying;

        public MusicPlayer(LiveManager liveManager)
        {
            _liveManager = liveManager;
            _liveManager.IsAuthenticated += OnLiveManagerAuthenticated;

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += MediaPlay_SourceEnded;
        }

        private void OnLiveManagerAuthenticated(object sender, bool e)
        {
            Stop();
        }

        private void MediaPlay_SourceEnded(object sender, EventArgs e)
        {
            PlayNextSong();
        }

        public void StartFolder(string musicFolder)
        {
            this._musicFolder = musicFolder;
            _isPaused = false;

            PlayNextSong();
        }

        public void PlayNextSong()
        {
            string[] fileEntries = Directory.GetFiles(_musicFolder, "*.mp3");

            if (fileEntries.Length > 0)
            {
                int rand = new Random().Next(0, fileEntries.Length - 1);
                string mp3Path = fileEntries[rand];

                TagLib.File file = TagLib.File.Create(mp3Path);

                IPicture firstPicture = file.Tag.Pictures[0] ?? null;

                if (firstPicture != null)
                {
                    byte[] imageArray = firstPicture.Data.Data;
                    string base64ImageRepresentation = Convert.ToBase64String(imageArray);

                    _currentSong = String.Join(", ", file.Tag.AlbumArtists) + " - " + file.Tag.Title;
                    NewSongPlaying?.Invoke(this, _currentSong);

                    _liveManager.SendNewSongMercureMessage(String.Join(", ", file.Tag.AlbumArtists), file.Tag.Title, $"url(data:{firstPicture.MimeType};base64,{base64ImageRepresentation})", false);

                    _mediaPlayer.Open(new Uri(mp3Path));
                    _mediaPlayer.Play();
                }
            }
        }

        public void Pause()
        {
            if (!_isPaused)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.Play();
            }

            _isPaused = !_isPaused;
            _liveManager.SendPauseSongMercureMessage(_isPaused);
        }

        public void Stop()
        {
            _mediaPlayer.Stop();

            _currentSong = DEFAULT_AUTHOR + " - " + DEFAULT_SONG;
            NewSongPlaying?.Invoke(this, _currentSong);

            _liveManager.SendNewSongMercureMessage(DEFAULT_AUTHOR, DEFAULT_SONG, DEFAULT_ALBUM_IMG, true);
        }
    }
}