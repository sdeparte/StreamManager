using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace StreamManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Message>));
        private List<IMidiInputDevice> devices = new List<IMidiInputDevice>();
        private OBSWebsocket obs;
        private string[] actions = new string[4] { "Changer de scène", "Muter / Unmute un élément d'une scène", "Démarer / Arreter le stream", "Démarer / Arreter l'engregistrement" };

        ObservableCollection<Message> listActions = new ObservableCollection<Message>();
        ObservableCollection<Command> listCommands = new ObservableCollection<Command>();
        ObservableCollection<SentenceRessource> listRessources = new ObservableCollection<SentenceRessource>();

        public MainWindow()
        {
            InitializeComponent();

            TwitchBot twitchBot = new TwitchBot(this);

            obs = new OBSWebsocket();
            obs.Connected += onConnect;

            try
            {
                obs.Connect("ws://127.0.0.1:4444", "");
            }
            catch (AuthFailureException)
            {
                MessageBox.Show("Authentication failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            catch (ErrorResponseException ex)
            {
                MessageBox.Show("Connect failed : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            foreach (IMidiInputDeviceInfo device in MidiDeviceManager.Default.InputDevices)
            {
                IMidiInputDevice inputDevice = device.CreateDevice();
                inputDevice.NoteOn += NoteOnMessageHandler;
                inputDevice.Open();

                devices.Add(inputDevice);
            }

            foreach (IMidiOutputDeviceInfo device in MidiDeviceManager.Default.OutputDevices)
            {
                MidiOutputDevices.Items.Add(device.Name);
            }

            foreach (string action in actions)
            {
                Actions.Items.Add(action);
            }

            using (FileStream fs = new FileStream(@"config.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    listActions = serializer.Deserialize(fs) as ObservableCollection<Message>;
                }
                catch (Exception ex)
                {}
            }

            ListActions.ItemsSource = listActions;
            ListCommands.ItemsSource = listCommands;
            ListResources.ItemsSource = listRessources;
        }

        private void onConnect(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                OBSState.Fill = new SolidColorBrush(Colors.Green);

                Scenes.Items.Clear();

                foreach (OBSScene scene in obs.ListScenes())
                {
                    ComboBoxItem scenceNode = new ComboBoxItem();
                    scenceNode.Content = scene.Name;
                    scenceNode.Tag = scene;

                    Scenes.Items.Add(scenceNode);
                }
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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

                listActions.Add(new Message() { MidiNote = MidiNote.Text, Action = actions[Actions.SelectedIndex], Scene = scene, SceneItem = sceneItem });

                MidiNote.Text = "";
                Actions.Text = "";
                Scenes.Text = "";
                SceneItems.Text = "";
            }
        }

        void NoteOnMessageHandler(IMidiInputDevice sender, in NoteOnMessage msg)
        {
            RtMidi.Core.Enums.Key key = msg.Key;

            foreach (Message message in listActions)
            {
                int midiNote = -1;
                int.TryParse(message.MidiNote, out midiNote);

                if (midiNote == (int)key)
                {
                    switch(Array.IndexOf(actions, message.Action))
                    {
                        case 0:
                            obs.SetCurrentScene(message.Scene);
                            break;

                        case 1:
                            obs.SetMute(message.SceneItem, !obs.GetMute(message.SceneItem));
                            break;
                    }
                    break;
                }
            }

            Application.Current.Dispatcher.Invoke(new Action(() => {
                MidiNote.Text = ((int)key).ToString();

                /*if (MidiOutputDevices.SelectedIndex != -1)
                {
                    foreach (IMidiOutputDeviceInfo device in MidiDeviceManager.Default.OutputDevices)
                    {
                        if (device.Name == MidiOutputDevices.SelectedItem.ToString())
                        {
                            IMidiOutputDevice outputDevice = device.CreateDevice();
                            outputDevice.Open();

                            outputDevice.Send(new NoteOnMessage(Channel.Channel1, key, 127));
                            System.Threading.Thread.Sleep(250);
                            outputDevice.Send(new NoteOffMessage(Channel.Channel1, key, 127));

                            outputDevice.Close();
                        }
                    }
                }*/
            }));
        }

        private void Scenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SceneItems.Items.Clear();

            if (Scenes.SelectedIndex > -1 && ((ComboBoxItem) Scenes.SelectedItem).Tag is OBSScene)
            {
                OBSScene scene = (OBSScene) ((ComboBoxItem)Scenes.SelectedItem).Tag;

                foreach (SceneItem sceneItem in scene.Items)
                {
                    ComboBoxItem sceneItemNode = new ComboBoxItem();
                    sceneItemNode.Content = sceneItem.SourceName;
                    sceneItemNode.Tag = sceneItem;

                    SceneItems.Items.Add(sceneItemNode);
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (FileStream fs = new FileStream(@"config.xml", FileMode.OpenOrCreate))
            {
                serializer.Serialize(fs, listActions);
            }
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (Value.Text != "")
            {
                foreach (SentenceRessource ressource in listRessources)
                {
                    if (ressource.Value == Value.Text)
                    {
                        return;
                    }
                }

                listRessources.Add(new SentenceRessource() { Value = Value.Text});

                UpdateRessourcesComboBox();

                Value.Text = "";
            }
        }

        private void UpdateRessourcesComboBox()
        {
            BotAnswer.Items.Clear();

            foreach (SentenceRessource ressource in listRessources)
            {
                BotAnswer.Items.Add(ressource.Value);
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (ListActions.SelectedIndex > -1)
            {
                listActions.RemoveAt(ListActions.SelectedIndex);
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (ListCommands.SelectedIndex > -1)
            {
                listCommands.RemoveAt(ListCommands.SelectedIndex);
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if (ListResources.SelectedIndex > -1)
            {
                listRessources.RemoveAt(ListResources.SelectedIndex);

                UpdateRessourcesComboBox();
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
                Value.Text = listRessources[ListResources.SelectedIndex].Value;
            }
        }
    }
}
