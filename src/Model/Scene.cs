using OBSWebsocketDotNet.Types;

namespace StreamManager.Model
{
    public class ObservableScene
    {
        public OBSScene OBSScene { get; set; }

        public override string ToString()
        {
            return OBSScene.Name;
        }
    }
}
