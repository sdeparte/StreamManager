using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace StreamManager.Services
{
    public class MidiController
    {
        private readonly string[] actions = new string[14] {
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
            "Transférer la note MIDI"
        };

        private MainWindow main;

        private HttpClient httpClient;

        private List<IMidiInputDevice> devices = new List<IMidiInputDevice>();

        public MidiController(MainWindow main, HttpClient httpClient)
        {
            this.main = main;
            this.httpClient = httpClient;

            foreach (IMidiInputDeviceInfo device in MidiDeviceManager.Default.InputDevices)
            {
                IMidiInputDevice inputDevice = device.CreateDevice();
                inputDevice.NoteOn += NoteOnMessageHandler;
                inputDevice.Open();

                devices.Add(inputDevice);
            }

            main.Actions.Items.Clear();

            foreach (string action in actions)
            {
                main.Actions.Items.Add(action);
            }
        }

        private void NoteOnMessageHandler(IMidiInputDevice sender, in NoteOnMessage msg)
        {
            RtMidi.Core.Enums.Key key = msg.Key;

            foreach (Message message in main.Get_ListActions())
            {
                int midiNote = -1;
                int.TryParse(message.MidiNote, out midiNote);

                if (midiNote == (int)key)
                {
                    switch (Array.IndexOf(actions, message.Action))
                    {
                        case 0:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().SetCurrentScene(message.Scene);
                            }
                            break;

                        case 1:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().ToggleMute(message.SceneItem);
                            }
                            break;

                        case 2:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().SetMute(message.SceneItem, true);
                            }
                            break;

                        case 3:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().SetMute(message.SceneItem, false);
                            }
                            break;

                        case 4:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().RestartMedia(message.SceneItem);
                            }
                            break;

                        case 5:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().ToggleStreaming();
                            }
                            break;

                        case 6:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().StartStreaming();
                            }
                            break;

                        case 7:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().StopStreaming();
                            }
                            break;

                        case 8:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().ToggleRecording();
                            }
                            break;

                        case 9:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().StartRecording();
                            }
                            break;

                        case 10:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().PauseRecording();
                            }
                            break;

                        case 11:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().ResumeRecording();
                            }
                            break;

                        case 12:
                            if (main.Get_ObsLinker().Get_Obs().IsConnected)
                            {
                                main.Get_ObsLinker().Get_Obs().StopStreaming();
                            }
                            break;

                        case 13:
                            this.ForwardMidiNote(midiNote);
                            break;
                    }
                    break;
                }
            }

            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.MidiNote.Text = ((int)key).ToString();
            }));
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

            HttpResponseMessage response = await httpClient.PostAsync(Resources.NoteForwardUrl, encodedContent);
        }

        public string[] Get_Actions()
        {
            return actions;
        }
    }
}
