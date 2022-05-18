﻿using System;
using System.IO;
using System.Windows.Media;
using TagLib;

namespace StreamManager.Services
{
    public class MusicPlayer
    {
        private const string DEFAULT_AUTHOR = "Sylvain D";
        private const string DEFAULT_SONG = "The silence";
        private const string DEFAULT_ALBUM_IMG = "data:image/jpeg;base64,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";

        private MainWindow main;

        private MediaPlayer mediaPlayer;

        private string musicFolder;
        private bool isPaused;

        public MusicPlayer(MainWindow main)
        {
            this.main = main;

            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlay_SourceEnded;

            main.CurrentSong.Text = DEFAULT_AUTHOR + " - " + DEFAULT_SONG;
            main.Get_LiveManager().sendNewSongMercureMessage(DEFAULT_AUTHOR, DEFAULT_SONG, DEFAULT_ALBUM_IMG, true);
        }

        public void StartFolder(string musicFolder)
        {
            this.musicFolder = musicFolder;
            isPaused = false;

            StartRandomSource();
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
            main.CurrentSong.Text = DEFAULT_AUTHOR + " - " + DEFAULT_SONG;
            main.Get_LiveManager().sendNewSongMercureMessage(DEFAULT_AUTHOR, DEFAULT_SONG, DEFAULT_ALBUM_IMG, true);
        }

        private void MediaPlay_SourceEnded(object sender, EventArgs e)
        {
            StartRandomSource();
        }

        private void StartRandomSource()
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

                    main.CurrentSong.Text = String.Join(", ", file.Tag.AlbumArtists) + " - " + file.Tag.Title;
                    main.Get_LiveManager().sendNewSongMercureMessage(String.Join(", ", file.Tag.AlbumArtists), file.Tag.Title, $"data:{firstPicture.MimeType};base64,{base64ImageRepresentation}", false);

                    mediaPlayer.Open(new Uri(mp3Path));
                    mediaPlayer.Play();
                }
            }
        }
    }
}