using System.Text.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Media;
using StreamManager.Model.LiveAnimator;

namespace StreamManager.Services
{
    public class LiveManager
    {
        private MainWindow main;

        private HttpClient httpClient;

        public LiveManager(MainWindow main, HttpClient httpClient)
        {
            this.main = main;
            this.httpClient = httpClient;

            authentificate();
        }

        public async void authentificate()
        {
            Authentification authentification = new Authentification { username = Resources.StreamManagerUsername, password = Resources.StreamManagerPassword };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(authentification), Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/login_check", bodyAndHeader);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JsonSerializer.Deserialize<JWT>(responseBody).token);

                Application.Current.Dispatcher.Invoke(new Action(() => {
                    main.LiveAnimator.Fill = new SolidColorBrush(Colors.Green);
                }));
            }
            catch (Exception ex) { }
        }

        public async void sendSubscribeMercureMessage(string username, bool isPrime, bool? isGift, string recipient)
        {
            if (null == isGift)
            {
                isGift = false;
            }
            
            Subscribe subscribeEvent = new Subscribe { username = username, isPrime = isPrime, isGift = (bool)isGift, recipient = recipient };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(subscribeEvent), UnicodeEncoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/subscribe", bodyAndHeader);
        }

        public async void sendFollowMercureMessage(string username)
        {
            Follow followEvent = new Follow { username = username };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(followEvent), UnicodeEncoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/follow", bodyAndHeader);
        }

        public async void sendDonationMercureMessage(string username, string amount)
        {
            Donation donationEvent = new Donation { username = username, amount = $"{amount} coins" };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(donationEvent), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/donation", bodyAndHeader);
        }

        public async void sendRaidMercureMessage(string username, string viewers)
        {
            Raid raidEvent = new Raid { username = username, viewers = viewers };
            StringContent bodyAndHeader = new StringContent(JsonSerializer.Serialize(raidEvent), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/raid", bodyAndHeader);
        }
    }
}