using OBSWebsocketDotNet.Types;
using Ookii.Dialogs.Wpf;
using StreamManager.Model;
using StreamManager.Services;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StreamManager.Helpers;
using System.Linq;

namespace StreamManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;

        private readonly OBSLinker _obsLinker;
        private readonly TwitchBot _twitchBot;
        private readonly LiveManager _liveManager;
        private readonly MidiController _midiController;
        private readonly ConfigReader _configReader;
        private readonly MusicPlayer _musicPlayer;
        private readonly MessageTemplating _messageTemplating;

        public SolidColorBrush LiveManager_StateBrush => _liveManager?.StateBrush;

        public MainWindow()
        {
            InitializeComponent();

            _httpClient = new HttpClient();

            _configReader = new ConfigReader();

            _obsLinker = new OBSLinker();
            _liveManager = new LiveManager(_httpClient);

            _musicPlayer = new MusicPlayer(_liveManager);
            _musicPlayer.NewSongPlaying += MusicPlayer_NewSongPlaying;

            _messageTemplating = new MessageTemplating(_musicPlayer);

            _midiController = new MidiController(_httpClient);
            _midiController.NewMidiNoteReceived += MidiController_NewMidiNote;
            _midiController.NewMessageRaised += MidiController_NewMessage;

            _twitchBot = new TwitchBot(_liveManager);
            _twitchBot.NewCommandRaised += TwitchBot_NewCommand;

            _configReader.ReadConfigFiles(_midiController, _twitchBot, this);

            _twitchBot.Connect();
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            _configReader.UpdateConfigFiles(_midiController, _twitchBot, this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _musicPlayer.Stop();
        }

        #region Stream Deck
        public ObservableCollection<Message> ListActions => _midiController?.ListActions;

        public ObservableCollection<string> ListPossibleActions => _midiController?.ListPossibleActions;

        public ObservableCollection<ObservableScene> ListScenes => _obsLinker?.ListScenes;

        public ObservableCollection<ObservableSceneItem> ListSceneItems => _obsLinker?.ListSceneItems;

        public ObservableCollection<ObservableAction> ListMessageActions { get; set; } = new ObservableCollection<ObservableAction>();

        public SolidColorBrush ObsLinker_StateBrush => _obsLinker?.StateBrush;

        private void MidiController_NewMidiNote(object sender, int midiNote)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                MidiNote.Text = midiNote.ToString();
            }));
        }

        private void MidiController_NewMessage(object sender, Message message)
        {
            foreach (ObservableAction observableAction in message.Actions)
            {
                switch (_midiController.GetActionIndex(observableAction.Name))
                {
                    case 0:
                        _obsLinker.SetCurrentScene(observableAction.Scene);
                        break;

                    case 1:
                        _obsLinker.ToggleMute(observableAction.SceneItem);
                        break;

                    case 2:
                        _obsLinker.SetMute(observableAction.SceneItem, true);
                        break;

                    case 3:
                        _obsLinker.SetMute(observableAction.SceneItem, false);
                        break;

                    case 4:
                        _obsLinker.RestartMedia(observableAction.SceneItem);
                        break;

                    case 5:
                        _obsLinker.ToggleStreaming();
                        break;

                    case 6:
                        _obsLinker.StartStreaming();
                        break;

                    case 7:
                        _obsLinker.StopStreaming();
                        break;

                    case 8:
                        _obsLinker.ToggleRecording();
                        break;

                    case 9:
                        _obsLinker.StartRecording();
                        break;

                    case 10:
                        _obsLinker.PauseRecording();
                        break;

                    case 11:
                        _obsLinker.ResumeRecording();
                        break;

                    case 12:
                        _obsLinker.StopRecording();
                        break;

                    case 13:
                        if (int.TryParse(message.MidiNote, out int midiNote))
                        {
                            _midiController.ForwardMidiNote(midiNote);
                        }

                        break;

                    case 14:
                        _musicPlayer.Pause();
                        break;

                    case 15:
                        _musicPlayer.PlayNextSong();
                        break;

                    case 16:
                        try
                        {
                            StreamConfig streamConfig = ListStreamConfigs.Single(streamConfig => streamConfig.Name == observableAction.StreamConfig);

                            _twitchBot.EditStreamInformationsAsync(streamConfig.Category.Id, streamConfig.Title);
                        }
                        catch (Exception)
                        {
                            ToastHelper.Toast("Relation introuvable", $"La configuration \"{observableAction.StreamConfig}\" est introuvable");
                        }

                        break;
                }
            }
        }

        private void Actions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scenes.Visibility = Visibility.Visible;
            SceneItems.Visibility = Visibility.Visible;
            StreamConfigs.Visibility = Visibility.Hidden;

            switch (Actions.SelectedIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                    _obsLinker.LoadScenes();
                    Scenes.IsEnabled = true;
                    break;
                case 16:
                    Scenes.Visibility = Visibility.Hidden;
                    SceneItems.Visibility = Visibility.Hidden;
                    StreamConfigs.Visibility = Visibility.Visible;
                    break;
                default:
                    Scenes.SelectedItem = null;
                    Scenes.IsEnabled = false;
                    break;
            }

            AddActionButton.IsEnabled = ValidateActionForm();
        }

        private void Scenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Scenes.SelectedItem != null)
            {
                switch (Actions.SelectedIndex)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        _obsLinker.LoadScenesItems((ObservableScene)Scenes.SelectedItem);
                        SceneItems.IsEnabled = true;
                        break;
                    default:
                        SceneItems.SelectedItem = null;
                        SceneItems.IsEnabled = false;
                        break;
                }
            }
            else
            {
                SceneItems.SelectedItem = null;
                SceneItems.IsEnabled = false;
            }

            AddActionButton.IsEnabled = ValidateActionForm();
        }

        private void SceneItems_StreamConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddActionButton.IsEnabled = ValidateActionForm();
        }

        private bool ValidateActionForm()
        {
            if (Actions.SelectedIndex > -1)
            {
                switch (Actions.SelectedIndex)
                {
                    case 0:
                           return Scenes.SelectedItem != null;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return Scenes.SelectedItem != null && SceneItems.SelectedItem != null;
                    case 16:
                        return StreamConfigs.SelectedItem != null;
                    default:
                        return true;
                }
            }

            return false;
        }

        private void AddAction(object sender, RoutedEventArgs e)
        {
            if (!ValidateActionForm())
            {
                return;
            }

            string scene = null;
            string sceneItem = null;
            StreamConfig streamConfig = null;

            if (Scenes.SelectedItem != null)
            {
                OBSScene sceneObject = ((ObservableScene)Scenes.SelectedItem).OBSScene;
                scene = sceneObject.Name;
            }

            if (SceneItems.SelectedItem != null)
            {
                SceneItem sceneItemObject = ((ObservableSceneItem)SceneItems.SelectedItem).SceneItem;
                sceneItem = sceneItemObject.SourceName;
            }

            if (StreamConfigs.SelectedItem != null)
            {
                streamConfig = (StreamConfig)StreamConfigs.SelectedItem;
            }

            ListMessageActions.Add(_midiController.GenerateAction(Actions.SelectedIndex, scene, sceneItem, streamConfig));

            Actions.SelectedItem = null;
            Scenes.SelectedItem = null;
            SceneItems.SelectedItem = null;
            StreamConfigs.SelectedItem = null;

            AddMessageButton.IsEnabled = ValidateMessageForm();
        }

        private void RemoveAction(object sender, RoutedEventArgs e)
        {
            if (MessageActions.SelectedIndex > -1)
            {
                ListMessageActions.RemoveAt(MessageActions.SelectedIndex);
            }

            AddMessageButton.IsEnabled = ValidateMessageForm();
        }

        private void MessageActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MessageActions.SelectedIndex > -1)
            {
                ObservableAction action = ListMessageActions[MessageActions.SelectedIndex];

                Actions.SelectedItem = action.Name;
                Scenes.Text = action.Scene;
                SceneItems.Text = action.SceneItem;
                StreamConfigs.Text = action.StreamConfig?.ToString();

                RemoveActionButton.IsEnabled = true;
            }
            else
            {
                RemoveActionButton.IsEnabled = false;
            }
        }

        private void MidiNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddMessageButton.IsEnabled = ValidateMessageForm();
        }

        private bool ValidateMessageForm()
        {
            return int.TryParse(MidiNote.Text, out _) && ListMessageActions.Count > 0;
        }

        private void AddMessage(object sender, RoutedEventArgs e)
        {
            if (!ValidateMessageForm())
            {
                return;
            }

            _midiController.AddAction(MidiNote.Text, ListMessageActions);

            MidiNote.Text = null;
            Actions.SelectedItem = null;
            Scenes.SelectedItem = null;
            SceneItems.SelectedItem = null;
            StreamConfigs.SelectedItem = null;

            ListMessageActions.Clear();
        }

        private void RemoveMessage(object sender, RoutedEventArgs e)
        {
            if (ListViewActions.SelectedIndex > -1)
            {
                _midiController.RemoveActionAt(ListViewActions.SelectedIndex);
            }
        }

        private void ListActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewActions.SelectedIndex > -1)
            {
                Message message = _midiController.GetActionAt(ListViewActions.SelectedIndex);

                MidiNote.Text = message.MidiNote;
                Actions.SelectedItem = null;
                Scenes.SelectedItem = null;
                SceneItems.SelectedItem = null;
                StreamConfigs.SelectedItem = null;

                ListMessageActions.Clear();

                foreach (ObservableAction observableAction in message.Actions)
                {
                    ListMessageActions.Add(observableAction);
                }

                AddMessageButton.IsEnabled = ValidateMessageForm();
                RemoveMessageButton.IsEnabled = true;
            }
            else
            {
                RemoveMessageButton.IsEnabled = false;
            }
        }
        #endregion

        #region Stream Settings
        public ObservableCollection<StreamConfig> ListStreamConfigs { get; set; } = new ObservableCollection<StreamConfig>();

        private async Task AutoComplet_UpdateCategories()
        {
            List<Category> games = await _twitchBot.SearchCategoriesAsync(StreamCategory.Text);

            StreamCategory.AutoSuggestionList.Clear();

            foreach (Category game in games)
            {
                StreamCategory.AutoSuggestionList.Add(game);
            }
        }

        private void StreamCategory_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = AutoComplet_UpdateCategories();

            AddStreamButton.IsEnabled = ValidateStreamConfigForm();
        }

        private void StreamCategory_SelectionChanged(object sender, EventArgs e)
        {
            AddStreamButton.IsEnabled = ValidateStreamConfigForm();
        }

        private void StreamName_StreamTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddStreamButton.IsEnabled = ValidateStreamConfigForm();
        }

        private bool ValidateStreamConfigForm()
        {
            return !string.IsNullOrEmpty(StreamName.Text) && !string.IsNullOrEmpty(StreamTitle.Text) && StreamCategory.SelectedItem != null;
        }

        private void AddStreamConfig(object sender, RoutedEventArgs e)
        {
            if (!ValidateStreamConfigForm())
            {
                return;
            }

            foreach (StreamConfig streamConfig in ListStreamConfigs)
            {
                if (streamConfig.Name == StreamName.Text)
                {
                    ListStreamConfigs.Remove(streamConfig);
                    break;
                }
            }

            Category observableCategory = (Category)StreamCategory.SelectedItem;
            ListStreamConfigs.Add(new StreamConfig() { Name = StreamName.Text, Title = StreamTitle.Text, Category = observableCategory });

            StreamName.Text = null;
            StreamTitle.Text = null;
            StreamCategory.Text = null;
            StreamCategory.SelectedItem = null;
        }

        private void RemoveStreamConfig(object sender, RoutedEventArgs e)
        {
            if (ListViewStreamConfigs.SelectedIndex > -1)
            {
                ListStreamConfigs.RemoveAt(ListViewStreamConfigs.SelectedIndex);
            }
        }

        private void ListStreamConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewStreamConfigs.SelectedIndex > -1)
            {
                StreamConfig streamConfig = ListStreamConfigs[ListViewStreamConfigs.SelectedIndex];

                StreamName.Text = streamConfig.Name;
                StreamTitle.Text = streamConfig.Title;
                StreamCategory.SelectedItem = streamConfig.Category;

                RemoveStreamButton.IsEnabled = true;
                SetConfig.IsEnabled = true;
            }
            else
            {
                RemoveStreamButton.IsEnabled = false;
                SetConfig.IsEnabled = false;
            }
        }

        private void SetSetting_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewStreamConfigs.SelectedIndex > -1)
            {
                StreamConfig streamConfig = ListStreamConfigs[ListViewStreamConfigs.SelectedIndex];

                _twitchBot.EditStreamInformationsAsync(streamConfig.Category.Id, streamConfig.Title);

                CurrentConfig.Text = streamConfig.ToString();
            }
        }
        #endregion

        #region Twitch Bot
        public ObservableCollection<Command> ListCommands => _twitchBot?.ListCommands;

        public ObservableCollection<string> ListPossibleCommandActions => _twitchBot?.ListPossibleCommandActions;

        public SolidColorBrush TwitchBot_ClientStateBrush => _twitchBot?.ClientStateBrush;

        public SolidColorBrush TwitchBot_FollowerServiceStateBrush => _twitchBot?.FollowerServiceStateBrush;

        public SolidColorBrush TwitchBot_SubServiceState => _twitchBot?.SubServiceState;

        private void TwitchBot_NewCommand(object sender, Command command)
        {
            int midiNote;
            switch (_twitchBot.GetCommandIndex(command.Action))
            {
                case 1:
                    if (int.TryParse(command.BotNote, out midiNote))
                    {
                        _midiController.UpMidiNote(midiNote);
                    }

                    break;
                case 2:
                    if (int.TryParse(command.BotNote, out midiNote))
                    {
                        _midiController.ForwardMidiNote(midiNote);
                    }

                    break;
                default:
                    try
                    {
                        Resource resource = ListResources.Single(resource => resource.Name == command.Resource);

                        _twitchBot.Client_SendMessage(_messageTemplating.RenderMessage(resource.Value));
                    }
                    catch (Exception)
                    {
                        ToastHelper.Toast("Relation introuvable", $"La ressource \"{command.Resource}\" est introuvable");
                    }

                    break;
            }
        }

        private void CommandName_CommandNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddCommandButton.IsEnabled = ValidateCommandForm();
        }

        private void CommandActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CommandResource.SelectedItem = null;
            CommandNote.Text = null;

            switch (CommandActions.SelectedIndex)
            {
                case 1:
                case 2:
                    CommandResource.IsEnabled = false;
                    CommandResource.Visibility = Visibility.Hidden;
                    CommandNote.Visibility = Visibility.Visible;
                    break;
                default:
                    CommandResource.IsEnabled = true;
                    CommandResource.Visibility = Visibility.Visible;
                    CommandNote.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void CommandResource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddCommandButton.IsEnabled = ValidateCommandForm();
        }

        private bool ValidateCommandForm()
        {
            if (!string.IsNullOrEmpty(CommandName.Text) && CommandActions.SelectedIndex > -1)
            {
                switch (CommandActions.SelectedIndex)
                {
                    case 1:
                    case 2:
                        if (int.TryParse(CommandNote.Text, out _))
                        {
                            return true;
                        }
                        break;
                    default:
                        if (CommandResource.SelectedIndex > -1)
                        {
                             return true;
                        }
                        break;
                }
            }

            return false;
        }

        private void AddCommand(object sender, RoutedEventArgs e)
        {
            if (!ValidateCommandForm())
            {
                return;
            }

            string botNote = null;
            string resource = null;

            switch (CommandActions.SelectedIndex)
            {
                case 1:
                case 2:
                    botNote = CommandNote.Text;
                    break;
                default:
                    resource = ListResources[CommandResource.SelectedIndex].Name;
                    break;
            }

            _twitchBot.AddCommand(CommandName.Text, CommandActions.SelectedIndex, botNote, resource);

            CommandName.Text = null;
            CommandActions.SelectedItem = null;
            CommandResource.SelectedItem = null;
            CommandNote.Text = null;
        }

        private void RemoveCommand(object sender, RoutedEventArgs e)
        {
            if (ListViewCommands.SelectedIndex > -1)
            {
                _twitchBot.RemoveCommandAt(ListViewCommands.SelectedIndex);
            }
        }

        private void ListCommands_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewCommands.SelectedIndex > -1)
            {
                Command command = _twitchBot.GetCommandAt(ListViewCommands.SelectedIndex);

                CommandName.Text = command.CommandName;
                CommandActions.SelectedItem = command.Action;
                CommandResource.Text = command.Resource;
                CommandNote.Text = command.BotNote;

                RemoveCommandButton.IsEnabled = true;
            }
            else
            {
                RemoveCommandButton.IsEnabled = false;
            }
        }
        #endregion

        #region Resources
        public ObservableCollection<Resource> ListResources { get; set; } = new ObservableCollection<Resource>();

        private void ResourceName_ResourceValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddResourceButton.IsEnabled = ValidateResourceForm();
        }

        private bool ValidateResourceForm()
        {
            return !string.IsNullOrEmpty(ResourceName.Text) && !string.IsNullOrEmpty(ResourceValue.Text);
        }

        private void AddResource(object sender, RoutedEventArgs e)
        {
            if (!ValidateResourceForm())
            {
                return;
            }

            foreach (Resource resource in ListResources)
            {
                if (resource.Name == ResourceName.Text)
                {
                    ListResources.Remove(resource);
                    break;
                }
            }

            ListResources.Add(new Resource() { Name = ResourceName.Text, Value = ResourceValue.Text });

            ResourceName.Text = null;
            ResourceValue.Text = null;
        }

        private void RemoveResource(object sender, RoutedEventArgs e)
        {
            if (ListViewResources.SelectedIndex > -1)
            {
                ListResources.RemoveAt(ListViewResources.SelectedIndex);
            }
        }

        private void ListResources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewResources.SelectedIndex > -1)
            {
                ResourceName.Text = ListResources[ListViewResources.SelectedIndex].Name;
                ResourceValue.Text = ListResources[ListViewResources.SelectedIndex].Value;

                RemoveResourceButton.IsEnabled = true;
            }
            else
            {
                RemoveResourceButton.IsEnabled = false;
            }
        }
        #endregion

        #region Playlists
        public ObservableCollection<Playlist> ListPlaylists { get; set; } = new ObservableCollection<Playlist>();

        private void MusicPlayer_NewSongPlaying(object sender, string currentSong)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                CurrentSong.Text = currentSong;
            }));
        }

        private void PlaylistName_PlaylistDossier_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddPlaylistButton.IsEnabled = ValidatePlaylistForm();
        }

        private bool ValidatePlaylistForm()
        {
            return !string.IsNullOrEmpty(PlaylistName.Text) && !string.IsNullOrEmpty(PlaylistDossier.Text);
        }

        private void AddPlaylist(object sender, RoutedEventArgs e)
        {
            if (!ValidatePlaylistForm())
            {
                return;
            }

            foreach (Playlist playlist in ListPlaylists)
            {
                if (playlist.Name == PlaylistName.Text || playlist.Dossier == PlaylistDossier.Text)
                {
                    ListPlaylists.Remove(playlist);
                    break;
                }
            }

            ListPlaylists.Add(new Playlist() { Name = PlaylistName.Text, Dossier = PlaylistDossier.Text });

            PlaylistName.Text = null;
            PlaylistDossier.Text = null;
        }

        private void RemovePlaylist(object sender, RoutedEventArgs e)
        {
            if (ListViewPlaylists.SelectedIndex > -1)
            {
                ListPlaylists.RemoveAt(ListViewPlaylists.SelectedIndex);
            }
        }

        private void ListPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewPlaylists.SelectedIndex > -1)
            {
                PlaylistName.Text = ListPlaylists[ListViewPlaylists.SelectedIndex].Name;
                PlaylistDossier.Text = ListPlaylists[ListViewPlaylists.SelectedIndex].Dossier;

                RemovePlaylistButton.IsEnabled = true;
                StartPlaylist.IsEnabled = true;
            }
            else
            {
                RemovePlaylistButton.IsEnabled = false;
                StartPlaylist.IsEnabled = false;
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.ShowDialog();

            PlaylistDossier.Text = folderBrowserDialog.SelectedPath;
        }

        private void StartPlaylist_Click(object sender, RoutedEventArgs e)
        {
            this._musicPlayer.StartFolder(PlaylistDossier.Text);

            StartPlaylist.IsEnabled = false;
            PausePlaylist.IsEnabled = true;
            StopPlaylist.IsEnabled = true;
        }

        private void PausePlaylist_Click(object sender, RoutedEventArgs e)
        {
            _musicPlayer.Pause();
        }

        private void StopPlaylist_Click(object sender, RoutedEventArgs e)
        {
            _musicPlayer.Stop();

            StartPlaylist.IsEnabled = true;
            PausePlaylist.IsEnabled = false;
            StopPlaylist.IsEnabled = false;
        }
        #endregion
    }
}
