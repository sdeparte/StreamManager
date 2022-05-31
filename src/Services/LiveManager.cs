using System.Text.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using StreamManager.Model.LiveAnimator;
using System.Windows.Media;

namespace StreamManager.Services
{
    public class LiveManager
    {
        private readonly HttpClient _httpClient;

        private bool _state { get; set; }

        public event EventHandler<bool> IsAuthenticated;

        public SolidColorBrush StateBrush
        {
            get
            {
                return new SolidColorBrush(_state ? Colors.Green : Colors.Red);
            }
        }

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
            }
            catch (Exception)
            {
                _state = false;
            }

            IsAuthenticated?.Invoke(this, _state);
        }

        public async void SendSubscribeMercureMessage(string username, bool isPrime, bool? isGift, string recipient)
        {
            if (null == isGift)
            {
                isGift = false;
            }
            
            Subscribe subscribeEvent = new Subscribe { username = username, isPrime = isPrime, isGift = (bool)isGift, recipient = recipient };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(subscribeEvent), UnicodeEncoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/subscribe", bodyAndHeader);
        }

        public async void SendFollowMercureMessage(string username)
        {
            Follow followEvent = new Follow { username = username };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(followEvent), UnicodeEncoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/follow", bodyAndHeader);
        }

        public async void SendDonationMercureMessage(string username, string amount)
        {
            Donation donationEvent = new Donation { username = username, amount = $"{amount} coins" };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(donationEvent), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/donation", bodyAndHeader);
        }

        public async void SendRaidMercureMessage(string username, string viewers)
        {
            Raid raidEvent = new Raid { username = username, viewers = viewers };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(raidEvent), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/raid", bodyAndHeader);
        }

        public async void SendNewSongMercureMessage(string author, string song, string albumImg, bool noSound)
        {
            Song songEvent = new Song { author = author, song = song, noSound = noSound, albumImg = albumImg };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(songEvent), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/music", bodyAndHeader);
        }

        public async void SendPauseSongMercureMessage(bool pause)
        {
            PauseSong pauseSongEvent = new PauseSong { noSound = pause };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(pauseSongEvent), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/music/noSound", bodyAndHeader);
        }
    }
}