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
        private static int userLocation;
        private static Location myLocation;
        private static bool cont = true;
        private static List<PendingAction> pendingActions;
        private static int turnCount;

        static void Main(string[] args)
        {
            InitializeWorld();

            while (cont)
            {
                while (cont)
                {
                    Console.Write($"{turnCount++}> ");
                    var userCommand = Console.ReadLine();
                    ProcessCommand(userCommand);
                    ProcessPendingActions();
                }
                Console.WriteLine("You left this world in a puff of smoke! You are very dead.");
                Console.Write("Revive? (Y/n)");
                var revive = Console.ReadLine().ToUpper();
                if (revive == "Y" || revive == string.Empty)
                {
                    cont = true;
                    InitializeWorld();
                }
            }
        }

        private static void DescribeWorld(bool brief = true)
        {
            if (!brief)
            {
                Console.WriteLine(myLocation.description);
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
                    if (!myLocation.showedDescription)
                    {
                        Console.WriteLine(myLocation.description);
                        myLocation.showedDescription = true;
                    }
                    else
                        Console.WriteLine(myLocation.title);
                }

            }
            ShowDestinations();

            var itemsHere = GetItemsByLocation(userLocation);
            if (itemsHere.Any())
            {
                Console.WriteLine("You can see: ");
                foreach (var item in itemsHere)
                    Console.WriteLine($"   {item.shortDescription}.");
            }
        }

        private static void ShowDestinations()
        {
            Console.Write($"You can go {(myLocation.destNorth > -1 ? "north " : string.Empty)}");
            Console.Write($"{(myLocation.destSouth > -1 ? "south " : string.Empty)}");
            Console.Write($"{(myLocation.destEast > -1 ? "east " : string.Empty)}");
            Console.Write($"{(myLocation.destWest > -1 ? "west " : string.Empty)}");
            Console.WriteLine();
        }

        private static void InitializeWorld()
        {
            pendingActions = new List<PendingAction>();
            turnCount = 1;

            InitMessage();

            ReadConfig();
            MainSettings();
            ShowGameInfo("default ");

            Console.WriteLine("=======================================================================================================");

            DescribeWorld();
        }

        private static void InitMessage()
        {
            Console.Clear();
            Console.WriteLine("=======================================================================================================");
            Console.WriteLine("WATERLILY ADVENTURE ENGINE 1.0");
            Console.WriteLine("BY TOMMY SJÖBLOM / EMBRACE LTD.");
            Console.WriteLine("(C) UGGADUNK v3.0 SOFTWARE, 2020");
            Console.WriteLine("=======================================================================================================");
        }

        private static void ShowGameInfo(string def = "")
        {
            Console.WriteLine($"Running {def}game {gameDefinition.name}");
            Console.WriteLine($"Author: {gameDefinition.author}");
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
            var verb = sentence[0].ToUpper();
            var obj = sentence.Count() > 1 ? sentence[1] : string.Empty;

            switch (verb)
            {
                case "WAIT":
                case "SLEEP":
                    WaitAction(verb);
                    break;
                case "LOAD":
                    LoadAction(obj);
                    break;
                case "EXIT":
                case "Q":
                    cont = false;
                    break;
                case "LOOK" when obj == string.Empty:
                case "WHERE":
                    DescribeWorld(brief: false);
                    break;
                case "GET":
                case "TAKE":
                    PickupAction(obj);
                    break;
                case "FUCK":
                    FuckAction(obj);
                    break;
                case "DROP":
                    DropAction(obj);
                    break;
                case "INV":
                case "INVENTORY":
                    InventoryAction();
                    break;
                case "EXAMINE":
                case "LOOK":
                    ExamineAction(obj);
                    break;
                case "OPEN":
                    OpenAction(obj);
                    break;
                case "BREAK":
                    BreakAction(obj);
                    break;
                case "S":
                case "SOUTH":
                    GoAction(myLocation.destSouth);
                    break;
                case "N":
                case "NORTH":
                    GoAction(myLocation.destNorth);
                    break;
                case "E":
                case "EAST":
                    GoAction(myLocation.destEast);
                    break;
                case "W":
                case "WEST":
                    GoAction(myLocation.destWest);
                    break;
                default:
                    Console.WriteLine("You gotta be kidding!");
                    break;
            }
        }

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
                Console.WriteLine($"The {pendAction.item} explodes in the {loc.title}. A large bang is heard all over town!");
                var safe = GetItemByName("safe");
                safe.isOpen = true;
                safe.longDescription = "The safe has been blown up";
                GetItemByName("money").location = 3;
                GetItemByName("bottle").location = -1;

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

            if (string.IsNullOrEmpty(Path.GetExtension(configPath)))
                configPath = new StringBuilder(configPath).Append(".json").ToString();

            if (File.Exists(configPath))
            {
                ReadSettingsFromFile(ref gameDefinition, configPath);
                InitMessage();
                MainSettings();
                ShowGameInfo();
                Console.WriteLine("=======================================================================================================");
                DescribeWorld();
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
                if (item.location == userLocation)
                {
                    if (item.canOpen)
                    {
                        if (!item.isOpen)
                        {
                            Console.WriteLine($"You open the {item.examinedTitle}.");
                            item.isOpen = true;
                        }
                        else
                        {
                            Console.WriteLine($"The {item.examinedTitle} is already open!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"The {item.examinedTitle} can't be opened.");
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
                if (item.location == userLocation)
                {
                    if (item.canBreak)
                    {
                        var breakItem = GetBreakingTool();
                        if (breakItem != null)
                        {
                            if (item.explosive)
                            {
                                Console.WriteLine($"You break the {item.examinedTitle} with the {breakItem.title}.");
                                Console.WriteLine($"KABOOM! It explodes and you are blown to bits!");
                                cont = false;
                            }
                            else
                            {
                                if (!item.isBroken)
                                {
                                    Console.WriteLine($"You break the {item.examinedTitle} with the {breakItem.title}.");
                                    item.shortDescription = new StringBuilder(item.shortDescription.Insert(2, "broken ")).ToString();
                                    item.longDescription = new StringBuilder(item.longDescription).Replace("closed", "broken").ToString();
                                    item.isBroken = true;

                                    if (userLocation == 1 && obj == "window")
                                    {
                                        myLocation.destNorth = 3;
                                        ShowDestinations();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"The {item.examinedTitle} is already broken!");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"You don't have anything to break the {item.examinedTitle} with.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"The {item.examinedTitle} can't be broken.");
                    }
                }
                else
                {
                    Console.WriteLine("It ain't here!");
                }
            }
        }

        private static void GoAction(int destination)
        {
            if (destination > -1)
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
                if (item.location == userLocation)
                {
                    Console.WriteLine(item.longDescription);
                    item.wasExamined = true;
                    item.shortDescription = item.examinedShortDescription;
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
                Console.WriteLine($"   {item.shortDescription}. ");
        }

        private static Item GetBreakingTool()
        {
            foreach (var item in GetMyItems())
            {
                if (item.canBreakTool)
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
                var myItems = new List<Item>();
                if (obj.ToUpper()=="ALL")
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
                    if (item.carry)
                    {
                        item.carry = false;
                        Console.WriteLine($"You {(item.explosive ? "very carefully put down" : "drop")} the {item.examinedTitle}.");
                        if (item.title == "bottle")
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
                    if (item.location == userLocation)
                    {
                        if (item.title == "bottle")
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
                var itemsHere = new List<Item>();
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
                    if (item.canTake)
                    {
                        if (item.location == userLocation)
                        {
                            if (!item.carry)
                            {
                                item.carry = true;
                                if (item.explosive)
                                {
                                    if (!item.wasExamined)
                                    {
                                        Console.WriteLine($"KABOOM! The {item.examinedTitle} explodes in your hand. Should have been more careful!");
                                        cont = false;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"You very carefully pick up the {item.title}.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"You got the {item.title}!");
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

        private static void MoveMyItems(int location)
        {
            foreach (var item in GetMyItems())
                item.location = location;
        }

        private static Location GetLocationByNumber(int number)
        {
            return gameDefinition.locations.Find(l => l.number == number);
        }

        private static List<Item> GetItemsByLocation(int number)
        {
            return gameDefinition.items.Where(i => i.location == number && !i.carry).ToList();
        }

        private static List<Item> GetMyItems()
        {
            return gameDefinition.items.Where(i => i.carry).ToList();
        }

        private static Item GetItemByName(string name)
        {
            var namedItem = gameDefinition.items.Where(i => i.title.ToUpper() == name.ToUpper()).FirstOrDefault();
            if (namedItem == null)
            {
                Console.WriteLine("What's that?!");
                return null;
            }
            else
                return namedItem;
        }

        private static void ReadSettingsFromFile(ref GameDefinition collection, string file)
        {
            var json = File.ReadAllText(file);
            collection = JsonConvert.DeserializeObject<GameDefinition>(json);
        }

        private static void ReadSettings(ref GameDefinition collection, string file)
        {
            var json = GetStringFromResource(file);
            collection = JsonConvert.DeserializeObject<GameDefinition>(json);

            InitializeDefaultStrings(collection);
        }

        private static void ReadConfig()
        {
            ReadSettings(ref gameDefinition, "default.json");
        }

        private static void InitializeDefaultStrings(GameDefinition collection)
        {
            foreach(var item in collection.items)
            {
                if (string.IsNullOrEmpty(item.examinedTitle))
                    item.examinedTitle = item.title;

                if (string.IsNullOrEmpty(item.examinedShortDescription))
                    item.examinedShortDescription = item.shortDescription;
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
}
