using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;

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