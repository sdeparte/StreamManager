using OBSWebsocketDotNet.Types;

namespace StreamManager.Model
{
    public class ObservableScene
    {
        public SceneBasicInfo ObsScene { get; set; }

        public override string ToString()
        {
            return ObsScene.Name;
        }
    }
}
