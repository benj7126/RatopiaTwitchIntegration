using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Newtonsoft.Json;
using UnityEngine;

namespace RatopiaTwitchIntegration
{
    public class TwitchUsers
    {
        const string TwitchIrcUrl = "irc.chat.twitch.tv";
        const int TwitchPort = 6667;

        public int sampleSize => ModBase.Instance.configSampleSize.Value;

        private static System.Random rnd = new System.Random();

        private Dictionary<string, string> userToDisplayName = new Dictionary<string, string>();
        private Dictionary<string, Gender> usernameToDesiredGender = new Dictionary<string, Gender>();
        private Dictionary<string, int> usernameToUnitID = new Dictionary<string, int>();
        // private List<string> bannedUsers = new List<string>();
        // ^^ probably wont be used since 
        // users can only be picked once, ever - i have just decided
        private List<string> userActivity = new List<string>();
        private List<string> pickedUsers = new List<string>(); // basically ban list, but not the same, i say.

        private Dictionary<string, string> awaitingCitizens = new Dictionary<string, string>(); // display-name to username

        public void CreatedCitizens(T_Citizen citizen)
        {
            string displayName = awaitingCitizens[citizen.m_UnitName];

            string username = userActivity[rnd.Next(userActivity.Count)];

            userActivity.Remove(username);
            pickedUsers.Add(username);

            if (!usernameToUnitID.ContainsKey(username)) // might have multiples
                usernameToUnitID.Add(username, citizen.m_ID); // just only let one talk, seems easiest

            citizen.name = displayName;
        }

        public (string, Gender) NewAwaiting()
        {
            List<string> candidates = new List<string>();

            for (int i = 0; i < userActivity.Count; i++)
            {
                string username = userActivity[i];

                if (!awaitingCitizens.ContainsValue(username))
                    candidates.Add(username);

                if (candidates.Count >= sampleSize)
                    break;
            }

            if (candidates.Count == 0)
                candidates = userActivity; // if there are none then what can you do?

            string seletedUsername = candidates[rnd.Next(candidates.Count)];
            string displayName = userToDisplayName[seletedUsername];

            if (!awaitingCitizens.ContainsKey(displayName)) // might have multiples
                awaitingCitizens.Add(displayName, seletedUsername);

            Gender g = rnd.Next(2) == 1 ? Gender.Female : Gender.Male;
            if (usernameToDesiredGender.ContainsKey(seletedUsername))
                g = usernameToDesiredGender[seletedUsername];

            return (displayName, g);
        }

        public void ClearAwaiting()
        {
            awaitingCitizens.Clear();
        }

        private string ReplaceEnding(string path)
        {
            return path.Substring(0, path.Length - 3) + "rti";
        }

        public void SaveTo(string path)
        {
            path = ReplaceEnding(path);

            string json = JsonConvert.SerializeObject((pickedUsers, usernameToDesiredGender, usernameToUnitID));

            File.WriteAllText(path, json);
        }
        public void LoadFrom(string path)
        {
            path = ReplaceEnding(path);

            string data = "";
            try
            {
                data = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Reset();

                ModBase.Instance.mls.LogError(ex.Message);
                return;
            }

            if (data != "")
            {
                (pickedUsers, usernameToDesiredGender, usernameToUnitID) =
                    JsonConvert.DeserializeObject<(List<string>, Dictionary<string, Gender>, Dictionary<string, int>)>(data);
                awaitingCitizens.Clear();
            }
        }
        public void Reset() // when making a new one.
        {
            // TODO: make this be called on new world creation
            awaitingCitizens.Clear();
            pickedUsers.Clear();
            usernameToDesiredGender.Clear();
            usernameToUnitID.Clear();
        }

        public async void Init(string channel, string file) // should be a Task? - the return type
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(TwitchIrcUrl, TwitchPort);

            var stream = tcpClient.GetStream();
            var writer = new StreamWriter(stream) { AutoFlush = true };
            var reader = new StreamReader(stream);

            string nick = $"justinfan{new System.Random().Next(10000, 99999)}";

            // Anonymous login
            await writer.WriteLineAsync("PASS oauth:anonymous");
            await writer.WriteLineAsync($"NICK {nick}");

            // Request tags to get display-name, etc.
            await writer.WriteLineAsync("CAP REQ :twitch.tv/tags");

            // Join channel
            await writer.WriteLineAsync($"JOIN #{channel}");

            while (true)
            {
                string line = await reader.ReadLineAsync();

                if (line.StartsWith("PING"))
                {
                    await writer.WriteLineAsync("PONG :tmi.twitch.tv");
                    continue;
                }

                if (line.Contains("PRIVMSG"))
                {
                    TwitchMessage tm = new TwitchMessage(line);

                    string username = tm.username;
                    if (pickedUsers.Contains(username))
                    {
                        if (!ModBase.Instance.writeMessages.Value)
                            continue;
                        if (!usernameToUnitID.ContainsKey(username))
                            continue;

                        T_Citizen citizen = GameMgr.Instance._T_UnitMgr.FindCitizen(usernameToUnitID[username]);
                        if (citizen != null && ModBase.Instance.messageLimit.Value != 0)
                        {
                            string message = tm.message;

                            if (message.Length > ModBase.Instance.messageLimit.Value && ModBase.Instance.messageLimit.Value > 0)
                                message = message.Substring(0, ModBase.Instance.messageLimit.Value) + "...";

                            GameMgr.Instance._PoolMgr.Pool_GetEffect.GetNextObj().GetComponent<GetEffect>().
                                GetRefEffect("GameScene/UI/UI_Canvas/Icon/Icon_Language", tm.message, citizen, new Vector3(0f, 1f, 0f));
                        }

                        continue;
                    }

                    if (!userToDisplayName.ContainsKey(username))
                    {
                        userToDisplayName[username] = username; // displayname is not always a thing.
                    }

                    if (tm.tags.ContainsKey("display-name"))
                    {
                        userToDisplayName[username] = tm.tags["display-name"]; // if there is a display name, update.
                    }

                    // place user at top of activity
                    if (userActivity.Contains(username))
                        userActivity.Remove(username);

                    userActivity.Add(username);

                    if (tm.message.ToLower() == "m")
                        usernameToDesiredGender[username] = Gender.Male;
                    else if (tm.message.ToLower() == "f")
                        usernameToDesiredGender[username] = Gender.Female;
                }
            }
        }

        public class TwitchMessage
        {
            public Dictionary<string, string> tags = new Dictionary<string, string>();
            public string message;
            public string username;

            public TwitchMessage(string line)
            {
                int i = 0;
                if (line[0] == '@')
                    i = 1;

                string str = "";
                for (; i < line.Length; i++)
                {
                    if (line[i] == ';')
                    {
                        string[] spllit = str.Split('=');
                        tags.Add(spllit[0], spllit[1]);
                        str = "";
                    }
                    else if (line[i] == ' ')
                    {
                        break;
                    }
                    else
                    {
                        str += line[i];
                    }
                }

                i += 2;
                string rest = line.Substring(i);

                int len = rest.IndexOf('!');
                username = rest.Substring(0, len);

                int mssgStart = rest.IndexOf(':') + 1;
                message = rest.Substring(mssgStart);
            }
        }
    }
}