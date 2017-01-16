using System;

using SteamKit2;

namespace SteamBot
{
    class UserHandler
    {
        public static void Announcer(string message)
        {
            int friendCount = SteamBot.steamFriends.GetFriendCount();

            for (int x = 0; x < friendCount; x++)
            {
                SteamID steamIdFriend = SteamBot.steamFriends.GetFriendByIndex(x);
                //This allows bot to target messages at the right people
                EPersonaState personaState = SteamBot.steamFriends.GetFriendPersonaState(steamIdFriend);
                bool condition = Json.Config.Msg_Condition(personaState, steamIdFriend);
                if (condition == false) { continue; }

                SteamBot.steamFriends.SendChatMessage(
                    steamIdFriend,
                    EChatEntryType.ChatMsg,
                    message
                    );
            }
            SimpleLogger.SimpleLog.Info(message);
        }

        static void chatCommands(string command, string Message, SteamID steamID)
        {
            dynamic gatherInfo;
            dynamic friendsList;
            string[] args;
            switch (command.ToLower())
            {
                //Commands go here: Prefix "!" for user commands, Prefix "#" for admin commands.
                //Commands must be written in *_lowercase_*. To the user commands won't be case-sensitive.
                //Please remember to return; each case.
                #region Announce message to all in friendslist
                case "#announce":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") { return; }

                    Console.WriteLine("Bot is attempting to announce: {0}\n", args[1]);
                    Announcer(args[1]);

                    return;
                #endregion

                #region Add Admin
                case "#addadmin":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") { return; }

                    Json.Config.addAdmin(args[1]);

                    SteamBot.steamFriends.SendChatMessage(
                        steamID,
                        EChatEntryType.ChatMsg,
                        "User was added as admin. ID: " + args[1]
                        );
                    return;
                #endregion

                #region Reload Config
                case "#reloadconfig":
                    Json.Config.reloadConfig();
                    SteamBot.steamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg, "Config was reloaded by user");
                    Console.WriteLine("Config was reloaded by user: {0}", steamID);
                    return;
                #endregion

                #region Request Gather Status
                case "!info":
                case "!status":
                    gatherInfo = Gather.getGatherInfo();
                    string current_state = gatherInfo["state"];
                    int gatherers = gatherInfo["gatherers"].Count;
                    gatherInfo = gatherInfo["gatherers"];
                    string status = Gather.returnStatus(
                        current_state,
                        gatherers,
                        gatherInfo,
                        steamID,
                        false
                        );
                    SteamBot.steamFriends.SendChatMessage(
                        steamID,
                        EChatEntryType.ChatMsg,
                        status
                        );
                    return;
                #endregion

                #region Help
                case "!help":
                    dynamic config = Json.Config.reloadConfig();
                    string helpText = config["!help"];
                    SteamBot.steamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg, 
                        helpText
                        );
                    return;
                #endregion

                #region Am I admin?
                case "!amiadmin":
                    SteamBot.steamFriends.RequestFriendInfo(steamID, EClientPersonaStateFlag.PlayerName);
                    string name = SteamBot.steamFriendsName;

                    Json.Config.reloadConfig();
                    if (Json.Config.isAdmin(steamID) == true)
                    {
                        SteamBot.steamFriends.SendChatMessage(
                            steamID,
                            EChatEntryType.ChatMsg,
                            name + " has admin privileges!"
                            );
                        return;
                    }
                    SteamBot.steamFriends.SendChatMessage(
                        steamID,
                        EChatEntryType.ChatMsg,
                        name + " does not have admin privileges!"
                        );
                    return;
                #endregion

                #region Add Friend
                case "#addfriend":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") { return; }

                    Console.WriteLine("Attempting to add friend: {0}", args[1]);

                    SteamBot.steamFriends.AddFriend(Convert.ToUInt64(args[1]));
                    return;
                #endregion

                #region Set Persona Name
                case "#personaname":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") { return; }

