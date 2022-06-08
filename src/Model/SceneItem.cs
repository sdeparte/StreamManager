﻿using OBSWebsocketDotNet.Types;

namespace StreamManager.Model
{
    public class ObservableSceneItem
    {
        public SceneItem SceneItem { get; set; }

        public override string ToString()
        {
            return SceneItem.SourceName;
        }
    }
}
