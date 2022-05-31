using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
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
        private readonly string[] _enumCommandActions = new string[3] {
            "Envoyer un message",
            "Envoyer une note MIDI",
            "Transférer une note MIDI"
        };

        private readonly MidiController _midiController;
        private readonly MessageTemplating _messageTemplating;
        private readonly LiveManager _liveManager;

        private bool _clientState { get; set; }
        private bool _followerServiceState { get; set; }
        private bool _subServiceState { get; set; }

        private readonly ObservableCollection<string> _listPossibleCommandActions = new ObservableCollection<string>();

        private ObservableCollection<Command> _listCommands = new ObservableCollection<Command>();

        public event EventHandler<bool> ClientLogged;
        public event EventHandler<bool> FollowerServiceStarted;
        public event EventHandler<bool> SubServiceStarted;

        public ObservableCollection<string> ListPossibleCommandActions
        {
            get { return _listPossibleCommandActions; }
        }

        public ObservableCollection<Command> ListCommands
        {
            get { return _listCommands; }
            set { _listCommands = value; }
        }

        public SolidColorBrush ClientStateBrush
        {
            get
            {
                return new SolidColorBrush(_clientState ? Colors.Green : Colors.Red);
            }
        }

        public SolidColorBrush FollowerServiceStateBrush
        {
            get
            {
                return new SolidColorBrush(_followerServiceState ? Colors.Green : Colors.Red);
            }
        }

        public SolidColorBrush SubServiceState
        {
            get
            {
                return new SolidColorBrush(_subServiceState ? Colors.Green : Colors.Red);
            }
        }

        public TwitchBot(MidiController midiController, MessageTemplating messageTemplating, LiveManager liveManager)
        {
            _midiController = midiController;
            _messageTemplating = messageTemplating;
            _liveManager = liveManager;

            try
            {
                if (new Ping().Send("www.google.com.mx").Status == IPStatus.Success)
                {
                    InitTwitchClient();
                    InitTwitchApi();
                    InitTwitchPubSub();
                }
            }
            catch (Exception) { }

            foreach (string commandAction in _enumCommandActions)
            {
                _listPossibleCommandActions.Add(commandAction);
            }
        }

        public void AddCommand(string commandName, int commandAction, string botNote, string botAnswer)
        {
                foreach (Command command in _listCommands)
                {
                    if (command.CommandName == commandName)
                    {
                        _listCommands.Remove(command);
                        break;
                    }
                }

                _listCommands.Add(new Command() { CommandName = commandName, Action = _enumCommandActions[commandAction], BotAnswer = botAnswer, BotNote = botNote });
        }

        public void RemoveCommandAt(int index)
        {
            _listCommands.RemoveAt(index);
        }

        public Command GetCommandAt(int index)
        {
            return _listCommands[index];
        }

        #region Client
        private TwitchClient _client;

        private void InitTwitchClient()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Resources.TwitchChannel, Resources.TwitchBotAccessToken);
            ClientOptions clientOptions = new ClientOptions { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };
            WebSocketClient customClient = new WebSocketClient(clientOptions);

            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, Resources.TwitchChannel);

            _client.OnLog += Client_OnLog;
            _client.OnJoinedChannel += Client_OnJoinedChannel;
            _client.OnChatCommandReceived += Client_OnChatCommandReceived;
            _client.OnRaidNotification += Client_OnRaidNotification;

            _client.Connect();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            _clientState = true;
            ClientLogged?.Invoke(this, true);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            List<string> commands = new List<string>();

            commands.Add("!help");

            foreach (Command command in _listCommands)
            {
                commands.Add("!" + command.CommandName);
            }

            _client.SendMessage(e.Channel, "Le bot Twitch est connecté !\nListe des commandes disponibles : " + String.Join(", ", commands.ToArray()) + ".");
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if ("help" == e.Command.CommandText.ToLower())
            {
                List<string> commands = new List<string>();

                commands.Add("!help");

                foreach (Command command in _listCommands)
                {
                    commands.Add("!" + command.CommandName);
                }

                _client.SendMessage(Resources.TwitchChannel, "Liste des commandes disponibles : " + String.Join(", ", commands.ToArray()) + ".");

                return;
            }

            foreach (Command command in _listCommands)
            {
                if (command.CommandName.ToLower() == e.Command.CommandText.ToLower())
                {
                    int midiNote = -1;

                    switch (Array.IndexOf(_enumCommandActions, command.Action))
                    {
                        case 1:
                            int.TryParse(command.BotNote, out midiNote);

                            _midiController.UpMidiNote(midiNote);
                            break;
                        case 2:
                            int.TryParse(command.BotNote, out midiNote);

                            _midiController.ForwardMidiNote(midiNote);
                            break;
                        default:
                            _client.SendMessage(Resources.TwitchChannel, _messageTemplating.renderMessage(command.BotAnswer));
                            break;
                    }

                    return;
                }
            }
        }

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            _liveManager.SendRaidMercureMessage(e.RaidNotification.Login, e.RaidNotification.MsgParamViewerCount);
        }
        #endregion

        #region Api
        private TwitchAPI _api;
        private FollowerService _followerSerice;
        private bool _firstCall = false;

        private void InitTwitchApi()
        {
            _api = new TwitchAPI();
            _api.Settings.ClientId = Resources.TwitchBotClientId;
            _api.Settings.AccessToken = Resources.TwitchBotAccessToken;

            _followerSerice = new FollowerService(_api, 5);
            _followerSerice.SetChannelsById(new List<string> { Resources.TwitchUserId });

            _followerSerice.OnServiceStarted += FollowerService_OnServiceStarted;
            _followerSerice.OnNewFollowersDetected += FollowerService_OnNewFollowersDetected;

            _followerSerice.Start();
        }

        private void FollowerService_OnServiceStarted(object sender, OnServiceStartedArgs e)
        {
            _followerServiceState = true;
            FollowerServiceStarted?.Invoke(this, true);
        }

        private void FollowerService_OnNewFollowersDetected(object sender, OnNewFollowersDetectedArgs e)
        {
            if (_firstCall)
            {
                foreach (Follow follow in e.NewFollowers)
                {
                    _liveManager.SendFollowMercureMessage(follow.FromUserName);
                }
            } else
            {
                _firstCall = true;
            }
        }
        #endregion

        #region PubSub
        private TwitchPubSub _pubSub;

        private void InitTwitchPubSub()
        {
            _pubSub = new TwitchPubSub();

            _pubSub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;

            _pubSub.OnChannelSubscription += PubSub_OnChannelSubscription;
            _pubSub.ListenToSubscriptions(Resources.TwitchUserId);

            _pubSub.OnBitsReceived += PubSub_OnBitsReceived;
            _pubSub.ListenToBitsEvents(Resources.TwitchUserId);

            _pubSub.Connect();
        }

        private void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            _subServiceState = true;
            SubServiceStarted?.Invoke(this, true);
        }

        private void PubSub_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            _liveManager.SendSubscribeMercureMessage(e.Subscription.Username, (int) e.Subscription.SubscriptionPlan == (int) SubscriptionPlan.Prime, e.Subscription.IsGift, e.Subscription.RecipientName);
        }

        private void PubSub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            _liveManager.SendDonationMercureMessage(e.Username, e.TotalBitsUsed + " bits");
        }
        #endregion
    }
}