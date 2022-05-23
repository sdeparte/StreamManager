using OBSWebsocketDotNet.Types;
using Ookii.Dialogs.Wpf;
using StreamManager.Model;
using StreamManager.Services;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpClient httpClient;

        OBSLinker obsLinker;
        TwitchBot twitchBot;
        LiveManager liveManager;
        MidiController midiController;
        ConfigReader configReader;
        MusicPlayer musicPlayer;

        private ObservableCollection<Message> listActions = new ObservableCollection<Message>();
        private ObservableCollection<Command> listCommands = new ObservableCollection<Command>();
        private ObservableCollection<Resource> listResources = new ObservableCollection<Resource>();
        private ObservableCollection<Playlist> listPlaylists = new ObservableCollection<Playlist>();

        public MainWindow()
        {
            InitializeComponent();

            httpClient = new HttpClient();

            configReader = new ConfigReader();

            obsLinker = new OBSLinker(this);
            twitchBot = new TwitchBot(this);

            liveManager = new LiveManager(this, httpClient);

            midiController = new MidiController(this, httpClient);
            configReader.readConfigFiles(this);

            musicPlayer = new MusicPlayer(this);

            UpdateResourcesComboBox();

            ListActions.ItemsSource = listActions;
            ListCommands.ItemsSource = listCommands;
            ListResources.ItemsSource = listResources;
            ListPlaylists.ItemsSource = listPlaylists;
        }

        public OBSLinker Get_ObsLinker()
        {
            return obsLinker;
        }

        public TwitchBot Get_TwitchBot()
        {
            return twitchBot;
        }

        public LiveManager Get_LiveManager()
        {
            return liveManager;
        }

        public MidiController Get_MidiController()
        {
            return midiController;
        }

        public ObservableCollection<Message> Get_ListActions()
        {
            return listActions;
        }

        public ObservableCollection<Command> Get_ListCommands()
        {
            return listCommands;
        }

        public ObservableCollection<Resource> Get_ListResources()
        {
            return listResources;
        }

        public ObservableCollection<Playlist> Get_ListPlaylists()
        {
            return listPlaylists;
        }

        public void Set_ListActions(ObservableCollection<Message> listActions)
        {
            this.listActions = listActions;
        }

        public void Set_ListCommands(ObservableCollection<Command> listCommands)
        {
            this.listCommands = listCommands;
        }

        public void Set_ListResources(ObservableCollection<Resource> listResources)
        {
            this.listResources = listResources;
        }

        public void Set_ListPlaylists(ObservableCollection<Playlist> listPlaylists)
        {
            this.listPlaylists = listPlaylists;
        }

        private void Actions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (Actions.SelectedIndex)
            {
                case 0:
                    Scenes.IsEnabled = true;
                    SceneItems.IsEnabled = false;
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    Scenes.IsEnabled = true;
                    SceneItems.IsEnabled = true;
                    break;
                default:
                    Scenes.IsEnabled = false;
                    SceneItems.IsEnabled = false;
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
            SceneItems.Items.Clear();

            foreach (ComboBoxItem item in obsLinker.LoadOBSSceneItems((ComboBoxItem)Scenes.SelectedItem))
            {
                SceneItems.Items.Add(item);
            }
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            configReader.updateConfigFiles(this);
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
                        if (Scenes.SelectedIndex > -1 && ((ComboBoxItem)Scenes.SelectedItem).Tag is OBSScene)
                        {
                            OBSScene sceneObject = (OBSScene)((ComboBoxItem)Scenes.SelectedItem).Tag;
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
                        if (SceneItems.SelectedIndex > -1 && ((ComboBoxItem)SceneItems.SelectedItem).Tag is SceneItem)
                        {
                            OBSScene sceneObject = (OBSScene)((ComboBoxItem)Scenes.SelectedItem).Tag;
                            scene = sceneObject.Name;

                            SceneItem sceneItemObject = (SceneItem)((ComboBoxItem)SceneItems.SelectedItem).Tag;
                            sceneItem = sceneItemObject.SourceName;
                        }
                        else
                        {
                            return;
                        }
                        break;
                }

                foreach (Message message in listActions)
                {
                    if (message.MidiNote == MidiNote.Text)
                    {
                        listActions.Remove(message);
                        break;
                    }
                }

                listActions.Add(new Message() { MidiNote = MidiNote.Text, Action = midiController.Get_Actions()[Actions.SelectedIndex], Scene = scene, SceneItem = sceneItem });

                MidiNote.Text = "";
                Actions.Text = "";
                Scenes.Text = "";
                SceneItems.Text = "";
            }
        }

        private void RemoveAction(object sender, RoutedEventArgs e)
        {
            if (ListActions.SelectedIndex > -1)
            {
                listActions.RemoveAt(ListActions.SelectedIndex);
            }
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

                foreach (Command command in listCommands)
                {
                    if (command.CommandName == CommandName.Text)
                    {
                        listCommands.Remove(command);
                        break;
                    }
                }

                listCommands.Add(new Command() { CommandName = CommandName.Text, Action = twitchBot.Get_CommandActions()[CommandActions.SelectedIndex], BotAnswer = botAnswer, BotNote = botNote });

                CommandName.Text = "";
                CommandActions.Text = twitchBot.Get_CommandActions()[0];
                BotAnswer.Text = "";
                BotNote.Text = "";
            }
        }

        private void RemoveCommand(object sender, RoutedEventArgs e)
        {
            if (ListCommands.SelectedIndex > -1)
            {
                listCommands.RemoveAt(ListCommands.SelectedIndex);
            }
        }

        private void AddResource(object sender, RoutedEventArgs e)
        {
            if (Value.Text != "")
            {
                foreach (Resource resource in listResources)
                {
                    if (resource.Value == Value.Text)
                    {
                        return;
                    }
                }

                listResources.Add(new Resource() { Value = Value.Text });

                UpdateResourcesComboBox();

                Value.Text = "";
            }
        }

        private void RemoveResource(object sender, RoutedEventArgs e)
        {
            if (ListResources.SelectedIndex > -1)
            {
                listResources.RemoveAt(ListResources.SelectedIndex);

                UpdateResourcesComboBox();
            }
        }

        private void UpdateResourcesComboBox()
        {
            BotAnswer.Items.Clear();

            foreach (Resource resource in listResources)
            {
                BotAnswer.Items.Add(resource.Value);
            }
        }

        private void AddPlaylist(object sender, RoutedEventArgs e)
        {
            if (Dossier.Text != "")
            {
                foreach (Playlist playlist in listPlaylists)
                {
                    if (playlist.Dossier == Dossier.Text)
                    {
                        return;
                    }
                }

                listPlaylists.Add(new Playlist() { Dossier = Dossier.Text });

                Dossier.Text = "";
            }
        }

        private void RemovePlaylist(object sender, RoutedEventArgs e)
        {
            if (ListPlaylists.SelectedIndex > -1)
            {
                listPlaylists.RemoveAt(ListPlaylists.SelectedIndex);
            }
        }

        private void ListActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListActions.SelectedIndex > -1)
            {
                MidiNote.Text = listActions[ListActions.SelectedIndex].MidiNote;
                Actions.Text = listActions[ListActions.SelectedIndex].Action;
                Scenes.Text = listActions[ListActions.SelectedIndex].Scene;
                SceneItems.Text = listActions[ListActions.SelectedIndex].SceneItem;
            }
        }

        private void ListCommands_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListCommands.SelectedIndex > -1)
            {
                CommandName.Text = listCommands[ListCommands.SelectedIndex].CommandName;
                CommandActions.Text = listCommands[ListCommands.SelectedIndex].Action;
                BotAnswer.Text = listCommands[ListCommands.SelectedIndex].BotAnswer;
                BotNote.Text = listCommands[ListCommands.SelectedIndex].BotNote;
            }
        }

        private void ListResources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListResources.SelectedIndex > -1)
            {
                Value.Text = listResources[ListResources.SelectedIndex].Value;
            }
        }

        private void ListPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListPlaylists.SelectedIndex > -1)
            {
                Dossier.Text = listPlaylists[ListPlaylists.SelectedIndex].Dossier;

                StartPlaylist.IsEnabled = true;
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
            this.musicPlayer.StartFolder(Dossier.Text);

            StartPlaylist.IsEnabled = false;
            PausePlaylist.IsEnabled = true;
            StopPlaylist.IsEnabled = true;
        }

        private void PausePlaylist_Click(object sender, RoutedEventArgs e)
        {
            this.musicPlayer.Pause();
        }

        private void StopPlaylist_Click(object sender, RoutedEventArgs e)
        {
            this.musicPlayer.Stop();

            StartPlaylist.IsEnabled = true;
            PausePlaylist.IsEnabled = false;
            StopPlaylist.IsEnabled = false;
        }
    }
}
