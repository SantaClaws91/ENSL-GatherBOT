using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SteamKit2;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Json
{
    public class Config
    {
        public static dynamic reloadConfig()
        {
            if (!File.Exists("config.cfg"))
            {
                Console.WriteLine("Creating configuration file...");

                StringBuilder sb = new StringBuilder();

                sb.Append("{\r\n");
                sb.Append("    \"Admins\" : [\"\"],\r\n");
                sb.Append("    \"Username\" : \"\",\r\n");
                sb.Append("    \"Password\" : \"\",\r\n");
                sb.Append("}\r\n");

                File.WriteAllText("config.cfg", sb.ToString());
            }
            return JsonConvert.DeserializeObject(File.ReadAllText("config.cfg"));
        }
        
        public static dynamic dumpFriendslist()
        {
            int friendCount = SteamBot.SteamBot.steamFriends.GetFriendCount();
            dynamic friendsList;

            if (!File.Exists("FriendsList.json"))
            {
                friendsList = new JObject();
                friendsList.steamID = new JObject();
            }
            else
            {
                friendsList = JsonConvert.DeserializeObject(File.ReadAllText("FriendsList.json"));
            }

            friendsList.usercount = friendCount;

            for (int x = 0; x < friendCount; x++)
            {
                SteamID steamID = SteamBot.SteamBot.steamFriends.GetFriendByIndex(x);
                string FriendID = steamID.ToString();
                string nick = SteamBot.SteamBot.steamFriends.GetFriendPersonaName(steamID);

                if (friendsList.steamID[FriendID] == null)
                {
                    dynamic obj_name = new JObject();
                    obj_name.name = nick;
                    friendsList.steamID[FriendID] = obj_name;
                }
                if (friendsList.steamID[FriendID]["Message_Conditions"] == null)
                {
                    friendsList.steamID[FriendID]["Message_Conditions"] = "online";
                }
            }
            File.WriteAllText("FriendsList.json", friendsList.ToString());
            return friendsList;
        }

        public static dynamic readFriendsList()
        {
            if (!File.Exists("FriendsList.json"))
            {
                return dumpFriendslist();
            }
            return JsonConvert.DeserializeObject(File.ReadAllText("FriendsList.json"));
        }

        public static void addMsgOpt(dynamic friendsList, SteamID steamID, string opt)
        {
            if (friendsList["steamID"][steamID.ToString()] == null)
            {
                friendsList = dumpFriendslist();
            }
            switch (opt.ToLower())
            {
                case "all":
                case "non":
                case "online":
                case "busy":
                case "away":
                    friendsList["steamID"][steamID.ToString()]["Message_Conditions"] = opt;
                    File.WriteAllText("FriendsList.json", friendsList.ToString());
                    SteamBot.SteamBot.steamFriends.SendChatMessage(
                        steamID,
                        EChatEntryType.ChatMsg,
                        string.Format("Announcement conditions set to: {0}", opt)
                        );
                    return;
            }
            SteamBot.SteamBot.steamFriends.SendChatMessage(
                steamID,
                EChatEntryType.ChatMsg,
                string.Format("Unknown condition: {0}", opt)
                );
        }

        public static bool Msg_Condition(EPersonaState personaState, SteamID steamID)
        {
            string state = personaState.ToString();

            dynamic friendsList = readFriendsList();

            string condition = "online";

            if (friendsList["steamID"][steamID.ToString()] == null)
            {
                friendsList = dumpFriendslist();
            }
            
            condition = friendsList["steamID"][steamID.ToString()]["Message_Conditions"];
            
            switch (condition.ToLower())
            {
                case null:
                    if (state == "Online") { return true; }
                    return false;

                case "non":
                    return false;

                case "all":
                    if (state != "Offline") { return true; }
                    return false;

                case "busy":
                    if (state != "Away" && state != "Offline") { return true; }
                    return false;

                case "away":
                    if (state != "Busy" && state != "Offline") { return true; }
                    return false;

                case "online":
                    if (state == "Online") { return true; }
                    return false;
            }
            return true;
        }

        public static bool isAdmin(SteamID steamID) 
        {
            dynamic config = reloadConfig();

            try
            {
                foreach (dynamic admin in config.Admins)
                {
                    if (admin == steamID)
                    {
                        return true;
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        public static void addAdmin(string steamID)
        {
            dynamic apiInfo = reloadConfig();

            apiInfo.Admins.Add(steamID);

            File.WriteAllText("config.cfg", apiInfo);

            Console.WriteLine("{0} has been added as admin!", steamID);
        }
    }
}
