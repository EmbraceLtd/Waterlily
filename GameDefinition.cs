﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waterlily
{
    public class GameDefinition
    {
        public string name { get; set; }
        public string author { get; set; }
        public string userLocation { get; set; }
        public List<string> intro { get; set; }
        public List<PropertyCollection> items { get; set; }
        public List<PropertyCollection> locations { get; set; }
        public List<Command> commands { get; set; }
        public List<Action> actions { get; set; }
    }

    public class PropertyCollection
    {
        public Dictionary<string, string> properties { get; set; }

        public string getProp(string property)
        {
            if (properties.ContainsKey(property))
                return properties[property];
            else
                return "";
        }

        public bool isProp(string property)
        {
            if (properties.ContainsKey(property))
                return properties[property] == "1";
            else
                return false;
        }

        public void setProp(string property, string value)
        {
            if (properties.ContainsKey(property))
                properties[property] = value;
            else
                properties.Add(property, value);
        }

        public void trueProp(string property)
        {
            if (properties.ContainsKey(property))
                properties[property] = "1";
            else
                properties.Add(property, "1");
        }
    }

    public class Location
    {
        public string number { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string destNorth { get; set; } = "-1";
        public string destSouth { get; set; } = "-1";
        public string destEast { get; set; } = "-1";
        public string destWest { get; set; } = "-1";
        public string destSouthWest { get; set; } = "-1";
        public string destSouthEast { get; set; } = "-1";
        public string destNorthWest { get; set; } = "-1";
        public string destNorthEast { get; set; } = "-1";
        public string destUp { get; set; } = "-1";
        public string destDown { get; set; } = "-1";
        public bool showedDescription { get; set; }
    }

    public class PendingAction
    {
        public string location { get; set; }
        public string item { get; set; }
        public string action { get; set; }
        public bool active { get; set; } = false;
        public bool completed { get; set; } = false;
    }

    public class Command
    {
        public List<string> verbs { get; set; }
        public string targetAction { get; set; }
    }

    public class Action
    {
        public string name { get; set; }
        public List<Condition> conditions { get; set; }
        public List<string> operations { get; set; }
        public string completedMessage { get; set; }
    }

    public class Condition
    {
        public string condition { get; set; }
        public string failureMessage { get; set; }
    }
}
