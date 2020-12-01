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
        private static LastCommand lastCommand = new LastCommand();
        private static bool cont = true;
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
            turnCount = 1;

            if (ReadConfig(customConfig, out string errorMessage))
            {
                InitMessage();
                MainSettings();
                DescribeWorld();
                return true;
            }
            else
            {
                Console.WriteLine($"Error reading game config: {errorMessage}");
                return false;
            }
        }

        private static void InitMessage()
        {
            Console.Clear();
            Console.WriteLine(dashline);
            Console.WriteLine("*");
            foreach (var line in gameDefinition.intro)
                Console.WriteLine($"*  {line}");
            Console.WriteLine("*");
            Console.WriteLine("************   Powered by Waterlily Engine by Tommy Sjöblom  Type 'about' for more info   *********");
            Console.WriteLine();
        }

        private static void MainSettings()
        {
            userLocation = gameDefinition.userLocation;
            myLocation = GetLocationByNumber(userLocation);
            cont = true;
        }

        private static void ProcessPendingActions()
        {
            //do
            //{
                foreach (var p in gameDefinition.triggerActions.Where(a => a.active && !a.completed))
                {
                    if (p.pendingCount-- == 0)
                    {
                        if (ProcessAction(p, p.objectName))
                        {
                            p.completed = true;
                            p.active = false;
                        }
                    }
                }
            //}
            //while (gameDefinition.triggerActions.Where(a => a.active && !a.completed).Any());
            //while (gameDefinition.triggerActions.Where(a => a.active).Any()) ;
        }

        private static void ProcessCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                cmd = "wait";

            var sentence = cmd.Split(' ');
            var verb = sentence[0].ToLower();
            var objName = sentence.Count() > 1 ? sentence[1] : string.Empty;

            lastCommand.Verb = verb;
            lastCommand.Object = objName;

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
            var theAction = gameDefinition.actions.FirstOrDefault(a => a.name == targetAction);
            if (theAction != null)
                ProcessAction(theAction, objectName);
        }

        private static bool ProcessAction(Action theAction, string objectName)
        {
            if (theAction != null)
            {
                foreach (var cond in theAction.conditions)
                {
                    var result = InterpretCondition(cond.condition.FixString(objectName, userLocation), out bool unknown);
                    if (unknown)
                        return false;

                    if (result == false)
                    {
                        if (!string.IsNullOrEmpty(cond.failureMessage))
                            Console.WriteLine(cond.failureMessage.FixString(objectName, userLocation));

                        return false;
                    }
                }

                foreach (var o in theAction.operations)
                    CommitOperation(o.FixString(objectName, userLocation));

                if (!string.IsNullOrEmpty(theAction.completedMessage))
                    Console.WriteLine(theAction.completedMessage.FixString(objectName, userLocation));

                return true;
            }
            return false;
        }

        private static void CommitOperation(string op)
        {
            var p = op.Split('.');
            var ope = p[0];

            if (ope == "set")
            {
                var typ = p[1];
                var obj = p[2];
                var prp = p[3];
                var val = p[4];

                if (typ == "self" && prp == "location")
                {
                    var loc = GetLocationByNumber(userLocation);
                    var dst = val.Replace("{", "").Replace("}", "");
                    var mval = loc.getProp(dst);
                    userLocation = mval;
                }
                else if (typ == "item")
                {
                    var _object = GetItemByName(obj);
                    _object.setProp(prp, val);
                }
                else if (typ == "loc")
                {
                    var loc = GetLocationByNumber(obj);
                    loc.setProp(prp, val);
                }

            }

            if (ope=="clear")
            {
                var typ = p[1];
                var obj = p[2];
                var prp = p[3];
                if (typ == "item")
                {
                    var _object = GetItemByName(obj);
                    _object.setProp(prp, "");
                }
            }

            if (ope == "write")
            {
                var typ = p[1];
                var obj = p[2];
                var prp = p[3];

                if (obj == "*" && typ == "item")
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

                    if (typ == "item")
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

            if (ope=="desc")
            {
                DescribeWorld(brief: false);
            }

            if (ope == "go")
            {
                var prop = p[1].StripBrackets();
                var dest = myLocation.getProp(prop);
                GoAction(dest);
            }

            if (ope == "about")
            {
                ShowGnuLicense();
            }

            if (ope == "die")
            {
                
                if (p.Length > 1)
                {
                    var obj = GetItemByName(p[1]);
                    var loc = obj.getProp(p[2]);
                    cont = (loc != userLocation);
                }
                else
                    cont = false;
            }

            if (ope == "dest")
            {
                ShowDestinations();
            }

            if (ope == "trig")
            {
                var nAct = p[1];
                var tAct = getTriggerAction(nAct);
                if (tAct != null)
                {
                    if (tAct.objectName == lastCommand.Object)
                    {
                        if (!tAct.completed)
                            tAct.active = true;
                    }
                }
            }
        }

        private static bool InterpretCondition(string cnd, out bool unknown)
        {
            unknown = false;

            var p = cnd.Split('.');
            var ope = p[0];

            //get.loc.{userLocation}.destNorth.ne.-1
            if (ope == "if")
            {
                var typ = p[1];
                var obj = p[2];
                var prp = p[3];
                var cmp = p[4];
                var val = p[5];

                if (val == "{empty}")
                    val = "";

                string objVal = string.Empty;

                if (typ == "item")
                {
                    var _object = GetItemByName(obj);
                    if (_object == null) 
                    {
                        unknown = true;
                        return true;
                    }

                    objVal = _object.getProp(prp);
                }

                if (typ == "loc")
                {
                    var loc = GetLocationByNumber(obj);
                    if (loc == null)
                    {
                        unknown = true;
                        return true;
                    }


                    var lval = prp.Replace("{", "").Replace("}", "");
                    objVal = loc.getProp(lval);
                }

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
                if (_object == null)
                {
                    unknown = true;
                    return true;
                }

                var objVal = _object.getProp(prp);
                Console.WriteLine(objVal);
            }

            return false;
        }

        private static TriggerAction getTriggerAction(string actionName)
        {
            return gameDefinition.triggerActions.FirstOrDefault(t => t.name == actionName);
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
            if (!string.IsNullOrEmpty(name))
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
            return null;
        }

        private static bool ReadSettingsFromFile(ref GameDefinition collection, string file, out string errorMessage)
        {
            errorMessage = string.Empty;
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
                    errorMessage = ex.Message;
                    return false;
                }
            }
            else
            {
                errorMessage = $"Can't find file {file}";
                return false;
            }
        }

        private static bool ReadSettings(ref GameDefinition collection, string file, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                var json = GetStringFromResource(file);
                collection = JsonConvert.DeserializeObject<GameDefinition>(json);

                InitializeDefaultStrings(collection);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static bool ReadConfig(string customConfig, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrEmpty(customConfig))
            {
                if (!ReadSettings(ref gameDefinition, "default.json", out errorMessage))
                    return false;
            }
            else
            {
                if (!customConfig.ToLower().EndsWith(".json"))
                    customConfig = new StringBuilder(customConfig).Append(".json").ToString();

                if (!ReadSettingsFromFile(ref gameDefinition, customConfig, out errorMessage))
                    return false;
            }

            return ValidateConfig(out errorMessage);
        }

        private static bool ValidateConfig(out string errorMessage)
        {
            var ret = true;
            var sb = new StringBuilder();

            List<string> dupNames;

            // Items 

            dupNames = gameDefinition.items.GroupBy(t => t.name).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (dupNames.Any())
            {
                sb.AppendLine("Duplicate found in item names:");
                dupNames.ForEach(d => sb.AppendLine($"  {d}"));
                ret = false;
            }

            // Items 

            dupNames = gameDefinition.locations.GroupBy(t => t.name).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (dupNames.Any())
            {
                sb.AppendLine("Duplicate found in location names:");
                dupNames.ForEach(d => sb.AppendLine($"  {d}"));
                ret = false;
            }


            // Trigger actions

            dupNames = gameDefinition.triggerActions.GroupBy(t => t.name).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (dupNames.Any())
            {
                sb.AppendLine("Duplicate found i triggerAction names:");
                dupNames.ForEach(d => sb.AppendLine($"  {d}"));
                ret = false;
            }

            errorMessage = sb.ToString();
            return ret;
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

        private static void ShowGnuLicense()
        {
            Console.WriteLine("WATERLILY Adventure Game Engine");
            Console.WriteLine("Copyright (C) 2020  Tommy Sjöblom");
            Console.WriteLine();
            Console.WriteLine("This program is free software: you can redistribute it and/or modify");
            Console.WriteLine("it under the terms of the GNU General Public License as published by");
            Console.WriteLine("the Free Software Foundation, either version 3 of the License, or");
            Console.WriteLine("(at your option) any later version.");
            Console.WriteLine();
            Console.WriteLine("This program is distributed in the hope that it will be useful,");
            Console.WriteLine("but WITHOUT ANY WARRANTY; without even the implied warranty of");
            Console.WriteLine("MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the");
            Console.WriteLine("GNU General Public License for more details.");
            Console.WriteLine();
            Console.WriteLine("You should have received a copy of the GNU General Public License");
            Console.WriteLine("along with this program.  If not, see <https://www.gnu.org/licenses/>.");
            Console.WriteLine();
            Console.WriteLine("AUTHOR'S NOTICE AND DISCLAIMER");
            Console.WriteLine();
            Console.WriteLine("The Waterlily Engine gives game creators large freedom to create");
            Console.WriteLine("new worlds and distribute game content beyond the author's control.");
            Console.WriteLine();
            Console.WriteLine("I, the author of the Waterlily Game Engine software, will accept");
            Console.WriteLine("NO RESPONSIBILITY whatsoever for the content of games created");
            Console.WriteLine("by others, in the event that players find the content controversial");
            Console.WriteLine("or ethically, morally, politically or in ANY OTHER WAY disturbing.");
            Console.WriteLine();
            Console.WriteLine("Such complaints MUST be directed to the game creator who created the");
            Console.WriteLine("the content in question. Thank you.");
        }
    }

    public static class Util
    {
        public static string FixString(this string s, string objectName, string userLocation)
        {
            if (s == null)
                return null;

            return s.Replace("{obj}", objectName).Replace("{userLocation}", userLocation);
        }

        public static string StripBrackets(this string s)
        {
            return s.Replace("{", "").Replace("}", "");
        }
    }
}
