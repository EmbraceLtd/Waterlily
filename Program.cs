﻿using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace Waterlily
{
    class Program
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        static extern IntPtr GetConsoleWindow();
        static IntPtr ThisConsole = GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static GameDefinition gameDefinition;
        private static string dashline = new string('*', 99);
        private static Location myLocation;
        private static bool cont = true;

        private static int ferryLength = 5;

        //private static int turnCount;
        //private static int gameDefinition.userLocation;
        //private static int userCash;
        //private static int userHealth;
        //private static List<PendingAction> gameDefinition.pendingActions;

        static void Main(string[] args)
        {
            ShowWindow(ThisConsole, 3);
            Print("banner.txt");

            var custom = args.Count() > 0 ? args[0] : string.Empty;
            if (InitializeWorld(custom))
            {
                while (cont)
                {
                    InitFerry();

                    while (cont)
                    {
                        Console.Write($"{gameDefinition.turnCount++}> ");
                        var userCommand = GetCommand();
                        ProcessCommand(userCommand);
                        CheckHealth();
                        ProcessPendingActions();
                    }
                    Console.WriteLine("\r\nWe are sorry for your unfortunate demise.\r\n\r\nGAME OVER\r\n");
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

        private static void InitFerry()
        {
            gameDefinition.pendingActions.Add(new PendingAction { action = "ferryArriveFlatville", location = -1, iterations = ferryLength });
        }

        private static void Print(string resource)
        {
            var text = GetStringFromResource(resource);
            var lines = text.Split(Environment.NewLine);
            foreach (var line in lines)
                Console.WriteLine(line);
        }

        private static string GetCommand()
        {
            string cmd;
            try
            {
                cmd = Reader.ReadLine(30000);
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
                Console.WriteLine(myLocation.description);
                CheckDandelion();
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
                        CheckDandelion();
                        myLocation.showedDescription = true;
                    }
                    else
                        Console.WriteLine(myLocation.title);
                }

            }


            ShowDestinations();

            var itemsHere = GetItemsByLocation(gameDefinition.userLocation);
            if (itemsHere.Any())
            {
                Console.WriteLine("You can see: ");
                foreach (var item in itemsHere)
                    Console.WriteLine($"   {item.shortDescription}.");
            }
        }

        private static void CheckDandelion()
        {
            if (gameDefinition.userLocation == 5)
            {
                if (myLocation.destEast != -1)
                    Console.WriteLine("The ferry S/S Dandelion is moored to the pier.");
                else
                    Console.WriteLine("The ferry isn't here.");
            }

            if (gameDefinition.userLocation == 13)
            {
                if (myLocation.destEast != -1)
                    Console.WriteLine("The ferry S/S Dandelion is moored to the terminal dock.");
                else
                    Console.WriteLine("The ferry isn't here.");
            }

        }

        private static void ShowDestinations()
        {
            var destinations = new List<string>();

            if (myLocation.destNorth > -1) destinations.Add("north");
            if (myLocation.destNorthWest > -1) destinations.Add("northwest");
            if (myLocation.destNorthEast > -1) destinations.Add("northeast");
            if (myLocation.destSouth > -1) destinations.Add("south");
            if (myLocation.destSouthWest > -1) destinations.Add("southwest");
            if (myLocation.destSouthEast > -1) destinations.Add("southeast");
            if (myLocation.destEast > -1) destinations.Add("east");
            if (myLocation.destWest > -1) destinations.Add("west");
            if (myLocation.destUp > -1) destinations.Add("up");
            if (myLocation.destDown > -1) destinations.Add("down");

            if (destinations.Count == 0)
                destinations.Add("nowhere");

            Console.WriteLine($"You can go: {string.Join(", ", destinations)}");
        }

        private static bool InitializeWorld(string customConfig = "")
        {
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

        private static void InitMessage(string loadMessage = "")
        {
            //Console.Clear();
            Console.WriteLine();
            Console.WriteLine();
            foreach (var line in gameDefinition.intro)
                Console.WriteLine($"   {line}");
            Console.WriteLine();
            if (!string.IsNullOrEmpty(loadMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"You resumed your gameplay from {loadMessage}");
            }
            Console.WriteLine();
        }

        private static void MainSettings()
        {
            myLocation = GetLocationByNumber(gameDefinition.userLocation);
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
                    LoadAction(obj, userResume: true);
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
                case "LIST":
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
                case "SW":
                case "SOUTHWEST":
                    GoAction(myLocation.destSouthWest);
                    break;
                case "SE":
                case "SOUTHEAST":
                    GoAction(myLocation.destSouthEast);
                    break;
                case "NW":
                case "NORTHWEST":
                    GoAction(myLocation.destNorthWest);
                    break;
                case "NE":
                case "NORTHEAST":
                    GoAction(myLocation.destNorthEast);
                    break;
                case "U":
                case "UP":
                    GoAction(myLocation.destUp);
                    break;
                case "D":
                case "DOWN":
                    GoAction(myLocation.destDown);
                    break;
                case "TALK":
                case "SPEAK":
                case "SAY":
                case "CONVERSE":
                    TalkAction(obj);
                    break;
                case "BUY":
                    BuyAction(obj);
                    break;
                case "SAVE":
                    SaveGameAction(obj);
                    break;
                case "DIR":
                    DirAction();
                    break;
                case "HELP":
                    Print("help.txt");
                    break;
                case "CHEW":
                    ChewAction(obj);
                    break;
                default:
                    Console.WriteLine("You gotta be kidding!");
                    break;
            }
        }

        private static void ChewAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.location == gameDefinition.userLocation)
                {
                    if (item.canChew)
                    {
                        if (item.carry)
                        {
                            Console.WriteLine($"You chew the {item.title}.{(item.title == "gum" ? " Your breath improves dramatically!" : "")}");
                            gameDefinition.goodBreath = true;
                        }
                        else
                        {
                            Console.WriteLine($"You don't have the {item.title}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"The {item.title} can't be chewed.");
                    }
                }
                else
                {
                    Console.WriteLine($"The {item.title} isn't here.");
                }
            }
        }

        private static void DirAction()
        {
            var files = Directory.GetFiles(".", "waterlily-savegame-*.json");
            Console.WriteLine("Directory of game files:");
            foreach (var file in files)
                Console.WriteLine(GetSavegameFromFilename(file));
        }

        private static string GetSavegameFromFilename(string file)
        {
            return $"   {file.Split('.')[1].Split('-')[2]}";
        }

        private static void CheckHealth()
        {
            if (myLocation != null)
            {
                if (myLocation.number == 8)
                {
                    gameDefinition.userHealth -= 25;
                    if (gameDefinition.userHealth == 25)
                    {
                        Console.WriteLine("You are running out of air. You'd better swim up!");
                    }
                    if (gameDefinition.userHealth <= 0)
                    {
                        Console.WriteLine("You are out of air.");
                        cont = false;
                    }
                }
            }
        }

        private static void ProcessPendingActions()
        {
            foreach (var pendAction in gameDefinition.pendingActions)
            {
                if (pendAction.iterations > 0 && pendAction.active)
                    pendAction.iterations--;

                if (pendAction.iterations == 0)
                {
                    if (pendAction.action == "detonate" && pendAction.active && !pendAction.completed)
                        Detonate(pendAction);

                    if (pendAction.action == "diebyfish" && pendAction.active && !pendAction.completed)
                        DieByFish(pendAction);

                    if (pendAction.action == "ferryArriveFlatville" && pendAction.active && !pendAction.completed)
                        FerryArriveFlatville(pendAction);

                    if (pendAction.action == "ferryLeaveFlatville" && pendAction.active && !pendAction.completed)
                        FerryLeaveFlatville(pendAction);

                    if (pendAction.action == "ferryArriveDeerwood" && pendAction.active && !pendAction.completed)
                        FerryArriveDeerwood(pendAction);

                    if (pendAction.action == "ferryLeaveDeerwood" && pendAction.active && !pendAction.completed)
                        FerryLeaveDeerwood(pendAction);
                }
            }

            gameDefinition.pendingActions.RemoveAll(p => p.completed);

            foreach (var pendAction in gameDefinition.pendingActions)
            {
                if (!pendAction.active)
                    pendAction.active = true;
            }

            if (gameDefinition.pendFerryLeaveFlatville)
            {
                gameDefinition.pendingActions.Add(new PendingAction { action = "ferryLeaveFlatville", location = -1, iterations = 4 });
                gameDefinition.pendFerryLeaveFlatville = false;
            }

            if (gameDefinition.pendFerryArriveDeerwood)
            {
                gameDefinition.pendingActions.Add(new PendingAction { action = "ferryArriveDeerwood", location = -1, iterations = ferryLength });
                gameDefinition.pendFerryArriveDeerwood = false;
            }

            if (gameDefinition.pendFerryLeaveDeerwood)
            {
                gameDefinition.pendingActions.Add(new PendingAction { action = "ferryLeaveDeerwood", location = -1, iterations = 4 });
                gameDefinition.pendFerryLeaveDeerwood = false;
            }

            if (gameDefinition.pendFerryArriveFlatville)
            {
                gameDefinition.pendingActions.Add(new PendingAction { action = "ferryArriveFlatville", location = -1, iterations = ferryLength });
                gameDefinition.pendFerryArriveFlatville = false;
            }
        }

        private static void FerryLeaveFlatville(PendingAction pendAction)
        {
            if (GetLocationByNumber(gameDefinition.userLocation).neighbourhood == "Flatville")
                Console.WriteLine("A steam whistle is heard all over town.");

            if (gameDefinition.userLocation == 11 || gameDefinition.userLocation == 12)
                Console.WriteLine("The ferry leaves the pier. We are underway!");

            if (!GetItemByName("ticket").carry)
                GetItemByName("ticket").location = -1;

            GetLocationByNumber(5).destEast = -1; ;
            GetLocationByNumber(11).destWest=-1;
            GetItemByName("ferry").location = -1;

            if (gameDefinition.userLocation == 5)
            {
                Console.WriteLine("The ferry has left the pier.");
                //DescribeWorld();
            }
            pendAction.active = false;
            pendAction.completed = true;

            gameDefinition.pendFerryArriveDeerwood = true;

        }

        private static void FerryArriveFlatville(PendingAction pendAction)
        {
            var hood = GetLocationByNumber(gameDefinition.userLocation).neighbourhood;
            if (hood == "Flatville" || hood == "Ferry")
                Console.WriteLine("A steam whistle is heard all over town.");

            if (gameDefinition.userLocation == 11 || gameDefinition.userLocation == 12)
                Console.WriteLine("We have arrived in Flatville!");

            GetLocationByNumber(5).destEast = 11;
            GetLocationByNumber(11).destWest = 5;
            GetItemByName("ferry").location = 5;

            if (gameDefinition.userLocation == 5)
            {
                Console.WriteLine("The ferry has arrived at the pier.");
                //DescribeWorld();
            }
            pendAction.active = false;
            pendAction.completed = true;

            gameDefinition.pendFerryLeaveFlatville = true;

        }

        private static void FerryArriveDeerwood(PendingAction pendAction)
        {
            var hood = GetLocationByNumber(gameDefinition.userLocation).neighbourhood;
            if (hood == "Deerwood" || hood=="Ferry")
                Console.WriteLine("A steam whistle is heard all over town.");

            if (gameDefinition.userLocation == 11 || gameDefinition.userLocation == 12)
                Console.WriteLine("We have arrived in Deerwood!");

            GetLocationByNumber(13).destEast = 11;
            GetLocationByNumber(11).destWest = 13;
            GetItemByName("ferry").location = 13;

            if (gameDefinition.userLocation == 13)
            {
                Console.WriteLine("The ferry has arrived in the terminal.");
                //DescribeWorld();
            }
            pendAction.active = false;
            pendAction.completed = true;

            gameDefinition.pendFerryLeaveDeerwood = true;

        }

        private static void FerryLeaveDeerwood(PendingAction pendAction)
        {
            if (GetLocationByNumber(gameDefinition.userLocation).neighbourhood == "Deerwood")
                Console.WriteLine("A steam whistle is heard all over town.");

            if (gameDefinition.userLocation == 11 || gameDefinition.userLocation == 12)
                Console.WriteLine("The ferry leaves the terminal. We are underway!");

            if (!GetItemByName("ticket").carry)
                GetItemByName("ticket").location = -1;

            GetLocationByNumber(13).destEast = -1;
            GetLocationByNumber(11).destWest = -1;
            GetItemByName("ferry").location = -1;

            if (gameDefinition.userLocation == 13)
            {
                Console.WriteLine("The ferry has left the terminal.");
                //DescribeWorld();
            }
            pendAction.active = false;
            pendAction.completed = true;

            gameDefinition.pendFerryArriveFlatville = true;

        }

        private static void Detonate(PendingAction pendAction)
        {
            if (pendAction.item == "bottle")
            {
                var loc = GetLocationByNumber(pendAction.location);
                Console.WriteLine($"{Kaboom()}The {pendAction.item} explodes in {(pendAction.location == 10 ? "" : "the ")}{loc.title}. A large bang is heard all over town!");

                if (pendAction.location == 3)
                {
                    var safe = GetItemByName("safe");
                    safe.isOpen = true;
                    safe.longDescription = "The safe has been blown up";
                    GetItemByName("money").location = 3;
                    GetItemByName("bottle").location = -1;
                }

                if (pendAction.location == 5)
                {
                    var man = GetItemByName("man");
                    man.shortDescription = "a dead old man";
                    man.longDescription = "The old man has died from the explosion.";
                    man.canTalk = false;
                    loc.description = "You are on the ferry pier. An dead old man is lying on a bench.";
                    GetItemByName("bottle").location = -1;
                }

                if (pendAction.location == 10)
                {
                    var man = GetItemByName("eli");
                    man.shortDescription = "Eli's dead body";
                    man.longDescription = "Eli was a stout man in his 40s. He lies dead on the floor.";
                    man.canTalk = false;
                    loc.description = "You are in Eli's Store. Eli's body lies behind the counter.";
                    GetItemByName("bottle").location = -1;
                    GetItemByName("gum").canBuy = false;
                    GetItemByName("gum").canTake = true;
                    GetItemByName("gum").location = 10;
                    GetItemByName("jewel").canBuy = false;
                    GetItemByName("jewel").canTake = true;
                    GetItemByName("jewel").location = 10;
                    GetItemByName("nails").canBuy = false;
                    GetItemByName("nails").canTake = true;
                    GetItemByName("nails").location = 10;
                }

                if (pendAction.location == gameDefinition.userLocation)
                {
                    Console.WriteLine("You are blown to bits!");
                    cont = false;
                }

                pendAction.active = false;
                pendAction.completed = true;
            }
        }

        private static void DieByFish(PendingAction pendAction)
        {
            Console.WriteLine($"You're getting tired of swimming. Suddenly something pulls you under water.\r\nA large fish with razor sharp teeth appears and smiles at you.\r\nIt opens its mouth and waits for a while before biting a big chunk out of your stomach.");
            cont = false;
        }

        private static void WaitAction(string verb)
        {
            Console.WriteLine($"You {verb.ToLower()}. Time passes.");
        }

        private static void LoadAction(string obj, bool userResume = false)
        {
            if (userResume)
                obj = new StringBuilder($".\\waterlily-savegame-{obj}.json").ToString();

            var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), obj);

            if (string.IsNullOrEmpty(Path.GetExtension(configPath)))
                configPath = new StringBuilder(configPath).Append(".json").ToString();

            if (File.Exists(configPath))
            {
                if (InitializeWorld(configPath))
                {
                    //InitMessage(userResume ? configPath : String.Empty);
                    //MainSettings();
                    //DescribeWorld();
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
                if (item.location == gameDefinition.userLocation)
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
                    Console.WriteLine($"The {item.title} isn't here.");
                }
            }
        }

        private static void BuyAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.location == gameDefinition.userLocation)
                {
                    if (item.canBuy)
                    {
                        if (!item.carry)
                        {
                            if (gameDefinition.userCash >= item.price)
                            {
                                Console.WriteLine($"You buy the {item.examinedTitle}.");
                                item.carry = true;
                                gameDefinition.userCash -= item.price;
                            }
                            else
                            {
                                Console.WriteLine($"Eli says: I'm sorry son, you don't seem to have enough money to buy the {item.title}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"You already have the {item.examinedTitle}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{(item.namedPerson?"":"The ")}{item.examinedTitle} is not for sale.");
                    }
                }
                else
                {
                    Console.WriteLine($"The {item.title} isn't here.");
                }
            }
        }

        private static void TalkAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.location == gameDefinition.userLocation)
                {
                    Console.WriteLine($"You talk to {(item.namedPerson ? "":"the ")}{item.title}.");

                    if (item.canTalk)
                    {
                        if (item.phraseIndex + 1 > item.phrases.Count)
                            item.phraseIndex = 0;


                        Console.WriteLine($"{(item.namedPerson ? "" : "The ")}{item.title} says: {item.phrases[item.phraseIndex]}");

                        if (item.title=="Eli")
                        {
                            if (item.phraseIndex == 2 && GetItemByName("gum").location == -1)
                                GetItemByName("gum").location = 10;

                            if (item.phraseIndex == 3 && GetItemByName("nails").location == -1)
                                GetItemByName("nails").location = 10;

                            if (item.phraseIndex == 4 && GetItemByName("jewel").location == -1)
                                GetItemByName("jewel").location = 10;
                        }

                        item.phraseIndex++;
                    }
                    else
                    {
                        Console.WriteLine("Nothing happens.");
                    }
                }
                else
                {
                    Console.WriteLine($"{(item.namedPerson ? "" : "The ")}{item.title} isn't here.");
                }
            }
        }

        private static void BreakAction(string obj)
        {
            var item = GetItemByName(obj);
            if (item != null)
            {
                if (item.location == gameDefinition.userLocation)
                {
                    if (item.canBreak)
                    {
                        var breakItem = GetBreakingTool();
                        if (breakItem != null)
                        {
                            if (item.explosive)
                            {
                                Console.WriteLine($"You break the {item.examinedTitle} with the {breakItem.title}.");
                                Console.WriteLine($"{Kaboom()}It explodes and you are blown to bits!");
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

                                    if (gameDefinition.userLocation == 1 && obj == "window")
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
                    Console.WriteLine($"The {item.title} isn't here.");
                }
            }
        }

        private static string Kaboom()
        {
            return "\r\n            KABOOM\r\n\r\n";
        }
        private static void GoAction(int destination)
        {
            if (destination > -1)
            {
                if (destination == 6 && myLocation.number == 8)
                {
                    Console.WriteLine("You gasp for air!");
                    gameDefinition.userHealth = 100;
                }

                if (destination == 6 && myLocation.number == 5)
                    Console.WriteLine("You jump into the river and start swimming.");

                if (destination == 8 && myLocation.number == 6)
                    Console.WriteLine("You take a deep breath and dive down.");

                if (destination == 11 && myLocation.number == 5)
                {
                    var ticket = GetItemByName("ticket");
                    if (ticket.carry)
                    {
                        Console.WriteLine("You embark the Dandelion.");
                    }
                    else
                    {
                        Console.WriteLine("You try to embark the ferry.");
                        Console.WriteLine("The ticket controller says: You don't have a ticket, mister! Just two dollars a piece.");
                        ticket.location = 5;
                        return;
                    }
                }  

                if (destination == 5 && myLocation.number == 11)
                    Console.WriteLine("You disembark the Dandelion.");

                if (destination == 7)
                    gameDefinition.pendingActions.Add(new PendingAction { action = "diebyfish", location = gameDefinition.userLocation, iterations = 8 });

                gameDefinition.userLocation = destination;
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
                if (item.location == gameDefinition.userLocation)
                {
                    if (item.title == "money")
                        Console.WriteLine($"{gameDefinition.userCash} dollar{(gameDefinition.userCash > 1 ? "s" : "")}");
                    else
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

            if (!myItems.Any() && gameDefinition.userCash == 0)
                Console.WriteLine("   not a damn thing!");

            foreach (var item in GetMyItems())
                if (item.title != "money")
                    Console.WriteLine($"   {item.shortDescription}. ");

            if (gameDefinition.userCash > 0)
                Console.WriteLine($"   {gameDefinition.userCash} dollar{(gameDefinition.userCash > 1 ? "s" : "")}");
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
                            gameDefinition.pendingActions.Add(new PendingAction { action = "detonate", item = "bottle", location = gameDefinition.userLocation});
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
                    if (item.location == gameDefinition.userLocation)
                    {
                        if (item.title == "bottle")
                        {
                            Console.WriteLine($"{Kaboom()}The bottle of nitroglycerine explodes in your tender parts. Should have been more careful!");
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
                    itemsHere = GetItemsByLocation(gameDefinition.userLocation);
                }
                else
                {
                    var item = GetItemByName(obj);
                    if (item != null)
                        itemsHere.Add(item);
                }

                foreach (var item in itemsHere)
                {
                    if (item.canTake || item.canBuy)
                    {
                        if (item.location == gameDefinition.userLocation)
                        {
                            if (!item.carry)
                            {
                                if (item.explosive)
                                {
                                    if (!item.wasExamined)
                                    {
                                        Console.WriteLine($"{Kaboom()}The {item.examinedTitle} explodes in your hand. Should have been more careful!");
                                        cont = false;
                                    }
                                    else
                                    {
                                        item.carry = true;
                                        Console.WriteLine($"You very carefully pick up the {item.title}.");
                                        var loc = GetLocationByNumber(2);
                                        loc.description = "You are in a park. Birds are singing and doing whatever birds do.";
                                    }
                                }
                                else
                                {
                                    if (item.canTake)
                                    {
                                        item.carry = true;
                                        Console.WriteLine($"You got the {item.title}!");

                                        if (item.title == "money")
                                            gameDefinition.userCash += 100;
                                    }
                                    else if (item.canBuy)
                                    {
                                        if (item.location == 10 && gameDefinition.userLocation == 10)
                                        {
                                            Console.WriteLine($"Eli says: You have to pay for the {item.title} my friend! That'll be {item.price} dollar{(item.price > 1 ? "s" : "")}!");
                                        }

                                        if ((item.location == 5 && gameDefinition.userLocation == 5) || (item.location == 13 && gameDefinition.userLocation == 13))
                                        {
                                            Console.WriteLine($"The ticket controller says: Well, you must pay for the {item.title} mister! It's {item.price} dollar{(item.price > 1 ? "s" : "")}!");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("You got it already!");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"The {item.title} isn't here.");
                        }
                    }
                    else
                    {
                        if (obj.ToUpper() != "ALL")
                            Console.WriteLine($"You can't take the {item.title}!");
                    }
                }
            }
        }

        private static void SaveGameAction(string obj)
        {
            var filename = $".\\waterlily-savegame-{obj}.json";
            if (!File.Exists(filename))
            {
                File.WriteAllText(filename, JsonConvert.SerializeObject(gameDefinition));
                Console.WriteLine($"You saved your game progress to {obj}.json");
            }
            else
            {
                Console.Write($"A save game with the name {obj} already exists. Overwrite? (y/N)");
                var resp = Console.ReadLine();
                if (resp.ToUpper() == "Y")
                {
                    File.WriteAllText(filename, JsonConvert.SerializeObject(gameDefinition));
                    Console.WriteLine($"You saved your game progress to {obj}.json");
                }
                else
                {
                    Console.WriteLine("The game was not saved.");
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

        private static bool ReadSettingsFromFile(ref GameDefinition collection, string file)
        {
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                collection = JsonConvert.DeserializeObject<GameDefinition>(json);
                return true;
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
