using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace StreamManager.Services
{
    public class MidiController
    {
        public readonly string[] _enumPossibleActions = new string[17] {
            "Changer de scène",
            "Muter / Unmute un élément d'une scène",
            "Muter un élément d'une scène",
            "Unmute un élément d'une scène",
            "Recommencer un élément (média) d'une scène",
            "Démarer / Arreter le stream",
            "Démarer le stream",
            "Arreter le stream",
            "Démarer / Arreter l'engregistrement",
            "Démarer l'engregistrement",
            "Mettre en pause l'engregistrement",
            "Reprendre l'engregistrement",
            "Arreter l'engregistrement",
            "Transférer la note MIDI",
            "Play / Pause la playlist en cours",
            "Arreter l'engregistrement",
            "Changer les informations du stream"
        };

        private readonly HttpClient _httpClient;

        private readonly List<IMidiInputDevice> _devices = new List<IMidiInputDevice>();
        private readonly ObservableCollection<string> _listPossibleActions = new ObservableCollection<string>();

        public event EventHandler<int> NewMidiNoteReceived;
        public event EventHandler<Message> NewMessageRaised;

        public ObservableCollection<string> ListPossibleActions => _listPossibleActions;

        public ObservableCollection<Message> ListActions { get; set; } = new ObservableCollection<Message>();

        public MidiController(HttpClient httpClient)
        {
            _httpClient = httpClient;

            foreach (IMidiInputDeviceInfo device in MidiDeviceManager.Default.InputDevices)
            {
                IMidiInputDevice inputDevice = device.CreateDevice();
                inputDevice.NoteOn += NoteOnMessageHandler;
                inputDevice.Open();

                _devices.Add(inputDevice);
            }

            foreach (string possibleAction in _enumPossibleActions)
            {
                _listPossibleActions.Add(possibleAction);
            }
        }

        public int GetActionIndex(string action)
        {
            return Array.IndexOf(_enumPossibleActions, action);
        }

        public ObservableAction GenerateAction(int action, string scene, string sceneItem, StreamConfig streamConfig)
        {
            return new ObservableAction() { Name = _enumPossibleActions[action], Scene = scene, SceneItem = sceneItem, StreamConfig = streamConfig.Name };
        }

        public void AddAction(string midiNote, ObservableCollection<ObservableAction> actions)
        {
            foreach (Message message in ListActions)
            {
                if (message.MidiNote == midiNote)
                {
                    ListActions.Remove(message);
                    break;
                }
            }

            ListActions.Add(new Message() { MidiNote = midiNote, Actions = new ObservableCollection<ObservableAction>(actions) });
        }

        public void RemoveActionAt(int index)
        {
            ListActions.RemoveAt(index);
        }

        public Message GetActionAt(int index)
        {
            return ListActions[index];
        }

        private void NoteOnMessageHandler(IMidiInputDevice sender, in NoteOnMessage msg)
        {
            Key key = msg.Key;

            foreach (Message message in ListActions)
            {
                if (int.TryParse(message.MidiNote, out int midiNote) && midiNote == (int) key)
                {
                    NewMessageRaised?.Invoke(this, message);
                    break;
                }
            }

            NewMidiNoteReceived?.Invoke(this, (int) key);
        }

        public void UpMidiNote(int midiNote)
        {
            foreach (IMidiOutputDeviceInfo device in MidiDeviceManager.Default.OutputDevices)
            {
                if (!device.Name.Contains("Microsoft GS Wavetable Synth"))
                {
                    _ = SendMidiNoteAsync(device, midiNote);
                }
            }

            Console.WriteLine($" [{DateTime.Now}] -- Midi Controller : Note #{midiNote} sended");
        }

        private async Task SendMidiNoteAsync(IMidiOutputDeviceInfo device, int midiNote)
        {
            IMidiOutputDevice outputDevice = device.CreateDevice();

            outputDevice.Open();

            outputDevice.Send(new NoteOnMessage(Channel.Channel1, (Key)midiNote, 127));

            await Task.Delay(100);

            outputDevice.Send(new NoteOffMessage(Channel.Channel1, (Key)midiNote, 0));

            await Task.Delay(5000);

            outputDevice.Send(new NoteOnMessage(Channel.Channel1, (Key)midiNote, 127));

            await Task.Delay(100);

            outputDevice.Send(new NoteOffMessage(Channel.Channel1, (Key)midiNote, 0));

            outputDevice.Close();
        }

        public async void ForwardMidiNote(int midiNote)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "midiNote", midiNote.ToString() }
            };

            FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);
            _ = await _httpClient.PostAsync(Resources.NoteForwardUrl, encodedContent);
        }
    }
}
