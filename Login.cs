using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using SteamKit2;
using Newtonsoft.Json;


namespace SteamBot
{
    class SteamBot
    {
        static SteamClient steamClient;
        static CallbackManager manager;

        static SteamUser steamUser;

        public static SteamFriends steamFriends;

        static bool isRunning;

        static string user, pass, authCode, twoFactorAuth;

        public static string steamFriendsName;

        static void Main()
        {
            dynamic config = Json.Config.reloadConfig();

            user = config.Username;
            pass = config.Password;
            
            if (user == "" || pass == "")
            {
                Console.Write("Username: ");
                user = Console.ReadLine();

                Console.Write("Password: ");
                pass = Console.ReadLine();

                config.Username = user;
                config.Password = pass;

                File.WriteAllText("config.cfg", config.ToString());
            }

            steamClient = new SteamClient();

            manager = new CallbackManager(steamClient);

            steamFriends = steamClient.GetHandler<SteamFriends>();

            steamUser = steamClient.GetHandler<SteamUser>();

            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);

            manager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
            manager.Subscribe<SteamFriends.FriendAddedCallback>(OnFriendAdded);
            manager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendsList);

            manager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMsg);

            manager.Subscribe<SteamFriends.ChatInviteCallback>(OnGroupChatInvite);
            manager.Subscribe<SteamFriends.ChatMsgCallback>(OnGroupChatMsg);
            manager.Subscribe<SteamFriends.ChatEnterCallback>(OnGroupChatEnter);

            manager.Subscribe<SteamFriends.ProfileInfoCallback>(OnProfileInfo);
            manager.Subscribe<SteamFriends.PersonaStateCallback>(OnPersonaState);

            isRunning = true;

            Console.WriteLine("Connecting to Steam...");
            steamClient.Connect();
            
            while (isRunning)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to connect to Steam: {0}", callback.Result);

                isRunning = false;
                return;
            }

            Console.WriteLine("Connected to Steam! Logging in '{0}'...", user);

            byte[] sentryHash = null;
            if (File.Exists("sentry.bin"))
            {
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = user,
                Password = pass,
                
                AuthCode = authCode,

                TwoFactorCode = twoFactorAuth,

                SentryFileHash = sentryHash,
            }
            );
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {

            Console.WriteLine("Disconnected from Steam, reconnecting in 5...");

            Thread.Sleep(5000);

            steamClient.Connect();
        }

        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2FA = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;

            if (isSteamGuard || is2FA)
            {
                Console.WriteLine("This account is SteamGuard protected!");

                if ( is2FA )
                {
                    Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                    twoFactorAuth = Console.ReadLine();
                }
                else
                {
                    Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
                    authCode = Console.ReadLine();
                }

                return;
            }

            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                isRunning = false;
                return;
            }

            Console.WriteLine("Successfully logged on!");

//            DBNation's Steam group chat:
//            steamFriends.JoinChat(110338190880311047);
//          Bitey's Steam group chat:
//            steamFriends.JoinChat(110338190877848457);
//            ENSL Group chat:
            steamFriends.JoinChat(103582791429543017);

            Gather.checkGatherState();
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating sentryfile...");

            int fileSize;
            byte[] sentryHash;
            using (var fs = File.Open("sentry.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek( callback.Offset, SeekOrigin.Begin );
                fs.Write( callback.Data, 0, callback.BytesToWrite );
                fileSize = (int)fs.Length;

                fs.Seek( 0, SeekOrigin.Begin );
                using (var sha = new SHA1CryptoServiceProvider())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = fileSize,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,

                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });

            Console.WriteLine("Done!");
        }

        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            steamFriends.SetPersonaState(EPersonaState.Online);
        }

        static void OnFriendAdded(SteamFriends.FriendAddedCallback callback)
        {
            string personaName = callback.PersonaName;
            SteamID steamID = callback.SteamID;

            Console.WriteLine("{0} is now a friend of {1}'s", personaName, user);
            
            string Greetings = string.Format(
                "Hello {0}. Thank you for adding the bot.\nUse !info to request gather information or wait for the automated responses.",
                personaName
                );

            steamFriends.SendChatMessage(
                steamID,
                EChatEntryType.ChatMsg,
                Greetings
                );
        }

        static void OnFriendsList(SteamFriends.FriendsListCallback callback)
        {
            foreach (var friend in callback.FriendList)
            {
                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                {
                    steamFriends.AddFriend(friend.SteamID);
                }
            }
        }

        static void OnFriendMsg(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.EntryType == EChatEntryType.ChatMsg)
            {
                UserHandler.steamBotCommandsHandling(callback.Message, callback.Sender, 0);
            }
        }

        static void OnGroupChatMsg(SteamFriends.ChatMsgCallback callback)
        {
            if (callback.ChatMsgType == EChatEntryType.ChatMsg)
            {
//                Console.WriteLine("Message received in {0}'s group chat: {1} says: {2}", callback.ChatRoomID, callback.ChatterID, callback.Message);
                UserHandler.steamBotCommandsHandling(callback.Message, callback.ChatterID, callback.ChatRoomID);
            }
        }

        static void OnGroupChatInvite(SteamFriends.ChatInviteCallback callback)
        {
            steamFriends.JoinChat(callback.ChatRoomID);
            Console.WriteLine(user + " has been invited to " + callback.ChatRoomName + "'s group chat: "+ callback.ChatRoomID);
        }

        static void OnGroupChatEnter(SteamFriends.ChatEnterCallback callback)
        {
            Console.WriteLine("Successfully entered: {0}'s group chat: {1}\n", callback.ChatRoomName, callback.ChatID);
        }

        static void OnProfileInfo(SteamFriends.ProfileInfoCallback callback)
        {
            return;
        }

        static void OnPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            steamFriendsName = callback.Name;
        }
        
    }
}