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
        public static ConfigEntry<KeyboardShortcut> MenuUpKey;
        public static ConfigEntry<KeyboardShortcut> MenuDownKey;
        public static ConfigEntry<KeyboardShortcut> MenuSelectKey;
        public static ConfigEntry<KeyboardShortcut> HideOutKey;
        public static ConfigEntry<KeyboardShortcut> PlayerKey;
        public static ConfigEntry<KeyboardShortcut> TradersKey;
        public static ConfigEntry<KeyboardShortcut> FleaMarketKey;
        public static ConfigEntry<KeyboardShortcut> ChatKey;


        public static void Init(ConfigFile Config)
        {
            MenuUpKey = Config.Bind(
                KeybindsSectionTitle,
                ".1. Next Menu Item Up",
                new KeyboardShortcut(KeyCode.W),
                "Highlight the next menu item up"
            );

            MenuDownKey = Config.Bind(
                KeybindsSectionTitle,
                ".2. Next Menu Item Down",
                new KeyboardShortcut(KeyCode.S),
                "Highlight the next menu item down"
            );

            MenuSelectKey = Config.Bind(
                KeybindsSectionTitle,
                ".3. Click Current Menu Item",
                new KeyboardShortcut(KeyCode.Space),
                "Click highlighted menu item"
            );

            NexTabKey = Config.Bind(
                KeybindsSectionTitle,
                ".4. Next tab key",
                new KeyboardShortcut(KeyCode.E),
                "Move to next upper tab in inventory or next trader"
            );

            PrevTabKey = Config.Bind(
                KeybindsSectionTitle,
                ".5. Prev tab key",
                new KeyboardShortcut(KeyCode.Q),
                "Move to previous upper tab in inventory or previous trader"
            );

            TraderTabKey = Config.Bind(
                KeybindsSectionTitle,
                ".6. Trader tab key",
                new KeyboardShortcut(KeyCode.W),
                "Move to next tab in trader menu"
            );

            HideOutKey = Config.Bind(
                KeybindsSectionTitle,
                ".7. Hideout key",
                new KeyboardShortcut(KeyCode.H),
                "Opens the Hideout"
            );
            
            PlayerKey = Config.Bind(
                KeybindsSectionTitle,
                ".8. Player Inventory key",
                new KeyboardShortcut(KeyCode.Alpha1),
                "Opens and closes the Player view"
            );

            TradersKey = Config.Bind(
                KeybindsSectionTitle,
                ".9. Traders key",
                new KeyboardShortcut(KeyCode.Alpha2),
                "Opens and closes the Trader view"
            );

            FleaMarketKey = Config.Bind(
                KeybindsSectionTitle,
                "10. FleaMarket key",
                new KeyboardShortcut(KeyCode.Alpha3),
                "Opens and closes the FleaMarket View"
            );

            ChatKey = Config.Bind(
                KeybindsSectionTitle,
                "11. Chat key",
                new KeyboardShortcut(KeyCode.Alpha4),
                "Opens the Chat"
            );
        }
    }
}
