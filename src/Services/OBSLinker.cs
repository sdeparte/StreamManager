using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using StreamManager.Helpers;
using StreamManager.Model;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace StreamManager.Services
{
    public class OBSLinker
    {
        public readonly OBSWebsocket _obs;

        private bool _state = false;

        private readonly ObservableCollection<ObservableScene> _listScenes = new ObservableCollection<ObservableScene>();
        private readonly ObservableCollection<ObservableSceneItem> _listSceneItems = new ObservableCollection<ObservableSceneItem>();

        public event EventHandler<bool> ObsConnected;

        public SolidColorBrush StateBrush => new SolidColorBrush(_state ? Colors.Green : Colors.Red);

        public ObservableCollection<ObservableScene> ListScenes => _listScenes;

        public ObservableCollection<ObservableSceneItem> ListSceneItems => _listSceneItems;

        public OBSLinker()
        {
            _obs = new OBSWebsocket();
            _obs.Connected += OnConnect;

            try
            {
                _obs.Connect(Resources.ObsUri, Resources.ObsPassword);
            }
            catch (AuthFailureException)
            {
                ToastHelper.Toast("Connexion échoué", $"La connection au WebSocket d'OBS a échoué");
            }
            catch (ErrorResponseException)
            {
                ToastHelper.Toast("Connexion impossible", $"Impossible de se connecter au WebSocket d'OBS");
            }
        }

        private void OnConnect(object sender, EventArgs e)
        {
            _state = true;
            ObsConnected?.Invoke(this, true);
        }

        public void LoadScenes()
        {
            if (_obs.IsConnected)
            {
                _listScenes.Clear();

                foreach (SceneBasicInfo obsScene in _obs.ListScenes())
                {
                    _listScenes.Add(new ObservableScene { ObsScene = obsScene });
                }
            }
        }

        public void LoadScenesItems(ObservableScene scene)
        {
            if (_obs.IsConnected)
            {
                _listSceneItems.Clear();

                foreach (SceneItemDetails sceneItem in _obs.GetSceneItemList(scene.ObsScene.Name))
                {
                    _listSceneItems.Add(new ObservableSceneItem { SceneItem = sceneItem });
                }
            }
        }

        public void SetCurrentScene(string scene)
        {
            try
            {
                if (_obs.IsConnected)
                {
                    _obs.SetCurrentProgramScene(scene);
                }
            }
            catch (ErrorResponseException)
            {
                ToastHelper.Toast("Relation introuvable", $"La scène OBS \"{scene}\" est introuvable");
            }
        }

        public void ToggleMute(string sceneItem)
        {
            try
            {
                if (_obs.IsConnected)
                {
                    _obs.ToggleInputMute(sceneItem);
                }
            }
            catch (ErrorResponseException)
            {
                ToastHelper.Toast("Relation introuvable", $"L'item OBS \"{sceneItem}\" est introuvable");
            }
        }

        public void SetMute(string sceneItem, bool mute)
        {
            try
            {
                if (_obs.IsConnected)
                {
                    _obs.SetInputMute(sceneItem, mute);
                }
            }
            catch (ErrorResponseException)
            {
                ToastHelper.Toast("Relation introuvable", $"L'item OBS \"{sceneItem}\" est introuvable");
            }
        }

        public void ToggleStreaming()
        {
            if (_obs.IsConnected)
            {
                _obs.ToggleStream();
            }
        }

        public void StartStreaming()
        {
            if (_obs.IsConnected)
            {
                _obs.StartStream();
            }
        }

        public void StopStreaming()
        {
            if (_obs.IsConnected)
            {
                _obs.StopStream();
            }
        }

        public void ToggleRecording()
        {
            if (_obs.IsConnected)
            {
                _obs.ToggleRecord();
            }
        }

        public void StartRecording()
        {
            if (_obs.IsConnected)
            {
                _obs.StartRecord();
            }
        }

        public void PauseRecording()
        {
            if (_obs.IsConnected)
            {
                _obs.PauseRecord();
            }
        }

        public void ResumeRecording()
        {
            if (_obs.IsConnected)
            {
                _obs.ResumeRecord();
            }
        }

        public void StopRecording()
        {
            if (_obs.IsConnected)
            {
                _obs.StopRecord();
            }
        }
    }
}
