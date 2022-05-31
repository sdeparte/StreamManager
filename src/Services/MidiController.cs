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
        public readonly string[] _enumPossibleActions = new string[16] {
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
            "Passer à la musique suivante"
        };

        private readonly OBSLinker _obsLinker;
        private readonly MusicPlayer _musicPlayer;
        private readonly HttpClient _httpClient;

        private readonly List<IMidiInputDevice> _devices = new List<IMidiInputDevice>();
        private readonly ObservableCollection<string> _listPossibleActions = new ObservableCollection<string>();

        private ObservableCollection<Message> _listActions = new ObservableCollection<Message>();

        public event EventHandler<int> NewMidiNoteReceived;

        public ObservableCollection<string> ListPossibleActions
        {
            get { return _listPossibleActions; }
        }

        public ObservableCollection<Message> ListActions
        {
            get { return _listActions; }
            set { _listActions = value; }
        }

        public MidiController(OBSLinker obsLinker, MusicPlayer musicPlayer, HttpClient httpClient)
        {
            _obsLinker = obsLinker;
            _musicPlayer = musicPlayer;
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

        public void AddAction(string midiNote, int action, string scene, string sceneItem)
        {
            foreach (Message message in _listActions)
            {
                if (message.MidiNote == midiNote)
                {
                    _listActions.Remove(message);
                    break;
                }
            }

            _listActions.Add(new Message() { MidiNote = midiNote, Action = _enumPossibleActions[action], Scene = scene, SceneItem = sceneItem });
        }

        public void RemoveActionAt(int index)
        {
            _listActions.RemoveAt(index);
        }

        public Message GetActionAt(int index)
        {
            return _listActions[index];
        }

        private void NoteOnMessageHandler(IMidiInputDevice sender, in NoteOnMessage msg)
        {
            Key key = msg.Key;

            foreach (Message message in _listActions)
            {
                int midiNote = -1;
                int.TryParse(message.MidiNote, out midiNote);

                if (midiNote == (int) key)
                {
                    switch (Array.IndexOf(_enumPossibleActions, message.Action))
                    {
                        case 0:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.SetCurrentScene(message.Scene);
                            }
                            break;

                        case 1:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.ToggleMute(message.SceneItem);
                            }
                            break;

                        case 2:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.SetMute(message.SceneItem, true);
                            }
                            break;

                        case 3:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.SetMute(message.SceneItem, false);
                            }
                            break;

                        case 4:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.RestartMedia(message.SceneItem);
                            }
                            break;

                        case 5:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.ToggleStreaming();
                            }
                            break;

                        case 6:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.StartStreaming();
                            }
                            break;

                        case 7:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.StopStreaming();
                            }
                            break;

                        case 8:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.ToggleRecording();
                            }
                            break;

                        case 9:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.StartRecording();
                            }
                            break;

                        case 10:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.PauseRecording();
                            }
                            break;

                        case 11:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.ResumeRecording();
                            }
                            break;

                        case 12:
                            if (_obsLinker.Obs.IsConnected)
                            {
                                _obsLinker.Obs.StopStreaming();
                            }
                            break;

                        case 13:
                            this.ForwardMidiNote(midiNote);
                            break;

                        case 14:
                            _musicPlayer.Pause();
                            break;

                        case 15:
                            _musicPlayer.PlayNextSong();
                            break;
                    }
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
