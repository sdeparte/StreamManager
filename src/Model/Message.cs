using System.Collections.ObjectModel;

namespace StreamManager.Model
{
    public class Message
    {
        public string MidiNote { get; set; }

        public ObservableCollection<ObservableAction> Actions { get; set; }
    }
}
