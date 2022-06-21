using StreamManager.Helpers;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Media;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Search;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
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

        private readonly LiveManager _liveManager;
        private readonly MusicPlayer _musicPlayer;

        private bool _clientState = false;
        private bool _followerServiceState = false;
        private bool _subServiceState = false;

        private readonly ObservableCollection<string> _listPossibleCommandActions = new ObservableCollection<string>();

        public event EventHandler<bool> ClientLogged;
        public event EventHandler<bool> FollowerServiceStarted;
        public event EventHandler<bool> SubServiceStarted;

        public event EventHandler<Command> NewCommandRaised;

        public ObservableCollection<string> ListPossibleCommandActions => _listPossibleCommandActions;

        public ObservableCollection<Command> ListCommands { get; set; } = new ObservableCollection<Command>();

        public SolidColorBrush ClientStateBrush => new SolidColorBrush(_clientState ? Colors.Green : Colors.Red);

        public SolidColorBrush FollowerServiceStateBrush => new SolidColorBrush(_followerServiceState ? Colors.Green : Colors.Red);

        public SolidColorBrush SubServiceState => new SolidColorBrush(_subServiceState ? Colors.Green : Colors.Red);

        public TwitchBot(LiveManager liveManager, MusicPlayer musicPlayer)
        {
            _liveManager = liveManager;
            _musicPlayer = musicPlayer;

            foreach (string commandAction in _enumCommandActions)
            {
                _listPossibleCommandActions.Add(commandAction);
            }
        }

        public void Connect()
        {
            try
            {
                if (new Ping().Send("www.google.com.mx").Status == IPStatus.Success)
                {
                    InitTwitchClient();
                    InitTwitchApi();
                    InitTwitchPubSub();
                }
                else
                {
                    ToastHelper.Toast("Aucune connectivité", $"Impossible de se conntecter à Twitch");
                }
            }
            catch (Exception) { }
        }

        public int GetCommandIndex(string command)
        {
            return Array.IndexOf(_enumCommandActions, command);
        }

        public void AddCommand(string commandName, int commandAction, string botNote, string resource)
        {
            foreach (Command command in ListCommands)
            {
                if (command.CommandName == commandName)
                {
                    ListCommands.Remove(command);
                    break;
                }
            }

            ListCommands.Add(new Command() { CommandName = commandName, Action = _enumCommandActions[commandAction], Resource = resource, BotNote = botNote });
        }

        public void RemoveCommandAt(int index)
        {
            ListCommands.RemoveAt(index);
        }

        public Command GetCommandAt(int index)
        {
            return ListCommands[index];
        }

        #region Client
        private TwitchClient _client;

        public void Client_SendMessage(string message)
        {
            _client.SendMessage(Resources.TwitchChannel, message);
        }

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
            List<string> commands = new List<string>
            {
                "!help"
            };

            foreach (Command command in ListCommands)
            {
                commands.Add("!" + command.CommandName);
            }

            _client.SendMessage(e.Channel, "Le bot Twitch est connecté !\nListe des commandes disponibles : " + String.Join(", ", commands.ToArray()) + ".");
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if ("help" == e.Command.CommandText.ToLower())
            {
                List<string> commands = new List<string>
                {
                    "!help"
                };

                foreach (Command command in ListCommands)
                {
                    commands.Add("!" + command.CommandName);
                }

                Client_SendMessage($"Liste des commandes disponibles : {String.Join(", ", commands.ToArray())}.");

                return;
            }

            foreach (Command command in ListCommands)
            {
                if (command.CommandName.ToLower() == e.Command.CommandText.ToLower())
                {
                    NewCommandRaised?.Invoke(this, command);
                    break;
                }
            }
        }

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            _musicPlayer.SetVolume(0.25);

            _liveManager.SendRaidMercureMessage(e.RaidNotification.Login, e.RaidNotification.MsgParamViewerCount);

            Task.Delay(6000).ContinueWith(t => _musicPlayer.SetVolume(1));
        }
        #endregion

        #region Api
        private TwitchAPI _broadcasterApi;
        private TwitchAPI _botApi;
        private FollowerService _followerSerice;
        private bool _firstCall = false;

        private void InitTwitchApi()
        {
            _broadcasterApi = new TwitchAPI();
            _broadcasterApi.Settings.ClientId = Resources.TwithBoradcasterClientId;
            _broadcasterApi.Settings.AccessToken = Resources.TwitchBoradcasterAccessToken;

            _botApi = new TwitchAPI();
            _botApi.Settings.ClientId = Resources.TwitchBotClientId;
            _botApi.Settings.AccessToken = Resources.TwitchBotAccessToken;

            _followerSerice = new FollowerService(_botApi, 5);
            _followerSerice.SetChannelsById(new List<string> { Resources.TwitchUserId });

            _followerSerice.OnServiceStarted += FollowerService_OnServiceStarted;
            _followerSerice.OnNewFollowersDetected += FollowerService_OnNewFollowersDetected;

            _followerSerice.Start();
        }

        public async Task<List<Category>> SearchCategoriesAsync(string query)
        {
            List<Category> games = new List<Category>();

            SearchCategoriesResponse result = await _botApi.Helix.Search.SearchCategoriesAsync(query);

            foreach (Game game in result.Games)
            {
                games.Add(new Category() { Id = game.Id, Name = game.Name });
            }
            
            return games;
        }

        public void EditStreamInformationsAsync(string gameId, string title)
        {
            ModifyChannelInformationRequest request = new ModifyChannelInformationRequest() { GameId = gameId, Title = title };
            _broadcasterApi.Helix.Channels.ModifyChannelInformationAsync(Resources.TwitchUserId, request);
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
                List<string> followers = new List<string>();
                
                foreach (Follow follow in e.NewFollowers)
                {
                    followers.Add(follow.FromUserName);
                }

                _musicPlayer.SetVolume(0.25);

                _liveManager.SendFollowMercureMessage(followers.ToArray());

                Task.Delay(6000).ContinueWith(t => _musicPlayer.SetVolume(1));
            }
            else
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
            _musicPlayer.SetVolume(0.25);

            _liveManager.SendSubscribeMercureMessage(e.Subscription.Username, (int)e.Subscription.SubscriptionPlan == (int)SubscriptionPlan.Prime, e.Subscription.IsGift, e.Subscription.RecipientName);

            Task.Delay(6000).ContinueWith(t => _musicPlayer.SetVolume(1));
        }

        private void PubSub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            _musicPlayer.SetVolume(0.25);

            _liveManager.SendDonationMercureMessage(e.Username, e.TotalBitsUsed + " bits");

            Task.Delay(6000).ContinueWith(t => _musicPlayer.SetVolume(1));
        }
        #endregion
    }
}