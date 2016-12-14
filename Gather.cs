using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using SteamKit2;
using System.Diagnostics;

namespace SteamBot
{
    class Gather
    {
        static string state = "";
        static dynamic gatherInfo_;
        static int delay;

        public static void checkGatherState(bool demand = false)
        {
            try
            {
                delay = 1000;
                dynamic gatherInfo = getGatherInfo();
                string current_status = gatherInfo["state"];

                #region test
                if (demand)
                {
                    Console.WriteLine(
                        "\ncurrent_status: {0}\nstate: {1}\ngatherers: {2}\n",
                        current_status,
                        state,
                        gatherInfo["gatherers"].Count
                        );
                    return;
                }
                #endregion

                switch (current_status)
                {
                    case "election":
                        if (state != "gathering")
                        { break; }
                        gatherServer.AnnounceServer(gatherInfo);
                        state = "election";
                        break;
                    case "selection":
                    case "gathering":
                        int gatherers = gatherInfo["gatherers"].Count;

                        if (gatherers < 8 && state == "selection" && current_status == "gathering")
                        {
                            //Inform joined members only about Server info here.
                            gatherServer.AnnounceServer(gatherInfo);
                            state = "";

                            //If a gather starts, we might as well wait 30 minutes (or more) before checking again. This also avoids the regather issues.
                            delay = 30 * 60 * 1000;
                            break;
                        }

                        if (gatherers >= 8 && current_status != state)
                        {
                            //This should break; in case of regathers - to reduce bot spam. (Depricated)
                            
                            announceGathering(gatherInfo);
                            state = current_status;
                        }
                        if (current_status == "selection")
                        {
                            gatherInfo_ = gatherInfo;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                SimpleLogger.SimpleLog.Error(e.ToString());
            }
            Task.Delay(delay).ContinueWith(x => checkGatherState());
        }
        public static dynamic getGatherInfo()
        {
            string link = "http://gathers.ensl.org/gathers/current";
            string download;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    download = wc.DownloadString(link);
                    return JsonConvert.DeserializeObject(download);
                }
                catch (Exception e)
                {
                    SimpleLogger.SimpleLog.Error(e.ToString());
                    return "";
                }
            }
        }
        public static string returnStatus(string current_state, int gatherers, dynamic ensl_users, SteamID steamID, bool announce = false)
        {
            string announcement = "";

            #region Handles players who have signed up for gather.
            string enslID = steamID.ToString().Replace("STEAM_", "");
            foreach (dynamic user in ensl_users)
            {
                if (user["user"]["steam"]["id"] != enslID) { continue; }

                switch (current_state)
                {
                    case "gathering":
                        announcement = string.Format(
                            "You are signed up for the gather as {0}\n{1}/12 Players\nSee lineup at http://gathers.ensl.org/",
                            user["user"]["username"], gatherers
                            );
                        break;

                    case "election":
                        announcement = string.Format(
                        "Gather is starting!\nYou are signed up for the gather as {0}\nElect captains at http://gathers.ensl.org/",
                            user["user"]["username"]
                            );
                        break;
                    case "selection":
                        announcement = string.Format(
                            "Gather is starting!\nYou are signed up for the gather as {0}\nSee lineup at http://gathers.ensl.org/",
                            user["user"]["username"]
                            );
                        if (user["leader"] == true)
                        {
                            announcement = announcement + "\nYou have been elected as leader, don't forget to select your team!";
                        }
                        break;
                }
            }
            if (announcement != "")
            {
                return announcement;
            }
            #endregion

            switch (current_state)
            {
                case "election":
                case "selection":
                    if (announce == true) { break; }
                    announcement = string.Format(
                        "Gather is starting. Sign up for the next one at http://gathers.ensl.org/"
                        );
                    break;

                case "gathering":
                    announcement = string.Format(
                        "{0}/12 Players\nJoin up at http://gathers.ensl.org/",
                        gatherers
                        );
                    break;
            }
            return announcement;
        }
        public static void announceGathering(dynamic gatherInfo)
        {
            string current_state = gatherInfo["state"];
            gatherInfo = gatherInfo["gatherers"];
            int gatherers = gatherInfo.Count;

            SimpleLogger.SimpleLog.Info(String.Format(
                "Gather announcement initiated:\nCurrent state: {0}\nGatherers: {1}",
                current_state, gatherers.ToString()
                )
                );

            int friendCount = SteamBot.steamFriends.GetFriendCount();
            for (int x = 0; x < friendCount; x++)
            {
                SteamID steamIdFriend = SteamBot.steamFriends.GetFriendByIndex(x);
                //This allows bot to target users more intelligently.
                EPersonaState personaState = SteamBot.steamFriends.GetFriendPersonaState(steamIdFriend);
                bool condition = Json.Config.Msg_Condition(personaState, steamIdFriend);
                if (condition == false) { continue; }

                string announcement = returnStatus(
                    current_state,
                    gatherers,
                    gatherInfo,
                    steamIdFriend,
                    true
                    );
                if (announcement == "") { continue; }
                SteamBot.steamFriends.SendChatMessage(
                    steamIdFriend,
                    EChatEntryType.ChatMsg,
                    announcement
                    );
            }
        }
    }
}
