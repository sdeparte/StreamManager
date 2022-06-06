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

namespace StreamManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;

        public readonly OBSLinker _obsLinker;
        public readonly TwitchBot _twitchBot;
        public readonly LiveManager _liveManager;
        public readonly MidiController _midiController;
        public readonly ConfigReader _configReader;
        public readonly MusicPlayer _musicPlayer;
        public readonly MessageTemplating _messageTemplating;

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

            _midiController = new MidiController(_obsLinker, _musicPlayer, _httpClient);
            _midiController.NewMidiNoteReceived += MidiController_NewMidiNote;

            _twitchBot = new TwitchBot(_midiController, _messageTemplating, _liveManager);

            _configReader.readConfigFiles(_midiController, _twitchBot, this);


            UpdateResourcesComboBox();
        }

        private void MidiController_NewMidiNote(object sender, int midiNote)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                MidiNote.Text = midiNote.ToString();
            }));
        }

        private void Actions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
                    BotAnswer.Visibility = Visibility.Hidden;
                    BotNote.Visibility = Visibility.Visible;
                    break;
                default:
                    BotAnswer.Visibility = Visibility.Visible;
                    BotNote.Visibility = Visibility.Hidden;
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
            if (int.TryParse(MidiNote.Text, out _) && Actions.SelectedIndex > -1)
            {
                string scene = "-";
                string sceneItem = "-";

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
                            OBSScene sceneObject = ((ObservableScene) Scenes.SelectedItem).OBSScene;
                            scene = sceneObject.Name;

                            SceneItem sceneItemObject = ((ObservableSceneItem) SceneItems.SelectedItem).SceneItem;
                            sceneItem = sceneItemObject.SourceName;
                        }
                        else
                        {
                            return;
                        }
                        break;
                }

                _midiController.AddAction(MidiNote.Text, Actions.SelectedIndex, scene, sceneItem);

                MidiNote.Text = "";
                Actions.Text = "";
                Scenes.Text = "";
                SceneItems.Text = "";
            }
        }

        private void RemoveAction(object sender, RoutedEventArgs e)
        {
            if (ListViewActions.SelectedIndex > -1)
            {
                _midiController.RemoveActionAt(ListViewActions.SelectedIndex);
            }
        }

        private void AddStreamConfig(object sender, RoutedEventArgs e)
        {
            if (StreamTitle.Text != "" && StreamCategory.SelectedItem != null)
            {
                Category observableCategory = (Category) StreamCategory.SelectedItem;
                ListStreamConfigs.Add(new StreamConfig() { Title = StreamTitle.Text, Category = observableCategory });

                StreamTitle.Text = "";
                StreamCategory.Text = "";
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
            if (CommandName.Text != "" && CommandActions.SelectedIndex > -1)
            {
                string botNote = "-";
                string botAnswer = "-";

                switch (CommandActions.SelectedIndex)
                {
                    case 1:
                    case 2:
                        if (BotNote.Text != "")
                        {
                            botNote = BotNote.Text;
                        }
                        else
                        {
                            return;
                        }
                        break;
                    default:
                        if (BotAnswer.Text != "")
                        {
                            botAnswer = BotAnswer.Text;
                        }
                        else
                        {
                            return;
                        }
                        break;
                }

                _twitchBot.AddCommand(CommandName.Text, CommandActions.SelectedIndex, botAnswer, botNote);

                CommandName.Text = "";
                CommandActions.Text = "";
                BotAnswer.Text = "";
                BotNote.Text = "";
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
            if (Value.Text != "")
            {
                foreach (Resource resource in ListResources)
                {
                    if (resource.Value == Value.Text)
                    {
                        return;
                    }
                }

                ListResources.Add(new Resource() { Value = Value.Text });

                UpdateResourcesComboBox();

                Value.Text = "";
            }
        }

        private void RemoveResource(object sender, RoutedEventArgs e)
        {
            if (ListViewResources.SelectedIndex > -1)
            {
                ListResources.RemoveAt(ListViewResources.SelectedIndex);

                UpdateResourcesComboBox();
            }
        }

        private void UpdateResourcesComboBox()
        {
            BotAnswer.Items.Clear();

            foreach (Resource resource in ListResources)
            {
                BotAnswer.Items.Add(resource.Value);
            }
        }

        private void AddPlaylist(object sender, RoutedEventArgs e)
        {
            if (Dossier.Text != "")
            {
                foreach (Playlist playlist in ListPlaylists)
                {
                    if (playlist.Dossier == Dossier.Text)
                    {
                        return;
                    }
                }

                ListPlaylists.Add(new Playlist() { Dossier = Dossier.Text });

                Dossier.Text = "";
            }
        }

        private void RemovePlaylist(object sender, RoutedEventArgs e)
        {
            if (ListViewPlaylists.SelectedIndex > -1)
            {
                ListPlaylists.RemoveAt(ListViewPlaylists.SelectedIndex);
            }
        }

        private void ListActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewActions.SelectedIndex > -1)
            {
                Message action = _midiController.GetActionAt(ListViewActions.SelectedIndex);

                MidiNote.Text = action.MidiNote;
                Actions.Text = action.Action;
                Scenes.Text = action.Scene;
                SceneItems.Text = action.SceneItem;
            }
        }

        private void ListStreamConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewStreamConfigs.SelectedIndex > -1)
            {
                StreamConfig streamConfig = ListStreamConfigs[ListViewStreamConfigs.SelectedIndex];

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
                CommandActions.Text = command.Action;
                BotAnswer.Text = command.BotAnswer;
                BotNote.Text = command.BotNote;
            }
        }

        private void ListResources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewResources.SelectedIndex > -1)
            {
                Value.Text = ListResources[ListViewResources.SelectedIndex].Value;
            }
        }

        private void ListPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewPlaylists.SelectedIndex > -1)
            {
                Dossier.Text = ListPlaylists[ListViewPlaylists.SelectedIndex].Dossier;

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

            Dossier.Text = folderBrowserDialog.SelectedPath;
        }

        private void StartPlaylist_Click(object sender, RoutedEventArgs e)
        {
            this._musicPlayer.StartFolder(Dossier.Text);

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
