using System;
using System.Collections.Generic;
using System.Text;

namespace Waterlily
{
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
}
