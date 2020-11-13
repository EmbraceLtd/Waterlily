using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Deerwood
{
    class Program
    {
        private static List<Location> locations;
        private static List<Item> items;
        private static int userLocation;
        private static Location myLocation;
        private static bool cont = true;

        static void Main(string[] args)
        {
            InitializeWorld();

            while (cont)
            {
                while (cont)
                {
                    Console.Write("> ");
                    var userCommand = Console.ReadLine();
                    ProcessCommand(userCommand);
                }
                Console.WriteLine("You left this world in a puff of smoke! You are very dead.");
                Console.Write("Revive? (Y/N)");
                var revive = Console.ReadLine().ToUpper();
                if (revive == "Y")
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
                if (!myLocation.showedDescription)
                {
                    Console.WriteLine(myLocation.description);
                    myLocation.showedDescription = true;
                }
                else
                    Console.WriteLine(myLocation.title);

            }
            Console.Write($"You can go {(myLocation.destNorth > -1 ? "north " : string.Empty)}");
            Console.Write($"{(myLocation.destSouth > -1 ? "south " : string.Empty)}");
            Console.Write($"{(myLocation.destEast > -1 ? "east " : string.Empty)}");
            Console.Write($"{(myLocation.destWest > -1 ? "west " : string.Empty)}");
            Console.WriteLine();

            var itemsHere = GetItemsByLocation(userLocation);
            if (itemsHere.Any())
            {
                Console.WriteLine("You can see: ");
                foreach (var item in itemsHere)
                    Console.WriteLine($"   {item.shortDescription}.");
            }
        } 

        private static void InitializeWorld()
        {
            Console.Clear();
            Console.WriteLine("DEERWOOD 1.0");
            Console.WriteLine("(C) Embrace Ltd. of Uggadunk V3.0, 2020");
            Console.WriteLine("=====================================================================================");

            ReadItems();
            ReadLocations();
            userLocation = 1;
            myLocation = GetLocationByNumber(userLocation);
            DescribeWorld();
        }

        private static void ProcessCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                return;

            var sentence = cmd.Split(' ');
            var verb = sentence[0].ToUpper();
            var obj = sentence.Count() > 1 ? sentence[1] : string.Empty;

            if (verb == "EXIT" || verb == "Q")
            {
                cont = false;
            }
            else if ((verb == "LOOK" && obj == string.Empty) || verb == "WHERE")
            {
                DescribeWorld(brief: false);
            }
            else if (verb == "GET" || verb == "TAKE")
            {
                GetAction(obj);
            }
            else if (verb == "FUCK")
            {
                FuckAction(obj);
            }
            else if (verb == "DROP")
            {
                DropAction(obj);
            }
            else if (verb == "INV" || verb == "INVENTORY")
            {
                InventoryAction();
            }
            else if (verb == "EXAMINE" || verb == "LOOK")
            {
                ExamineAction(obj);
            }
            else if (verb == "S" || verb == "SOUTH")
            {
                GoAction(myLocation.destSouth);
            }
            else if (verb == "N" || verb == "NORTH")
            {
                GoAction(myLocation.destNorth);
            }
            else if (verb == "E" || verb == "EAST")
            {
                GoAction(myLocation.destEast);
            }
            else if (verb == "W" || verb == "WEST")
            {
                GoAction(myLocation.destWest);
            }
            else
            {
                Console.WriteLine("You gotta be kidding!");
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
                Console.WriteLine("   Not a damn thing!");

            foreach (var item in GetMyItems())
                Console.WriteLine($"   {item.shortDescription}. ");
        }

        private static void DropAction(string obj)
        {
            if (string.IsNullOrEmpty(obj))
                Console.WriteLine($"Drop what?");
            else
            {
                var item = GetItemByName(obj);
                if (item != null)
                {
                    if (item.carry)
                    {
                        item.carry = false;
                        Console.WriteLine($"You {(item.sensitive ? "very carefully put down" : "dropped")} the {obj}.");
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

        private static void GetAction(string obj)
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
                                if (item.sensitive)
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
                        Console.WriteLine("You can't!");
                    }
                }
            }
        }

        private static void MoveMyItems(int location)
        {
            foreach (var item in GetMyItems())
                item.location = location;
        }

        private static Location GetLocationByNumber(int number)
        {
            return locations.Find(l => l.number == number);
        }

        private static List<Item> GetItemsByLocation(int number)
        {
            return items.Where(i => i.location == number && !i.carry).ToList();
        }

        private static List<Item> GetMyItems()
        {
            return items.Where(i => i.carry).ToList();
        }

        private static Item GetItemByName(string name)
        {
            var namedItem = items.Where(i => i.title.ToUpper() == name.ToUpper()).FirstOrDefault();
            if (namedItem == null)
            {
                Console.WriteLine("What's that?!");
                return null;
            }
            else
                return namedItem;
        }

        private static void ReadItems()
        {
            var json = GetStringFromResource("items.json");
            items = JsonConvert.DeserializeObject<List<Item>>(json);
        }

        private static void ReadLocations()
        {
            var json = GetStringFromResource("locations.json");
            locations = JsonConvert.DeserializeObject<List<Location>>(json);
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
