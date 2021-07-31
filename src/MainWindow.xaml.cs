using OBSWebsocketDotNet.Types;
using StreamManager.Model;
using StreamManager.Services;
using System;
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
        MidiController midiController;
        OBSLinker obsLinker;
        TwitchBot twitchBot;
        ConfigReader configReader;

        private ObservableCollection<Message> listActions = new ObservableCollection<Message>();
        private ObservableCollection<Command> listCommands = new ObservableCollection<Command>();
        private ObservableCollection<Resource> listResources = new ObservableCollection<Resource>();

        public MainWindow()
        {
            InitializeComponent();

            configReader = new ConfigReader();

            obsLinker = new OBSLinker(this);
            twitchBot = new TwitchBot(this);

            midiController = new MidiController(this);
            configReader.readConfigFiles(this);

            UpdateResourcesComboBox();

            ListActions.ItemsSource = listActions;
            ListCommands.ItemsSource = listCommands;
            ListResources.ItemsSource = listResources;
        }

        public async void sendMercureMessage(string username)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("http://172.19.137.32:8000/api/subscribe?username=" + username);
        }

        public MidiController Get_MidiController()
        {
            return midiController;
        }

        public OBSLinker Get_ObsLinker()
        {
            return obsLinker;
        }

        public TwitchBot Get_TwitchBot()
        {
            return twitchBot;
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

        private void Actions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (Actions.SelectedIndex)
            {
                case 0:
                    Scenes.IsEnabled = true;
                    SceneItems.IsEnabled = false;
                    break;
                case 1:
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

                listCommands.Add(new Command() { CommandName = CommandName.Text, Action = midiController.Get_CommandActions()[CommandActions.SelectedIndex], BotAnswer = botAnswer, BotNote = botNote });

                CommandName.Text = "";
                CommandActions.Text = midiController.Get_CommandActions()[0];
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
    }
}
