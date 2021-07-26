using StreamManager.Model;
using System;
using System.Windows;
using System.Windows.Media;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace StreamManager.Services
{
    public class TwitchBot
    {
        private MainWindow main;

        private TwitchClient client;

        public TwitchBot(MainWindow main)
        {
            this.main = main;
            ConnectionCredentials credentials = new ConnectionCredentials(Resources.TwitchChannel, Resources.TwitchBotAccessToken);

            var clientOptions = new ClientOptions {MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30)};
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, Resources.TwitchChannel);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;

            client.Connect();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.TwitchBotState.Fill = new SolidColorBrush(Colors.Green);
            }));
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            //client.SendMessage(e.Channel, "Le bot Twitch est connecté !");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            // TODO
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            foreach (Command command in main.Get_ListCommands())
            {
                if (command.CommandName.ToLower() == e.Command.CommandText.ToLower())
                {
                    client.SendMessage(Resources.TwitchChannel, command.BotAnswer);
                }
            }
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            /*if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            else
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");*/
        }
    }
}