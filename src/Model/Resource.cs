﻿namespace StreamManager.Model
{
    public class Resource
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
