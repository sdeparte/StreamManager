using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Messages;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Windows;

namespace StreamManager.Services
{
    public class MidiController
    {
        private readonly string[] actions = new string[4] { "Changer de scène", "Muter / Unmute un élément d'une scène", "Démarer / Arreter le stream", "Démarer / Arreter l'engregistrement" };

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
                            main.Get_ObsLinker().Get_Obs().ToggleStreaming();
                            break;

                        case 3:
                            main.Get_ObsLinker().Get_Obs().ToggleRecording();
                            break;
                    }
                    break;
                }
            }

            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.MidiNote.Text = ((int)key).ToString();
            }));
        }

        public string[] Get_Actions()
        {
            return actions;
        }
    }
}
