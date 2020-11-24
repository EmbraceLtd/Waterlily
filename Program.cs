using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Waterlily
{
    class Program
    {
        private static GameDefinition gameDefinition;
        private static string userLocation;
        private static PropertyCollection myLocation;
        private static bool cont = true;
        private static List<PendingAction> pendingActions;
        private static int turnCount;
        private static string dashline = new string('*', 99);

        static void Main(string[] args)
        {
            var custom = args.Count() > 0 ? args[0] : string.Empty;
            if (InitializeWorld(custom))
            {

                while (cont)
                {
                    while (cont)
                    {
                        Console.Write($"{turnCount++}> ");
                        var userCommand = GetCommand();
                        ProcessCommand(userCommand);
                        ProcessPendingActions();
                    }
                    Console.WriteLine("You left this world in a puff of smoke! You are very dead.");
                    Console.Write("Revive? (Y/n)");
                    var revive = Reader.ReadLine().ToUpper();

                    if (revive == "Y" || revive == string.Empty)
                    {
                        cont = true;
                        InitializeWorld();
                    }
                }
            }
        }

        private static string GetCommand()
        {
            string cmd;
            try
            {
                cmd = Reader.ReadLine(60000);
            }
            catch (TimeoutException)
            {
                cmd = "wait";
                Console.WriteLine();
            }
            return cmd;
        }

        private static void DescribeWorld(bool brief = true)
        {
            if (!brief)
            {
                Console.WriteLine(myLocation.getProp("description"));
            }
            else
            {
                if (myLocation == null)
                {
                    Console.WriteLine("You have entered an undefined area! Look for a newer version of this game!");
                    cont = false;
                    return;
                }
                else
                {
                    if (myLocation.getProp("showedDescription")!="1")
                    {
                        Console.WriteLine(myLocation.getProp("description"));
                        myLocation.setProp("showedDescription", "1");
                    }
                    else
                        Console.WriteLine(myLocation.getProp("title"));
                }

            }
            ShowDestinations();

            var itemsHere = GetItemsByLocation(userLocation);
            if (itemsHere.Any())
            {
                Console.WriteLine("You can see: ");
                foreach (var item in itemsHere)
                    Console.WriteLine($"   {item.getProp("shortDescription")}.");
            }
        }

        private static void ShowDestinations()
        {
            Console.Write($"You can go ");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destNorth")) ? "north " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destNorthWest")) ? "northwest " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destNorthEast")) ? "northeast " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destSouth")) ? "south " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destSouthWest")) ? "southwest " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destSouthEast")) ? "southeast " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destEast")) ? "east " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destWest")) ? "west " : string.Empty)}"); 
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destUp")) ? "up " : string.Empty)}");
            Console.Write($"{(!string.IsNullOrEmpty(myLocation.getProp("destDown")) ? "down " : string.Empty)}");
            Console.WriteLine();
        }

        private static bool InitializeWorld(string customConfig = "")
        {
            pendingActions = new List<PendingAction>();
            turnCount = 1;

            if (ReadConfig(customConfig))
            {
                InitMessage();
                MainSettings();
                DescribeWorld();
                return true;
            }
            else
                return false;
        }

        private static void InitMessage()
        {
            Console.Clear();
            Console.WriteLine(dashline);
            Console.WriteLine("*");
            foreach (var line in gameDefinition.intro)
                Console.WriteLine($"*  {line}");
            Console.WriteLine("*");
            Console.WriteLine("**************************************************** Powered by Waterlily Engine by Tommy Sjöblom *");
            Console.WriteLine();
        }

        private static void MainSettings()
        {
            userLocation = gameDefinition.userLocation;
            myLocation = GetLocationByNumber(userLocation);
            cont = true;
        }

        private static void ProcessCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                cmd = "wait";

            var sentence = cmd.Split(' ');
            var verb = sentence[0].ToLower();
            var objName = sentence.Count() > 1 ? sentence[1] : string.Empty;

            foreach(var gamec in gameDefinition.commands)
            {
                if(gamec.verbs.Contains(verb))
                {
                    ProcessAction(gamec.targetAction, objName);
                }
            }
        }

        private static void ProcessAction(string targetAction, string objectName)
        {
            Action theAction = null;
            foreach (var action in gameDefinition.actions)
            {
                if (action.name == targetAction)
                    theAction = action;
            }

            if (theAction != null)
            {
                foreach(var cond in theAction.conditions)
                {
                    var result = InterpretCondition(cond.condition.FixString(objectName, userLocation));
                    if (result == false)
                    {
                        Console.WriteLine(cond.failureMessage.FixString(objectName, userLocation));
                        return;
                    }
                }

                var fixedOp = new List<string>();
                foreach(var o in theAction.operations)
                    fixedOp.Add(o.FixString(objectName, userLocation));

                CommitOperations(fixedOp);

                Console.WriteLine(theAction.completedMessage.FixString(objectName, userLocation));
            }
        }

        private static void CommitOperations(List<string> operations)
        {
            foreach (var op in operations)
            {
                var p = op.Split('.');
                var ope = p[0];

                if (ope == "set")
                {
                    var obj = p[1];
                    var prp = p[2];
                    var val = p[3];

                    var _object = GetItemByName(obj);
                    _object.setProp(prp, val);
                }

                if (ope == "write")
                {
                    var typ = p[1];
                    var obj = p[2];
                    var prp = p[3];

                    if (obj == "*" && typ=="item")
                    {
                        List<PropertyCollection> objects = null;
                        if (p.Length == 7)
                            objects = GetItemsByFilter(p[4], p[5], p[6]);
                        else
                            objects = GetAllItems();

                        if (objects != null)
                        {
                            foreach (var o in objects)
                                Console.WriteLine($"{o.getProp(prp)} ");

                            Console.WriteLine();
                        }
                    }
                    else
                    { 
                        PropertyCollection _object = null;

                        if (typ=="item")
                            _object = GetItemByName(obj);

                        if (typ == "loc")
                            _object = GetLocationByNumber(obj);

                        if (_object != null)
                            Console.WriteLine(_object.getProp(prp));
                    }
                }

                if (ope == "print")
                {
                    Console.WriteLine(p[1]);
                }
            }
        }

        private static bool InterpretCondition(string cnd)
        {
            var p = cnd.Split('.');
            var ope = p[0];

            if (ope == "get")
            {
                var obj = p[1];
                var prp = p[2];
                var cmp = p[3];
                var val = p[4];

                var _object = GetItemByName(obj);
                var objVal = _object.getProp(prp);
                if (cmp == "eq")
                    return (objVal == val);

                if (cmp == "ne")
                    return (objVal != val);
            }

            if (ope == "write")
            {
                var obj = p[1];
                var prp = p[2];
                var _object = GetItemByName(obj);
                var objVal = _object.getProp(prp);
                Console.WriteLine(objVal);
            }

            return false;
        }

        //private static void ProcessCommand2(string cmd)
        //{
        //    if (string.IsNullOrWhiteSpace(cmd))
        //        cmd = "wait";

        //    var sentence = cmd.Split(' ');
        //    var verb = sentence[0].ToUpper();
        //    var obj = sentence.Count() > 1 ? sentence[1] : string.Empty;

        //    switch (verb)
        //    {
        //        case "WAIT":
        //        case "SLEEP":
        //            WaitAction(verb);
        //            break;
        //        case "LOAD":
        //            LoadAction(obj);
        //            break;
        //        case "EXIT":
        //        case "Q":
        //            cont = false;
        //            break;
        //        case "LOOK" when obj == string.Empty:
        //        case "WHERE":
        //            DescribeWorld(brief: false);
        //            break;
        //        case "GET":
        //        case "TAKE":
        //            PickupAction(obj);
        //            break;
        //        case "FUCK":
        //            FuckAction(obj);
        //            break;
        //        case "DROP":
        //            DropAction(obj);
        //            break;
        //        case "INV":
        //        case "INVENTORY":
        //            InventoryAction();
        //            break;
        //        case "EXAMINE":
        //        case "LOOK":
        //            ExamineAction(obj);
        //            break;
        //        case "OPEN":
        //            OpenAction(obj);
        //            break;
        //        case "BREAK":
        //            BreakAction(obj);
        //            break;
        //        case "S":
        //        case "SOUTH":
        //            GoAction(myLocation.getProp("destSouth"));
        //            break;
        //        case "N":
        //        case "NORTH":
        //            GoAction(myLocation.getProp("destNorth"));
        //            break;
        //        case "E":
        //        case "EAST":
        //            GoAction(myLocation.getProp("destEast"));
        //            break;
        //        case "W":
        //        case "WEST":
        //            GoAction(myLocation.getProp("destWest"));
        //            break;
        //        case "SW":
        //        case "SOUTHWEST":
        //            GoAction(myLocation.getProp("destSouthWest"));
        //            break;
        //        case "SE":
        //        case "SOUTHEAST":
        //            GoAction(myLocation.getProp("destSouthEast"));
        //            break;
        //        case "NW":
        //        case "NORTHWEST":
        //            GoAction(myLocation.getProp("destNorthWest"));
        //            break;
        //        case "NE":
        //        case "NORTHEAST":
        //            GoAction(myLocation.getProp("destNorthEast"));
        //            break;
        //        case "U":
        //        case "UP":
        //            GoAction(myLocation.getProp("destUp"));
        //            break;
        //        case "D":
        //        case "DOWN":
        //            GoAction(myLocation.getProp("destDown"));
        //            break;
        //        default:
        //            Console.WriteLine("You gotta be kidding!");
        //            break;
        //    }
        //}

        private static void ProcessPendingActions()
        {
            foreach (var pendAction in pendingActions)
            {
                if (pendAction.action == "detonate" && pendAction.active && !pendAction.completed)
                    Detonate(pendAction);
            }

            pendingActions.RemoveAll(p => p.completed);

            foreach (var pendAction in pendingActions)
            {
                if (!pendAction.active)
                    pendAction.active = true;
            }
        }

        private static void Detonate(PendingAction pendAction)
        {
            if (pendAction.item == "bottle")
            {
                var loc = GetLocationByNumber(pendAction.location);
                Console.WriteLine($"The {pendAction.item} explodes in the {loc.getProp("title")}. A large bang is heard all over town!");
                var safe = GetItemByName("safe");
                safe.setProp("isOpen", "1");
                safe.setProp("longDescription", "The safe has been blown up");
                GetItemByName("money").setProp("location","3");
                GetItemByName("bottle").setProp("location", "-1");

                if (pendAction.location == userLocation)
                    cont = false;

                pendAction.active = false;
                pendAction.completed = true;
            }
        }

        private static void WaitAction(string verb)
        {
            Console.WriteLine($"You {verb.ToLower()}. Time passes.");
        }

        private static void LoadAction(string obj)
        {
            var configPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                obj);

            Console.WriteLine(configPath);

            if (string.IsNullOrEmpty(Path.GetExtension(configPath)))
                configPath = new StringBuilder(configPath).Append(".json").ToString();

            if (File.Exists(configPath))
            {
                if (InitializeWorld(configPath))
                {
                    InitMessage();
                    MainSettings();
                    DescribeWorld();
                }
            }
            else
            {
                Console.WriteLine($"Can't find {configPath}");
            }
        }

        private static void OpenAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.getProp("location") == userLocation)
                {
                    if (item.getProp("canOpen") == "1")
                    {
                        if (item.getProp("isOpen") != "1")
                        {
                            Console.WriteLine($"You open the {item.getProp("examinedTitle")}.");
                            item.setProp("isOpen", "1");
                        }
                        else
                        {
                            Console.WriteLine($"The {item.getProp("examinedTitle")} is already open!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"The {item.getProp("examinedTitle")} can't be opened.");
                    }
                }
                else
                {
                    Console.WriteLine("It ain't here!");
                }
            }
        }

        private static void BreakAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.getProp("location") == userLocation)
                {
                    if (item.isProp("canBreak"))
                    {
                        var breakItem = GetBreakingTool();
                        if (breakItem != null)
                        {
                            if (item.isProp("explosive"))
                            {
                                Console.WriteLine($"You break the {item.getProp("examinedTitle")} with the {breakItem.getProp("title")}.");
                                Console.WriteLine($"KABOOM! It explodes and you are blown to bits!");
                                cont = false;
                            }
                            else
                            {
                                if (item.isProp("isBroken"))
                                {
                                    Console.WriteLine($"You break the {item.getProp("examinedTitle")} with the {breakItem.getProp("title")}.");
                                    item.setProp("shortDescription", new StringBuilder(item.getProp("shortDescription").Insert(2, "broken ")).ToString());
                                    item.setProp("longDescription", new StringBuilder(item.getProp("longDescription")).Replace("closed", "broken").ToString());
                                    item.setProp("isBroken", "1");

                                    if (userLocation == "1" && obj == "window")
                                    {
                                        myLocation.setProp("destNorth", "3");
                                        ShowDestinations();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"The {item.getProp("examinedTitle")} is already broken!");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"You don't have anything to break the {item.getProp("examinedTitle")} with.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"The {item.getProp("examinedTitle")} can't be broken.");
                    }
                }
                else
                {
                    Console.WriteLine("It ain't here!");
                }
            }
        }

        private static void GoAction(string destination)
        {
            if (destination != "-1")
            {
                userLocation = destination;
                MoveMyItems(destination);
                myLocation = GetLocationByNumber(destination);
                DescribeWorld();
            }
            else
            {
                Console.WriteLine("You can't go there!");
            }
        }

        private static void ExamineAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.getProp("location") == userLocation)
                {
                    Console.WriteLine(item.getProp("longDescription"));
                    item.setProp("wasExamined", "1");
                    item.setProp("shortDescription", item.getProp("examinedShortDescription"));
                }
                else
                {
                    Console.WriteLine("It ain't here!");
                }
            }
        }

        private static void InventoryAction()
        {
            Console.WriteLine("You are carrying: ");
            var myItems = GetMyItems();

            if (!myItems.Any())
                Console.WriteLine("   not a damn thing!");

            foreach (var item in GetMyItems())
                Console.WriteLine($"   {item.getProp("shortDescription")}. ");
        }

        private static PropertyCollection GetBreakingTool()
        {
            foreach (var item in GetMyItems())
            {
                if (item.getProp("canBreakTool") == "1")
                    return item;
            }
            return null;
        }

        private static void DropAction(string obj)
        {
            if (string.IsNullOrEmpty(obj))
                Console.WriteLine($"Drop what?");
            else
            {
                var myItems = new List<PropertyCollection>();
                if (obj.ToUpper() == "ALL")
                {
                    myItems = GetMyItems();
                }
                else
                {
                    var item = GetItemByName(obj);
                    if (item != null)
                        myItems.Add(item);
                }
                foreach(var item in myItems)
                {
                    if (item.getProp("carry")=="1")
                    {
                        item.setProp("carry", "0");
                        Console.WriteLine($"You {(item.getProp("explosive") == "1" ? "very carefully put down" : "drop")} the {item.getProp("examinedTitle")}.");
                        if (item.getProp("title") == "bottle")
                            pendingActions.Add(new PendingAction { action = "detonate", item = "bottle", location = userLocation});
                    }
                    else
                    {
                        Console.WriteLine("You ain't got it!");
                    }
                }
            }
        }

        private static void FuckAction(string obj)
        {
            if (string.IsNullOrEmpty(obj))
                Console.WriteLine($"Hey!");
            else
            {
                var item = GetItemByName(obj);
                if (item != null)
                {
                    if (item.getProp("location") == userLocation)
                    {
                        if (item.getProp("title") == "bottle")
                        {
                            Console.WriteLine("KABOOM! The bottle of nitroglycerine explodes in your tender parts. Should have been more careful!");
                            cont = false;
                        }
                        else
                        {
                            Console.WriteLine($"You don't want to do that! I'm serious!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("It ain't here!");
                    }
                }
            }
        }

        private static void PickupAction(string obj)
        {
            if (string.IsNullOrEmpty(obj))
                Console.WriteLine($"Get what?");
            else
            {
                var itemsHere = new List<PropertyCollection>();
                if (obj.ToUpper() == "ALL")
                {
                    itemsHere = GetItemsByLocation(userLocation);
                }
                else
                {
                    var item = GetItemByName(obj);
                    if (item != null)
                        itemsHere.Add(item);
                }

                foreach (var item in itemsHere)
                {
                    if (item.getProp("canTake") == "1")
                    {
                        if (item.getProp("location") == userLocation)
                        {
                            if (item.getProp("carry") != "1")
                            {
                                item.setProp("carry", "1");
                                if (item.getProp("explosive") == "1")
                                {
                                    if (item.getProp("wasExamined") != "1")
                                    {
                                        Console.WriteLine($"KABOOM! The {item.getProp("examinedTitle")} explodes in your hand. Should have been more careful!");
                                        cont = false;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"You very carefully pick up the {item.getProp("title")}.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"You got the {item.getProp("title")}!");
                                }
                            }
                            else
                            {
                                Console.WriteLine("You got it already!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("It ain't here!");
                        }
                    }
                    else
                    {
                        if (obj.ToUpper() != "ALL")
                            Console.WriteLine("You can't!");
                    }
                }
            }
        }

        private static bool HaveItem(string obj)
        {
            var qItem = GetItemByName(obj);
            if (qItem != null)
            {
                foreach(var item in GetMyItems())
                {
                    if (item == qItem)
                        return true;
                }
            }
            return false;
        }

        private static void MoveMyItems(string location)
        {
            foreach (var item in GetMyItems())
                item.setProp("location", location);
        }

        private static PropertyCollection GetLocationByNumber(string number)
        { 
            return gameDefinition.locations.Find(l => l.getProp("number") == number);
        }

        private static List<PropertyCollection> GetItemsByLocation(string locNumber)
        {
            return gameDefinition.items.Where(i => i.getProp("location") == locNumber && i.getProp("carry") != "1").ToList();
        }

        private static List<PropertyCollection> GetMyItems()
        {
            return gameDefinition.items.Where(i => i.getProp("carry") == "1").ToList();
        }

        private static List<PropertyCollection> GetAllItems()
        {
            return gameDefinition.items.ToList();
        }

        private static List<PropertyCollection> GetItemsByFilter(string wPrp, string wCmp, string wVal)
        {
            if (wCmp=="eq")
                return gameDefinition.items.Where(i => i.getProp(wPrp) == wVal).ToList();
            else if (wCmp == "ne")
                return gameDefinition.items.Where(i => i.getProp(wPrp) != wVal).ToList();
            else
                return new List<PropertyCollection>();

        }

        private static PropertyCollection GetItemByName(string name)
        {
            var namedItem = gameDefinition.items.Where(i => i.getProp("title").ToUpper() == name.ToUpper()).FirstOrDefault();
            if (namedItem == null)
            {
                Console.WriteLine("What's that?!");
                return null;
            }
            else
                return namedItem;
        }

        private static bool ReadSettingsFromFile(ref GameDefinition collection, string file)
        {
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                try
                {
                    collection = JsonConvert.DeserializeObject<GameDefinition>(json);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Problems reading config: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Can't find file {file}");
                return false;
            }
        }

        private static void ReadSettings(ref GameDefinition collection, string file)
        {
            var json = GetStringFromResource(file);
            collection = JsonConvert.DeserializeObject<GameDefinition>(json);

            InitializeDefaultStrings(collection);




        }

        private static bool ReadConfig(string customConfig)
        {
            if (string.IsNullOrEmpty(customConfig))
            {
                ReadSettings(ref gameDefinition, "default.json");
                return true;
            }
            else
            {
                if (!customConfig.ToLower().EndsWith(".json"))
                    customConfig = new StringBuilder(customConfig).Append(".json").ToString();

                return ReadSettingsFromFile(ref gameDefinition, customConfig);
            }
        }

        private static void InitializeDefaultStrings(GameDefinition collection)
        {
            foreach(var item in collection.items)
            {
                if (string.IsNullOrEmpty(item.getProp("examinedTitle")))
                    item.setProp("examinedTitle", item.getProp("title"));

                if (string.IsNullOrEmpty(item.getProp("examinedShortDescription")))
                    item.setProp("examinedShortDescription", item.getProp("shortDescription"));
            }
        }

        private static string GetStringFromResource(string resourceName)
        {
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            string text;
            using (var stream = embeddedProvider.GetFileInfo(resourceName).CreateReadStream())
            {
                var reader = new StreamReader(stream);
                text = reader.ReadToEnd();
            }
            return text;
        }
    }

    public static class Util
    {
        public static string FixString(this string s, string objectName, string userLocation)
        {
            return s.Replace("{obj}", objectName).Replace("{userLocation}", userLocation);
        }
    }
}
