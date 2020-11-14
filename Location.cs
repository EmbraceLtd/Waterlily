using System;
using System.Collections.Generic;
using System.Text;

namespace Waterlily
{
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
}
