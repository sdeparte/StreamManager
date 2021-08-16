using Newtonsoft.Json;
using StreamManager.Model;
using System;
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
                HttpResponseMessage response = await httpClient.PostAsync($"{Resources.StreamManagerUrl}/authentication_token", bodyAndHeader);
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

            HttpResponseMessage response = await httpClient.GetAsync($"{Resources.StreamManagerUrl}/api/subscribe?username={username}&isPrime={isPrime}&isGift={isGift}&recipient={recipient}");
        }

        public async void sendFollowMercureMessage(string username)
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{Resources.StreamManagerUrl}/api/follow?username={username}");
        }

        public async void sendDonationMercureMessage(string username, string amount)
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{Resources.StreamManagerUrl}/api/donation?username={username}&amount={amount}");
        }

        public async void sendRaidMercureMessage(string username, string viewers)
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{Resources.StreamManagerUrl}/api/raid?username={username}&viewers={viewers}");
        }
    }
}