using DotLiquid;

namespace StreamManager.Services
{
    public class MessageTemplating
    {
        private MainWindow main;

        public MessageTemplating(MainWindow main)
        {
            this.main = main;
        }

        public string renderMessage(string message)
        {
            Template template = Template.Parse(message);

            return template.Render(Hash.FromAnonymousObject(new { currentSong = main.Get_MusicPlayer().CurrentSong }));
        }
    }
}