                    SteamBot.steamFriends.SetPersonaName(args[1]);
                    return;
                #endregion

                #region Hello
                case "!hello":
                    SteamBot.steamFriends.RequestFriendInfo(steamID, EClientPersonaStateFlag.PlayerName);

                    name = SteamBot.steamFriends.GetFriendPersonaName(steamID);
                    SteamBot.steamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg, string.Format("Hello {0}! I'm awake!", name));
                    return;
                #endregion

                #region Mute/Unmute
                case "!mute":
                    friendsList = Json.Config.readFriendsList();
                    Json.Config.addMsgOpt(friendsList, steamID, "non");
                    return;

                case "!unmute":
                    friendsList = Json.Config.readFriendsList();
                    Json.Config.addMsgOpt(friendsList, steamID, "all");
                    return;
                #endregion

                #region Dump Friendslist
                case "#dumpfriendslist":
                    Json.Config.dumpFriendslist();
                    return;
                #endregion

                #region Simulate Gather Announcement
                case "#gathertest":
                    args = separate(2, ' ', Message);
                    if (args[0] == "-1") { return; }

                    gatherInfo = Gather.getGatherInfo();
                    dynamic enslusers = gatherInfo["gatherers"];

                    string gatherTest = Gather.returnStatus(
                        args[2],
                        Convert.ToInt32(args[1]),
                        enslusers,
                        steamID,
                        false
                        );
                    SteamBot.steamFriends.SendChatMessage(
                        steamID,
                        EChatEntryType.ChatMsg,
                        gatherTest
                        );

                    return;
                #endregion

                #region Check Gather Info Print
                case "#checkgather":
                    Gather.checkGatherState(true);
                    return;
                #endregion

                #region Announce Gather Status
                case "#anngather":
                    gatherInfo = Gather.getGatherInfo();
                    Gather.announceGathering(gatherInfo);
                    return;
                #endregion

