﻿using TwitchLib.Api.Helix.Models.Games;

namespace StreamManager.Model
{
    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
