using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace SteamBot
{
    class gatherServer
    {
        static int mostFrequent(JArray sourceArray)
        {
            //Credit: http://stackoverflow.com/questions/2655759/how-to-get-the-most-common-value-in-an-int-array-c
            
            var values = new Dictionary<int, int>();
            foreach (int id in sourceArray)
            {
                if (values.ContainsKey(id))
                {
                    values[id]++;
                }
                else
                {
                    values.Add(id, 1);
                }
            }
            int mostFreq = 0;
            int count = 0;
            foreach (KeyValuePair<int, int> pair in values)
            {
                if (pair.Value > count)
                {
                    mostFreq = pair.Key;
                    count = pair.Value;
                }
            }
            return mostFreq;
        }

        static dynamic getServerInfo()
        {
            string link = "http://www.ensl.org/api/v1/servers";
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
                    Console.WriteLine(e.Message);
                    return "";
                }
            }
        }

        static JObject serverWinner(int serverID)
        {
            dynamic serverInfo = getServerInfo();

            foreach (dynamic server in serverInfo["servers"])
            {
                if (serverID == Convert.ToUInt32(server.id))
                {
                    return server;
                }
            }
            return new JObject();
        }

        public static dynamic gatherServer_(dynamic gatherInfo)
        {
            JArray serverVotes = new JArray();

            foreach (dynamic user in gatherInfo["gatherers"])
            {
                JArray Votes = user["serverVote"];
                serverVotes.Merge(Votes);
            }

            int serverID = mostFrequent(serverVotes);
            return serverWinner(serverID);
        }

        public static string gatherServerInfo_(dynamic gatherInfo)
        {
            dynamic server = gatherServer_(gatherInfo);

            if (server.ToString() == "{}") { return ""; }

            string name = server["name"];
            string port = ":" + server["port"];
            string password = server["password"];
            string ip = server["ip"];

            string message = string.Format(
                "Gather Started. Join up:\nServer Name: {0}\nsteam://run/4920//+connect%20{1}{2}%20+password%20{3}",
                name,
                ip,
                port,
                password
                );

            return message;
        }

        public static void AnnounceServer(dynamic gatherInfo)
        {
            string message = gatherServerInfo_(gatherInfo);

            if (message == "") { return; }

            int friendCount = SteamBot.steamFriends.GetFriendCount();
            for (int x = 0; x < friendCount; x++)
            {
                SteamID steamIdFriend = SteamBot.steamFriends.GetFriendByIndex(x);
                //This prevents bot from messaging people who are in any other state than "Online" - including busy, away etc.
                EPersonaState personaState = SteamBot.steamFriends.GetFriendPersonaState(steamIdFriend);
                bool condition = Json.Config.Msg_Condition(personaState, steamIdFriend);
                if (condition == false) { continue; }

                dynamic ensl_users = gatherInfo["gatherers"];
                string enslID = steamIdFriend.ToString().Replace("STEAM_", "");
                foreach (dynamic user in ensl_users)
                {
                    if (user["user"]["steam"]["id"] != enslID) { continue; }

                    SteamBot.steamFriends.SendChatMessage(
                        steamIdFriend,
                        EChatEntryType.ChatMsg,
                        message
                    );
                }
            }
        }
    }
}
