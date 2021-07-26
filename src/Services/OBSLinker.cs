using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using StreamManager.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Services
{
    public class OBSLinker
    {
        private MainWindow main;

        private OBSWebsocket obs;

        public OBSLinker(MainWindow main)
        {
            this.main = main;

            obs = new OBSWebsocket();
            obs.Connected += onConnect;

            try
            {
                obs.Connect(Resources.ObsUri, Resources.ObsPassword);
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

        private void onConnect(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                main.OBSState.Fill = new SolidColorBrush(Colors.Green);

                main.Scenes.Items.Clear();

                foreach (OBSScene scene in obs.ListScenes())
                {
                    ComboBoxItem scenceNode = new ComboBoxItem();
                    scenceNode.Content = scene.Name;
                    scenceNode.Tag = scene;

                    main.Scenes.Items.Add(scenceNode);
                }
            }));
        }

        public List<ComboBoxItem> LoadOBSSceneItems(ComboBoxItem comboBoxScene)
        {
            List<ComboBoxItem> sceneItems = new List<ComboBoxItem>();

            if (null != comboBoxScene && comboBoxScene.Tag is OBSScene)
            {
                OBSScene scene = (OBSScene) comboBoxScene.Tag;

                foreach (SceneItem sceneItem in scene.Items)
                {
                    ComboBoxItem sceneItemNode = new ComboBoxItem();
                    sceneItemNode.Content = sceneItem.SourceName;
                    sceneItemNode.Tag = sceneItem;

                    sceneItems.Add(sceneItemNode);
                }
            }

            return sceneItems;
        }

        public OBSWebsocket Get_Obs()
        {
            return obs;
        }
    }
}
