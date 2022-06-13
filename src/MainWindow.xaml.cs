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

        public ObservableCollection<Message> ListActions => _midiController?.ListActions;

        public ObservableCollection<string> ListPossibleActions => _midiController?.ListPossibleActions;

        public SolidColorBrush ObsLinker_StateBrush => _obsLinker?.StateBrush;

        public ObservableCollection<ObservableScene> ListScenes => _obsLinker?.ListScenes;

        public ObservableCollection<ObservableSceneItem> ListSceneItems => _obsLinker?.ListSceneItems;

        public SolidColorBrush TwitchBot_ClientStateBrush => _twitchBot?.ClientStateBrush;

        public SolidColorBrush TwitchBot_FollowerServiceStateBrush => _twitchBot?.FollowerServiceStateBrush;

        public SolidColorBrush TwitchBot_SubServiceState => _twitchBot?.SubServiceState;

        public ObservableCollection<Command> ListCommands => _twitchBot?.ListCommands;

        public ObservableCollection<string> ListPossibleCommandActions => _twitchBot?.ListPossibleCommandActions;

        public SolidColorBrush LiveManager_StateBrush => _liveManager?.StateBrush;

        public ObservableCollection<ObservableAction> ListMessageActions { get; set; } = new ObservableCollection<ObservableAction>();

        public ObservableCollection<StreamConfig> ListStreamConfigs { get; set; } = new ObservableCollection<StreamConfig>();

        public ObservableCollection<Resource> ListResources { get; set; } = new ObservableCollection<Resource>();

        public ObservableCollection<Playlist> ListPlaylists { get; set; } = new ObservableCollection<Playlist>();

        public MainWindow()
        {
            InitializeComponent();

            _httpClient = new HttpClient();

            _configReader = new ConfigReader();

            _obsLinker = new OBSLinker();
            _liveManager = new LiveManager(_httpClient);
            _musicPlayer = new MusicPlayer(_liveManager);
            _messageTemplating = new MessageTemplating(_musicPlayer);

            _midiController = new MidiController(_httpClient);
            _midiController.NewMidiNoteReceived += MidiController_NewMidiNote;
            _midiController.NewMessageRaised += MidiController_NewMessage;

            _twitchBot = new TwitchBot(_liveManager);
            _twitchBot.NewCommandRaised += TwitchBot_NewCommand;

            _configReader.readConfigFiles(_midiController, _twitchBot, this);

            _twitchBot.Connect();
        }

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

                        _twitchBot.Client_SendMessage(_messageTemplating.renderMessage(resource.Value));
                    }
                    catch (Exception)
                    {
                        ToastHelper.Toast("Relation introuvable", $"La ressource \"{command.Resource}\" est introuvable");
                    }

                    break;
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
        }

        private void CommandActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CommandActions.SelectedIndex)
            {
                case 1:
                case 2:
                    CommandResource.Visibility = Visibility.Hidden;
                    CommandNote.Visibility = Visibility.Visible;
                    break;
                default:
                    CommandResource.Visibility = Visibility.Visible;
                    CommandNote.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void Scenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Scenes.SelectedItem != null)
            {
                _obsLinker.LoadScenesItems((ObservableScene)Scenes.SelectedItem);
                SceneItems.IsEnabled = true;
            }
            else
            {
                SceneItems.SelectedItem = null;
                SceneItems.IsEnabled = false;
            }
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            _configReader.updateConfigFiles(_midiController, _twitchBot, this);
        }

        private void AddAction(object sender, RoutedEventArgs e)
        {
            if (Actions.SelectedIndex > -1)
            {
                string scene = null;
                string sceneItem = null;
                StreamConfig streamConfig = null;

                switch (Actions.SelectedIndex)
                {
                    case 0:
                        if (Scenes.SelectedItem != null)
                        {
                            OBSScene sceneObject = ((ObservableScene) Scenes.SelectedItem).OBSScene;
                            scene = sceneObject.Name;
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        if (Scenes.SelectedItem != null && SceneItems.SelectedItem != null)
                        {
                            OBSScene sceneObject = ((ObservableScene)Scenes.SelectedItem).OBSScene;
                            scene = sceneObject.Name;

                            SceneItem sceneItemObject = ((ObservableSceneItem)SceneItems.SelectedItem).SceneItem;
                            sceneItem = sceneItemObject.SourceName;
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case 16:
                        if (StreamConfigs.SelectedItem != null)
                        {
                            streamConfig = (StreamConfig) StreamConfigs.SelectedItem;
                        }
                        else
                        {
                            return;
                        }
                        break;
                }

                ListMessageActions.Add(_midiController.GenerateAction(Actions.SelectedIndex, scene, sceneItem, streamConfig));

                Actions.SelectedItem = null;
                Scenes.SelectedItem = null;
                SceneItems.SelectedItem = null;
                StreamConfigs.SelectedItem = null;
            }
        }

        private void RemoveAction(object sender, RoutedEventArgs e)
        {
            if (MessageActions.SelectedIndex > -1)
            {
                ListMessageActions.RemoveAt(MessageActions.SelectedIndex);
            }
        }

        private void AddMessage(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MidiNote.Text, out _) && ListMessageActions.Count > 0)
            {
                _midiController.AddAction(MidiNote.Text, ListMessageActions);

                MidiNote.Text = null;
                Actions.SelectedItem = null;
                Scenes.SelectedItem = null;
                SceneItems.SelectedItem = null;
                StreamConfigs.SelectedItem = null;

                ListMessageActions.Clear();
            }
        }

        private void RemoveMessage(object sender, RoutedEventArgs e)
        {
            if (ListViewActions.SelectedIndex > -1)
            {
                _midiController.RemoveActionAt(ListViewActions.SelectedIndex);
            }
        }

        private void AddStreamConfig(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(StreamName.Text) && !string.IsNullOrEmpty(StreamTitle.Text) && StreamCategory.SelectedItem != null)
            {
                foreach (StreamConfig streamConfig in ListStreamConfigs)
                {
                    if (streamConfig.Name == StreamName.Text)
                    {
                        ListStreamConfigs.Remove(streamConfig);
                        break;
                    }
                }

                Category observableCategory = (Category) StreamCategory.SelectedItem;
                ListStreamConfigs.Add(new StreamConfig() { Name = StreamName.Text, Title = StreamTitle.Text, Category = observableCategory });

                StreamName.Text = null;
                StreamTitle.Text = null;
                StreamCategory.Text = null;
                StreamCategory.SelectedItem = null;
            }
        }

        private void RemoveStreamConfig(object sender, RoutedEventArgs e)
        {
            if (ListViewStreamConfigs.SelectedIndex > -1)
            {
                ListStreamConfigs.RemoveAt(ListViewStreamConfigs.SelectedIndex);
            }
        }

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
        }

        private void AddCommand(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CommandName.Text) && CommandActions.SelectedIndex > -1)
            {
                string botNote = null;
                string resource = null;

                switch (CommandActions.SelectedIndex)
                {
                    case 1:
                    case 2:
                        if (!string.IsNullOrEmpty(CommandNote.Text))
                        {
                            botNote = CommandNote.Text;
                        }
                        else
                        {
                            return;
                        }
                        break;
                    default:
                        if (CommandResource.SelectedIndex > -1)
                        {
                            resource = ListResources[CommandResource.SelectedIndex].Name;
                        }
                        else
                        {
                            return;
                        }
                        break;
                }

                _twitchBot.AddCommand(CommandName.Text, CommandActions.SelectedIndex, botNote, resource);

                CommandName.Text = null;
                CommandActions.SelectedItem = null;
                CommandResource.SelectedItem = null;
                CommandNote.Text = null;
            }
        }

        private void RemoveCommand(object sender, RoutedEventArgs e)
        {
            if (ListViewCommands.SelectedIndex > -1)
            {
                _twitchBot.RemoveCommandAt(ListViewCommands.SelectedIndex);
            }
        }

        private void AddResource(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ResourceName.Text) && !string.IsNullOrEmpty(ResourceValue.Text))
            {
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
        }

        private void RemoveResource(object sender, RoutedEventArgs e)
        {
            if (ListViewResources.SelectedIndex > -1)
            {
                ListResources.RemoveAt(ListViewResources.SelectedIndex);
            }
        }

        private void AddPlaylist(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(PlaylistName.Text) && !string.IsNullOrEmpty(PlaylistDossier.Text))
            {
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
        }

        private void RemovePlaylist(object sender, RoutedEventArgs e)
        {
            if (ListViewPlaylists.SelectedIndex > -1)
            {
                ListPlaylists.RemoveAt(ListViewPlaylists.SelectedIndex);
            }
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
                
                StreamCategory.Text = StreamCategory.SelectedItem.ToString();

                SetConfig.IsEnabled = true;
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
            }
        }

        private void ListResources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewResources.SelectedIndex > -1)
            {
                ResourceName.Text = ListResources[ListViewResources.SelectedIndex].Name;
                ResourceValue.Text = ListResources[ListViewResources.SelectedIndex].Value;
            }
        }

        private void ListPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewPlaylists.SelectedIndex > -1)
            {
                PlaylistName.Text = ListPlaylists[ListViewPlaylists.SelectedIndex].Name;
                PlaylistDossier.Text = ListPlaylists[ListViewPlaylists.SelectedIndex].Dossier;

                StartPlaylist.IsEnabled = true;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _musicPlayer.Stop();
        }
    }
}