                #region Edit Message Conditions
                case "!msgconditions":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") {
                        string str = "!msgconditions [option]\nOnline: Only announce if your personastatus on steam is set to \"Online\"(Default setting)\nAway: Announce if your personastatus on steam is set to \"Online\" or \"Away\"\nBusy: Announce if your personastatus on steam is set to \"Online\" or \"Busy\"\nAll: Announce if your personastatus on steam is set to anything other than \"Offline\"\nNon: Disable gather announcing";
                        SteamBot.steamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg,
                        str
                        );
                        return;
                    }

                    friendsList = Json.Config.readFriendsList();
                    Json.Config.addMsgOpt(friendsList, steamID, args[1]);
                    return;
                #endregion

                #region Server Voting Test
                case "#testserver":
                    Console.WriteLine("Testing server...");
                    gatherInfo = Gather.getGatherInfo();
                    gatherServer.AnnounceServer(gatherInfo);
                    return;
                    #endregion
            }
            SteamBot.steamFriends.SendChatMessage(
                steamID,
                EChatEntryType.ChatMsg,
                string.Format("Unknown command: {0}", command)
                );
            string friend = SteamBot.steamFriends.GetFriendPersonaName(steamID);
            Console.WriteLine("User {0} used an uknown command: {1}", friend, command);
        }

        static void groupChatCommands(string command, string Message, SteamID steamID, SteamID chatID)
        {
            string[] args;
            string name;
            switch (command.ToLower())
            {
                //Commands go here: "!" for user commands, "#" for admin commands.
                #region Announce message to all in friendslist
                case "#announce":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") { return; }

                    Console.WriteLine("Bot is attempting to announce: {0}\n", args[1]);

                    int friendCount = SteamBot.steamFriends.GetFriendCount();

                    for (int x = 0; x < friendCount; x++)
                    {
                        SteamID steamIdFriend = SteamBot.steamFriends.GetFriendByIndex(x);
                        EPersonaState personaState = SteamBot.steamFriends.GetFriendPersonaState(steamIdFriend);

                        //This prevents bot from messaging people who are in any other state than "Online" - including busy, away etc.
                        if (personaState.ToString() == "Online")
                        {
                            SteamBot.steamFriends.SendChatMessage(steamIdFriend, EChatEntryType.ChatMsg, args[1]);
                            Console.WriteLine("{0} : {1}", steamIdFriend.Render(), args[1]);
                        }
                    }
                    break;
                #endregion

                #region Add Admin
                case "#addadmin":
                    args = separate(1, ' ', Message);
                    if (args[0] == "-1") { return; }

                    Json.Config.addAdmin(args[1]);

                    SteamBot.steamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg,
                        "User was added as admin. ID: " + args[1]
                        );
                    break;
                #endregion

                #region Reload Config
                case "#reloadconfig":
                    Json.Config.reloadConfig();
                    SteamBot.steamFriends.SendChatRoomMessage(chatID, EChatEntryType.ChatMsg, "Config was reloaded by user");
                    Console.WriteLine("Config was reloaded by user: {0}", steamID);
                    break;
                #endregion

                #region Request Gather Status
                case "!status":


                    break;
                #endregion

                #region Am I admin?
                case "!amiadmin":
                    Json.Config.reloadConfig();
                    SteamBot.steamFriends.RequestFriendInfo(steamID, EClientPersonaStateFlag.PlayerName);
                    name = SteamBot.steamFriendsName;

                    if (Json.Config.isAdmin(steamID) == true)
                    {
                        SteamBot.steamFriends.SendChatRoomMessage(chatID, EChatEntryType.ChatMsg, name + " has admin privileges!");
                    }
                    else
                    {
                        SteamBot.steamFriends.SendChatRoomMessage(chatID, EChatEntryType.ChatMsg, name + " does not have admin privileges!");
                    }
                    break;
                #endregion

                #region Add Friend
                case "#addfriend":
                    args = separate(1, ' ', Message);

                    Console.WriteLine("Attempting to add friend: {0}", args[1]);

                    SteamBot.steamFriends.AddFriend(Convert.ToUInt64(args[1]));
                    break;
                #endregion

                #region Set Persona Name
                case "#personaname":
                    args = separate(1, ' ', Message);
                    SteamBot.steamFriends.SetPersonaName(args[1]);
                    break;
                    #endregion
            }
        }

        public static void steamBotCommandsHandling(string Message, SteamID steamID, SteamID chatID)
        {
            string command = Message;

            if (Message.Length > 1)
            {
                if (Message.Remove(1) == "#")
                {
                    Json.Config.reloadConfig();
                    if (Json.Config.isAdmin(steamID) == false)
                    {
                        Console.WriteLine("User attempted administrator command: {0}, without proper privileges.", Message);
                        return;
                    }
                }
                if (Message.Remove(1) == "!" || Message.Remove(1) == "#")
                {
                    if (Message.Contains(" "))
                    {
                        command = Message.Remove(Message.IndexOf(' '));
                    }
                    if (chatID == 0)
                        chatCommands(command, Message, steamID);

                    else
                        groupChatCommands(command, Message, steamID, chatID);
                }
            }
        }

        static string[] separate(int nargs, char seperator, string myString)
        {
            string[] returned = new string[4];

            int i = 0;
            int error = 0;
            int Length = myString.Length;

            foreach (char c in myString)
            {
                if (i != nargs)
                {
                    if (error > Length || nargs > 5)
                    {
                        returned[0] = "-1";
                        return returned;
                    }
                    else if (c == seperator)
                    {
                        returned[i] = myString.Remove(myString.IndexOf(c));
                        myString = myString.Remove(0, myString.IndexOf(c) + 1);
                        i++;
                    }
                    error++;

                    if (error == Length && i != nargs)
                    {
                        returned[0] = "-1";
                        return returned;
                    }
                }
                else
                {
                    returned[i] = myString;
                }
            }
            return returned;
        }
    }
}
