using BepInEx.Configuration;
using UnityEngine;

namespace MenuHotKeys
{
    internal class Settings
    {
        private const string KeybindsSectionTitle = "Keybinds";

        public static ConfigEntry<KeyboardShortcut> NexTabKey;
        public static ConfigEntry<KeyboardShortcut> PrevTabKey;
        public static ConfigEntry<KeyboardShortcut> TraderTabKey;
        public static ConfigEntry<KeyboardShortcut> HideOutKey;
        public static ConfigEntry<KeyboardShortcut> PlayerKey;
        public static ConfigEntry<KeyboardShortcut> TradersKey;
        public static ConfigEntry<KeyboardShortcut> FleaMarketKey;
        public static ConfigEntry<KeyboardShortcut> ChatKey;


        public static void Init(ConfigFile Config)
        {
            NexTabKey = Config.Bind(
                KeybindsSectionTitle,
                "1. Next tab key",
                new KeyboardShortcut(KeyCode.E),
                "Move to next upper tab in inventory or next trader"
            );

            PrevTabKey = Config.Bind(
                KeybindsSectionTitle,
                "2. Prev tab key",
                new KeyboardShortcut(KeyCode.Q),
                "Move to previous upper tab in inventory or previous trader"
            );

            TraderTabKey = Config.Bind(
                KeybindsSectionTitle,
                "3. Trader tab key",
                new KeyboardShortcut(KeyCode.W),
                "Move to next tab in trader menu"
            );

            HideOutKey = Config.Bind(
                KeybindsSectionTitle,
                "4. Hideout key",
                new KeyboardShortcut(KeyCode.H),
                "Opens the Hideout"
            );
            
            PlayerKey = Config.Bind(
                KeybindsSectionTitle,
                "5. Player Inventory key",
                new KeyboardShortcut(KeyCode.Alpha1),
                "Opens and closes the Player view"
            );

            TradersKey = Config.Bind(
                KeybindsSectionTitle,
                "6. Traders key",
                new KeyboardShortcut(KeyCode.Alpha2),
                "Opens and closes the Trader view"
            );

            FleaMarketKey = Config.Bind(
                KeybindsSectionTitle,
                "7. FleaMarket key",
                new KeyboardShortcut(KeyCode.Alpha3),
                "Opens and closes the FleaMarket View"
            );

            ChatKey = Config.Bind(
                KeybindsSectionTitle,
                "8. Chat key",
                new KeyboardShortcut(KeyCode.Alpha4),
                "Opens the Chat"
            );
        }
    }
}
