﻿using System.Text.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using StreamManager.Model.LiveAnimator;
using System.Windows.Media;
using StreamManager.Helpers;

namespace StreamManager.Services
{
    public class LiveManager
    {
        private readonly HttpClient _httpClient;

        private bool _state = false;

        public event EventHandler<bool> IsAuthenticated;

        public SolidColorBrush StateBrush => new SolidColorBrush(_state ? Colors.Green : Colors.Red);

        public LiveManager(HttpClient httpClient)
        {
            this._httpClient = httpClient;

            Authentificate();
        }

        public async void Authentificate()
        {
            Authentification authentification = new Authentification { username = Resources.StreamManagerUsername, password = Resources.StreamManagerPassword };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(authentification), Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/login_check", bodyAndHeader);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JsonSerializer.Deserialize<JWT>(responseBody).token);

                _state = true;
                ToastHelper.Toast("Connexion réussi", $"Vous êtes connecté à LiveAnimator", false);
            }
            catch (Exception)
            {
                _state = false;
                ToastHelper.Toast("Connexion impossible", $"Impossible de se connecter à LiveAnimator");
            }

            IsAuthenticated?.Invoke(this, _state);
        }

        public async void SendSubscribeMercureMessage(string username, bool isPrime, bool? isGift, string recipient)
        {
            try
            {
                if (null == isGift)
                {
                    isGift = false;
                }

                Subscribe subscribeEvent = new Subscribe { username = username, isPrime = isPrime, isGift = (bool)isGift, recipient = recipient };
                StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(subscribeEvent), UnicodeEncoding.UTF8, "application/json");

                _ = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/subscribe", bodyAndHeader);
            }
            catch (Exception)
            {
                ToastHelper.Toast("Problème de communication", $"Impossible d'envoyer l'évenement \"subscribe\" à LiveAnimateor");
            }
        }

        public async void SendFollowMercureMessage(string[] usernames)
        {
            try
            {
                Follow followEvent = new Follow { usernames = String.Join(", ", usernames) };
                StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(followEvent), UnicodeEncoding.UTF8, "application/json");

                _ = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/follow", bodyAndHeader);
            }
            catch (Exception)
            {
                ToastHelper.Toast("Problème de communication", $"Impossible d'envoyer l'évenement \"follow\" à LiveAnimateor");
            }
        }

        public async void SendDonationMercureMessage(string username, string amount)
        {
            try
            {
                Donation donationEvent = new Donation { username = username, amount = $"{amount} coins" };
                StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(donationEvent), Encoding.UTF8, "application/json");

                _ = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/donation", bodyAndHeader);
            }
            catch (Exception)
            {
                ToastHelper.Toast("Problème de communication", $"Impossible d'envoyer l'évenement \"donation\" à LiveAnimateor");
            }
        }

        public async void SendRaidMercureMessage(string username, string viewers)
        {
            try
            {
                Raid raidEvent = new Raid { username = username, viewers = viewers };
                StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(raidEvent), Encoding.UTF8, "application/json");

                _ = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/raid", bodyAndHeader);
            }
            catch (Exception)
            {
                ToastHelper.Toast("Problème de communication", $"Impossible d'envoyer l'évenement \"raid\" à LiveAnimateor");
            }
        }

        public async void SendNewSongMercureMessage(string author, string song, string albumImg, bool noSound)
        {
            try
            {
                Song songEvent = new Song { author = author, song = song, noSound = noSound, albumImg = albumImg };
                StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(songEvent), Encoding.UTF8, "application/json");

                _ = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/music", bodyAndHeader);
            }
            catch (Exception)
            {
                ToastHelper.Toast("Problème de communication", $"Impossible d'envoyer l'évenement \"music\" à LiveAnimateor");
            }
        }

        public async void SendPauseSongMercureMessage(bool pause)
        {
            try
            {
                PauseSong pauseSongEvent = new PauseSong { noSound = pause };
                StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(pauseSongEvent), Encoding.UTF8, "application/json");

                _ = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/music/noSound", bodyAndHeader);
            }
            catch (Exception)
            {
                ToastHelper.Toast("Problème de communication", $"Impossible d'envoyer l'évenement \"music/noSound\" à LiveAnimateor");
            }
        }
    }
}