using Newtonsoft.Json;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Services
{
    public class LiveManager
    {
        private MainWindow main;

        private HttpClient httpClient;

        public LiveManager(MainWindow main)
        {
            this.main = main;

            httpClient = new HttpClient();

            authentificate();
        }

        public async void authentificate()
        {
            StringContent bodyAndHeader = new StringContent(
                "{\"username\": \"" + Resources.StreamManagerUsername + "\", \"password\": \"" + Resources.StreamManagerPassword + "\" }",
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/login_check", bodyAndHeader);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JsonConvert.DeserializeObject<JWT>(responseBody).token);

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

            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "username", username },
                { "isPrime", isPrime ? "1" : "0" },
                { "isGift", (bool) isGift ? "1" : "0" },
                { "recipient", recipient }
            };

            FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/subscribe", encodedContent);
        }

        public async void sendFollowMercureMessage(string username)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "username", username }
            };

            FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/follow", encodedContent);
        }

        public async void sendDonationMercureMessage(string username, string amount)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "username", username },
                { "amount", $"{amount} coins" }
            };

            FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/donation", encodedContent);
        }

        public async void sendRaidMercureMessage(string username, string viewers)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "username", username },
                { "viewers", viewers }
            };

            FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);

            HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/api/raid", encodedContent);
        }
    }
}