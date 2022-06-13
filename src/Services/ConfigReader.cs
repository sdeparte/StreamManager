using StreamManager.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace StreamManager.Services
{
    public class ConfigReader
    {
        private readonly XmlSerializer messageSerializer = new XmlSerializer(typeof(ObservableCollection<Message>));
        private readonly XmlSerializer streamConfigSerializer = new XmlSerializer(typeof(ObservableCollection<StreamConfig>));
        private readonly XmlSerializer commandeSerializer = new XmlSerializer(typeof(ObservableCollection<Command>));
        private readonly XmlSerializer resourceSerializer = new XmlSerializer(typeof(ObservableCollection<Resource>));
        private readonly XmlSerializer playlistSerializer = new XmlSerializer(typeof(ObservableCollection<Playlist>));

        public void ReadConfigFiles(MidiController midiController, TwitchBot twitchBot, MainWindow main)
        {
            using (FileStream fs = new FileStream(@"actions.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    midiController.ListActions = messageSerializer.Deserialize(fs) as ObservableCollection<Message>;
                }
                catch (Exception) { }
            }

            using (FileStream fs = new FileStream(@"streamConfis.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.ListStreamConfigs = streamConfigSerializer.Deserialize(fs) as ObservableCollection<StreamConfig>;
                }
                catch (Exception) { }
            }

            using (FileStream fs = new FileStream(@"commands.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    twitchBot.ListCommands = commandeSerializer.Deserialize(fs) as ObservableCollection<Command>;
                }
                catch (Exception) { }
            }

            using (FileStream fs = new FileStream(@"resources.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.ListResources = resourceSerializer.Deserialize(fs) as ObservableCollection<Resource>;
                }
                catch (Exception) { }
            }

            using (FileStream fs = new FileStream(@"playlists.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.ListPlaylists = playlistSerializer.Deserialize(fs) as ObservableCollection<Playlist>;
                }
                catch (Exception) { }
            }
        }

        public void UpdateConfigFiles(MidiController midiController, TwitchBot twitchBot, MainWindow main)
        {
            using (FileStream fs = new FileStream(@"actions.xml", FileMode.OpenOrCreate))
            {
                messageSerializer.Serialize(fs, midiController.ListActions);
            }

            using (FileStream fs = new FileStream(@"streamConfis.xml", FileMode.OpenOrCreate))
            {
                streamConfigSerializer.Serialize(fs, main.ListStreamConfigs);
            }

            using (FileStream fs = new FileStream(@"commands.xml", FileMode.OpenOrCreate))
            {
                commandeSerializer.Serialize(fs, twitchBot.ListCommands);
            }

            using (FileStream fs = new FileStream(@"resources.xml", FileMode.OpenOrCreate))
            {
                resourceSerializer.Serialize(fs, main.ListResources);
            }

            using (FileStream fs = new FileStream(@"playlists.xml", FileMode.OpenOrCreate))
            {
                playlistSerializer.Serialize(fs, main.ListPlaylists);
            }
        }
    }
}
