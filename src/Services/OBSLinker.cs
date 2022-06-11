using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Services
{
    public class OBSLinker
    {
        public readonly OBSWebsocket Obs;

        private bool _state = false;

        private readonly ObservableCollection<ObservableScene> _listScenes = new ObservableCollection<ObservableScene>();
        private readonly ObservableCollection<ObservableSceneItem> _listSceneItems = new ObservableCollection<ObservableSceneItem>();

        public event EventHandler<bool> ObsConnected;

        public SolidColorBrush StateBrush => new SolidColorBrush(_state ? Colors.Green : Colors.Red);

        public ObservableCollection<ObservableScene> ListScenes => _listScenes;

        public ObservableCollection<ObservableSceneItem> ListSceneItems => _listSceneItems;

        public OBSLinker()
        {
            Obs = new OBSWebsocket();
            Obs.Connected += OnConnect;

            try
            {
                Obs.Connect(Resources.ObsUri, Resources.ObsPassword);
            }
            catch (AuthFailureException)
            {
                MessageBox.Show("Authentication failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (ErrorResponseException ex)
            {
                MessageBox.Show("Connect failed : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void OnConnect(object sender, EventArgs e)
        {
            _state = true;
            ObsConnected?.Invoke(this, true);
        }

        public void LoadScenes()
        {
            if (Obs.IsConnected)
            {
                _listScenes.Clear();

                foreach (OBSScene obsScene in Obs.ListScenes())
                {
                    _listScenes.Add(new ObservableScene { OBSScene = obsScene });
                }
            }
        }

        public void LoadScenesItems(ObservableScene scene)
        {
            if (Obs.IsConnected)
            {
                _listSceneItems.Clear();

                foreach (SceneItem sceneItem in scene.OBSScene.Items)
                {
                    _listSceneItems.Add(new ObservableSceneItem { SceneItem = sceneItem });
                }
            }
        }
    }
}
