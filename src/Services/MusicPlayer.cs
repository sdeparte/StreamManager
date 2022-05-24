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

        private MainWindow main;

        private MediaPlayer mediaPlayer;

        private string musicFolder;
        private string currentSong;
        private bool isPaused;

        public string CurrentSong { get { return currentSong; } }

        public MusicPlayer(MainWindow main)
        {
            this.main = main;

            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlay_SourceEnded;

            Stop();
        }

        public void StartFolder(string musicFolder)
        {
            this.musicFolder = musicFolder;
            isPaused = false;

            PlayNextSong();
        }

        private void MediaPlay_SourceEnded(object sender, EventArgs e)
        {
            PlayNextSong();
        }

        public void PlayNextSong()
        {
            string[] fileEntries = Directory.GetFiles(musicFolder, "*.mp3");

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

                    currentSong = String.Join(", ", file.Tag.AlbumArtists) + " - " + file.Tag.Title;
                    main.CurrentSong.Text = currentSong;

                    main.Get_LiveManager().sendNewSongMercureMessage(String.Join(", ", file.Tag.AlbumArtists), file.Tag.Title, $"url(data:{firstPicture.MimeType};base64,{base64ImageRepresentation})", false);

                    mediaPlayer.Open(new Uri(mp3Path));
                    mediaPlayer.Play();
                }
            }
        }

        public void Pause()
        {
            if (!isPaused)
            {
                mediaPlayer.Pause();
            }
            else
            {
                mediaPlayer.Play();
            }

            isPaused = !isPaused;
            main.Get_LiveManager().sendPauseSongMercureMessage(isPaused);
        }

        public void Stop()
        {
            mediaPlayer.Stop();

            currentSong = DEFAULT_AUTHOR + " - " + DEFAULT_SONG;
            main.CurrentSong.Text = currentSong;

            main.Get_LiveManager().sendNewSongMercureMessage(DEFAULT_AUTHOR, DEFAULT_SONG, DEFAULT_ALBUM_IMG, true);
        }
    }
}