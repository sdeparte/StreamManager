namespace StreamManager.Model
{
    public class ObservableAction
    {
        public string Name { get; set; }

        public string Scene { get; set; }

        public string SceneItem { get; set; }

        public string StreamConfig { get; set; }

        public string Playlist { get; set; }

        public override string ToString()
        {
            if (Scene != null && SceneItem != null && StreamConfig == null)
            {
                return $"{Name} => Scène : {Scene} | Elément : {SceneItem}";
            }
            else if (Scene != null && StreamConfig == null)
            {
                return $"{Name} => Scène : {Scene}";
            }
            else if (StreamConfig != null)
            {
                return $"{Name} => Configuration : {StreamConfig}";
            }
            else if (Playlist != null)
            {
                return $"{Name} => Playlist : {Playlist}";
            }
            else
            {
                return $"{Name}";
            }
        }
    }
}
