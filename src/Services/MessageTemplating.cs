using DotLiquid;

namespace StreamManager.Services
{
    public class MessageTemplating
    {
        private readonly MusicPlayer _musicPlayer;

        public MessageTemplating(MusicPlayer musicPlayer)
        {
            _musicPlayer = musicPlayer;
        }

        public string renderMessage(string message)
        {
            Template template = Template.Parse(message);

            return template.Render(Hash.FromAnonymousObject(new { currentSong = _musicPlayer.CurrentSong }));
        }
    }
}