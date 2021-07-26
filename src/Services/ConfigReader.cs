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
        private readonly XmlSerializer commandeSerializer = new XmlSerializer(typeof(ObservableCollection<Command>));
        private readonly XmlSerializer resourceSerializer = new XmlSerializer(typeof(ObservableCollection<Resource>));

        public void readConfigFiles(MainWindow main)
        {
            using (FileStream fs = new FileStream(@"actions.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.Set_ListActions(messageSerializer.Deserialize(fs) as ObservableCollection<Message>);
                }
                catch (Exception ex) { }
            }

            using (FileStream fs = new FileStream(@"commands.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.Set_ListCommands(commandeSerializer.Deserialize(fs) as ObservableCollection<Command>);
                }
                catch (Exception ex) { }
            }

            using (FileStream fs = new FileStream(@"resources.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.Set_ListResources(resourceSerializer.Deserialize(fs) as ObservableCollection<Resource>);
                }
                catch (Exception ex) { }
            }
        }

        public void updateConfigFiles(MainWindow main)
        {
            using (FileStream fs = new FileStream(@"actions.xml", FileMode.OpenOrCreate))
            {
                messageSerializer.Serialize(fs, main.Get_ListActions());
            }

            using (FileStream fs = new FileStream(@"commands.xml", FileMode.OpenOrCreate))
            {
                commandeSerializer.Serialize(fs, main.Get_ListCommands());
            }

            using (FileStream fs = new FileStream(@"resources.xml", FileMode.OpenOrCreate))
            {
                resourceSerializer.Serialize(fs, main.Get_ListResources());
            }
        }
    }
}
