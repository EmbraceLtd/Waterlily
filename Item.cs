using System;
using System.Collections.Generic;
using System.Text;

namespace Deerwood
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
    }
}
