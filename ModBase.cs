using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Numerics;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;

namespace RatopiaTwitchIntegration
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModBase : BaseUnityPlugin
    {
        public ConfigEntry<int> configSampleSize;
        public ConfigEntry<string> configLowerCaseStream;

        public ConfigEntry<bool> writeMessages;
        public ConfigEntry<UnityEngine.Vector3> offset;

        public ConfigEntry<int> messageLimit;

        private const string modGUID = "catNull.RatopiaTwitchIntegration";
        private const string modName = "Ratopia Twitch Integration";
        private const string modVersion = "1.0.0";

        public static ModBase Instance; // Add this
        public ManualLogSource mls;

        public TwitchUsers tUsers = new TwitchUsers();

        private readonly Harmony harmony = new Harmony(modGUID);

        void Awake()
        {
            if (Instance == null)
                Instance = this; // Assign the instance

            configSampleSize = Config.Bind("General", "Sample Size", 100, "Top x latest chatters to pick new citizens from.\nLess than 0 for ANY during this session, note that might create duplicates.");
            configLowerCaseStream = Config.Bind("General", "Steam name", "[stream]", "The stream name in lower case.");
            
            writeMessages = Config.Bind("General", "Write text", true, "Do write chat messages.");
            offset = Config.Bind("General", "Text offset", new UnityEngine.Vector3(0, 1.3f, 0), "Offset to write messages with.");

            messageLimit = Config.Bind("General", "How many characters are allowed in a message", 20, "The amount of characters messages get cut down to, -1 of northing, 0 for no messages.");

            mls = Logger;

            tUsers.Init(configLowerCaseStream.Value, "");

            harmony.PatchAll();
        }
    }
}
