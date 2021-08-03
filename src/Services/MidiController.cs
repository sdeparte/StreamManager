using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Windows;

namespace StreamManager.Services
{
    public class MidiController
    {
        private readonly string[] actions = new string[13] { "Changer de scène", "Muter / Unmute un élément d'une scène", "Muter un élément d'une scène", "Unmute un élément d'une scène", "Recommencer un élément (média) d'une scène", "Démarer / Arreter le stream", "Démarer le stream", "Arreter le stream", "Démarer / Arreter l'engregistrement", "Démarer l'engregistrement", "Mettre en pause l'engregistrement", "Reprendre l'engregistrement", "Arreter l'engregistrement" };
        private readonly string[] commandActions = new string[2] { "Envoyer un message", "Envoyer une note MIDI" };

        private MainWindow main;

        private List<IMidiInputDevice> devices = new List<IMidiInputDevice>();

        public MidiController(MainWindow main)
        {
            this.main = main;

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

            main.CommandActions.Items.Clear();

            foreach (string action in commandActions)
            {
                main.CommandActions.Items.Add(action);
            }

            main.CommandActions.Text = commandActions[0];
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
                            main.Get_ObsLinker().Get_Obs().SetCurrentScene(message.Scene);
                            break;

                        case 1:
                            main.Get_ObsLinker().Get_Obs().ToggleMute(message.SceneItem);
                            break;

                        case 2:
                            main.Get_ObsLinker().Get_Obs().SetMute(message.SceneItem, true);
                            break;

                        case 3:
                            main.Get_ObsLinker().Get_Obs().SetMute(message.SceneItem, false);
                            break;

                        case 4:
                            main.Get_ObsLinker().Get_Obs().RestartMedia(message.SceneItem);
                            break;

                        case 5:
                            main.Get_ObsLinker().Get_Obs().ToggleStreaming();
                            break;

                        case 6:
                            main.Get_ObsLinker().Get_Obs().StartStreaming();
                            break;

                        case 7:
                            main.Get_ObsLinker().Get_Obs().StopStreaming();
                            break;

                        case 8:
                            main.Get_ObsLinker().Get_Obs().ToggleRecording();
                            break;

                        case 9:
                            main.Get_ObsLinker().Get_Obs().StartRecording();
                            break;

                        case 10:
                            main.Get_ObsLinker().Get_Obs().PauseRecording();
                            break;

                        case 11:
                            main.Get_ObsLinker().Get_Obs().ResumeRecording();
                            break;

                        case 12:
                            main.Get_ObsLinker().Get_Obs().StopStreaming();
                            break;
                    }
                    break;
                }
            }

            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.MidiNote.Text = ((int)key).ToString();
            }));
        }

        public void UpMidiNote(int note)
        {
            foreach (IMidiOutputDeviceInfo device in MidiDeviceManager.Default.OutputDevices)
            {
                if (device.Name.Contains("Arduino"))
                {
                    IMidiOutputDevice outputDevice = device.CreateDevice();
                    outputDevice.Open();
                    outputDevice.Send(new NoteOnMessage(Channel.Channel1, (Key)note, 127));
                    System.Threading.Thread.Sleep(100);
                    outputDevice.Send(new NoteOffMessage(Channel.Channel1, (Key)note, 0));

                    System.Threading.Thread.Sleep(5000);

                    outputDevice.Send(new NoteOnMessage(Channel.Channel1, (Key)note, 127));
                    System.Threading.Thread.Sleep(100);
                    outputDevice.Send(new NoteOffMessage(Channel.Channel1, (Key)note, 0));
                    outputDevice.Close();
                }
            }
        }

        public string[] Get_Actions()
        {
            return actions;
        }

        public string[] Get_CommandActions()
        {
            return commandActions;
        }
    }
}
