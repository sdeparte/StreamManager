using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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
    public class TwitchBot
    {
        private readonly string[] commandActions = new string[3] {
            "Envoyer un message",
            "Envoyer une note MIDI",
            "Transférer une note MIDI"
        };

        private MainWindow main;

        public TwitchBot(MainWindow main)
        {
            this.main = main;

            try
            {
                if (new Ping().Send("www.google.com.mx").Status == IPStatus.Success)
                {
                    initTwitchClient();
                    initTwitchApi();
                    initTwitchPubSub();
                }
            }
            catch (Exception ex) { }

            main.CommandActions.Items.Clear();

            foreach (string action in commandActions)
            {
                main.CommandActions.Items.Add(action);
            }

            main.CommandActions.Text = commandActions[0];
        }

        public string[] Get_CommandActions()
        {
            return commandActions;
        }

        #region Client
        private TwitchClient client;

        private void initTwitchClient()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Resources.TwitchChannel, Resources.TwitchBotAccessToken);
            ClientOptions clientOptions = new ClientOptions { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };
            WebSocketClient customClient = new WebSocketClient(clientOptions);

            client = new TwitchClient(customClient);
            client.Initialize(credentials, Resources.TwitchChannel);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnRaidNotification += Client_OnRaidNotification;

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
            List<string> commands = new List<string>();

            commands.Add("!help");

            foreach (Command command in main.Get_ListCommands())
            {
                commands.Add("!" + command.CommandName);
            }

            client.SendMessage(e.Channel, "Le bot Twitch est connecté !\nListe des commandes disponibles : " + String.Join(", ", commands.ToArray()) + ".");
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if ("help" == e.Command.CommandText.ToLower())
            {
                List<string> commands = new List<string>();

                commands.Add("!help");

                foreach (Command command in main.Get_ListCommands())
                {
                    commands.Add("!" + command.CommandName);
                }

                client.SendMessage(Resources.TwitchChannel, "Liste des commandes disponibles : " + String.Join(", ", commands.ToArray()) + ".");

                return;
            }

            foreach (Command command in main.Get_ListCommands())
            {
                if (command.CommandName.ToLower() == e.Command.CommandText.ToLower())
                {
                    int midiNote = -1;

                    switch (Array.IndexOf(commandActions, command.Action))
                    {
                        case 1:
                            int.TryParse(command.BotNote, out midiNote);

                            main.Get_MidiController().UpMidiNote(midiNote);
                            break;
                        case 2:
                            int.TryParse(command.BotNote, out midiNote);

                            main.Get_MidiController().ForwardMidiNote(midiNote);
                            break;
                        default:
                            client.SendMessage(Resources.TwitchChannel, main.Get_MessageTemplating().renderMessage(command.BotAnswer));
                            break;
                    }

                    return;
                }
            }
        }

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            main.Get_LiveManager().sendRaidMercureMessage(e.RaidNotification.Login, e.RaidNotification.MsgParamViewerCount);
        }
        #endregion

        #region Api
        private TwitchAPI api;
        private FollowerService followerSerice;
        private bool firstCall = false;

        private void initTwitchApi()
        {
            api = new TwitchAPI();
            api.Settings.ClientId = Resources.TwitchBotClientId;
            api.Settings.AccessToken = Resources.TwitchBotAccessToken;

            followerSerice = new FollowerService(api, 5);
            followerSerice.SetChannelsById(new List<string> { Resources.TwitchUserId });

            followerSerice.OnServiceStarted += FollowerService_OnServiceStarted;
            followerSerice.OnNewFollowersDetected += FollowerService_OnNewFollowersDetected;

            followerSerice.Start();
        }

        private void FollowerService_OnServiceStarted(object sender, OnServiceStartedArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.TwitchApiState.Fill = new SolidColorBrush(Colors.Green);
            }));
        }

        private void FollowerService_OnNewFollowersDetected(object sender, OnNewFollowersDetectedArgs e)
        {
            if (firstCall)
            {
                foreach (Follow follow in e.NewFollowers)
                {
                    main.Get_LiveManager().sendFollowMercureMessage(follow.FromUserName);
                }
            } else
            {
                firstCall = true;
            }
        }
        #endregion

        #region PubSub
        private TwitchPubSub pubSub;

        private void initTwitchPubSub()
        {
            pubSub = new TwitchPubSub();

            pubSub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;

            pubSub.OnChannelSubscription += PubSub_OnChannelSubscription;
            pubSub.ListenToSubscriptions(Resources.TwitchUserId);

            pubSub.OnBitsReceived += PubSub_OnBitsReceived;
            pubSub.ListenToBitsEvents(Resources.TwitchUserId);

            pubSub.Connect();
        }

        private void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.TwitchPubSubState.Fill = new SolidColorBrush(Colors.Green);
            }));
        }

        private void PubSub_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            main.Get_LiveManager().sendSubscribeMercureMessage(e.Subscription.Username, (int) e.Subscription.SubscriptionPlan == (int) SubscriptionPlan.Prime, e.Subscription.IsGift, e.Subscription.RecipientName);
        }

        private void PubSub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            main.Get_LiveManager().sendDonationMercureMessage(e.Username, e.TotalBitsUsed + " bits");
        }
        #endregion
    }
}