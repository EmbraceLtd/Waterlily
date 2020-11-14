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
        public bool carry { get; set; }
        public bool canTake { get; set; }
        public bool canUse { get; set; }
        public bool wasExamined { get; set; }
        public bool sensitive { get; set; }
        public bool canOpen { get; set; }
        public bool canBreak { get; set; }
        public bool isOpen { get; set; }
        public bool isBroken { get; set; }
        public bool canBreakTool { get; set; }
    }

    public class Location
    {
        public int number { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public int destNorth { get; set; }
        public int destSouth { get; set; }
        public int destEast { get; set; }
        public int destWest { get; set; }
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
