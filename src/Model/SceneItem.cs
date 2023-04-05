using OBSWebsocketDotNet.Types;

namespace StreamManager.Model
{
    public class ObservableSceneItem
    {
        public SceneItemDetails SceneItem { get; set; }

        public override string ToString()
        {
            return SceneItem.SourceName;
        }
    }
}
