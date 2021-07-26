using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using StreamManager.Model;
using StreamManager.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

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
            configReader.readConfigFiles(this);

            obsLinker = new OBSLinker(this);
            twitchBot = new TwitchBot(this);

            midiController = new MidiController(this);

            ListActions.ItemsSource = listActions;
            ListCommands.ItemsSource = listCommands;
            ListResources.ItemsSource = listResources;
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
            string scene = "-";
            string sceneItem = "-";

            if (int.TryParse(MidiNote.Text, out _) && Actions.SelectedIndex > -1)
            {
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
            if (CommandName.Text != "" && BotAnswer.Text != "")
            {
                foreach (Command command in listCommands)
                {
                    if (command.CommandName == CommandName.Text)
                    {
                        listCommands.Remove(command);
                        break;
                    }
                }

                listCommands.Add(new Command() { CommandName = CommandName.Text, BotAnswer = BotAnswer.Text });

                CommandName.Text = "";
                BotAnswer.Text = "";
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
                BotAnswer.Text = listCommands[ListCommands.SelectedIndex].BotAnswer;
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
