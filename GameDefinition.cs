using System;
using System.Collections.Generic;
using System.Text;

namespace Waterlily
{
    public class GameDefinition
    {
        public string name { get; set; }
        public string author { get; set; }
        public int userLocation { get; set; }
        public List<string> intro { get; set; }
        public List<Item> items { get; set; }
        public List<Location> locations { get; set; }
    }

    public class Item
    {
        public string title { get; set; }
        public string examinedTitle { get; set; }
        public string longDescription { get; set; }
        public string shortDescription { get; set; }
        public string examinedShortDescription { get; set; }
        public int location { get; set; }
        public bool carry { get; set; } = false;
        public bool canTake { get; set; } = false;
        public bool canUse { get; set; } = false;
        public bool wasExamined { get; set; } = false;
        public bool explosive { get; set; } = false;
        public bool canOpen { get; set; } = false;
        public bool canBreak { get; set; } = false;
        public bool isOpen { get; set; } = false;
        public bool isBroken { get; set; } = false;
        public bool canBreakTool { get; set; } = false;
    }

    public class Location
    {
        public int number { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public int destNorth { get; set; } = -1;
        public int destSouth { get; set; } = -1;
        public int destEast { get; set; } = -1;
        public int destWest { get; set; } = -1;
        public int destSouthWest { get; set; } = -1;
        public int destSouthEast { get; set; } = -1;
        public int destNorthWest { get; set; } = -1;
        public int destNorthEast { get; set; } = -1;
        public int destUp { get; set; } = -1;
        public int destDown { get; set; } = -1;
        public bool showedDescription { get; set; }
    }

    public class PendingAction
    {
        public int location { get; set; }
        public string item { get; set; }
        public string action { get; set; }
        public bool active { get; set; } = false;
        public bool completed { get; set; } = false;
    }
}
