using BepInEx;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static CurrentScreenSingletonClass;
using static EFT.UI.MenuScreen;
using static EFT.UI.TraderScreensGroup;

namespace MenuHotKeys
{
    [BepInPlugin("MenuHotKeysMod", "MenuHotKeys", "1.0.0")]
    [BepInDependency("com.SPT.core", "3.11.0")]
    public class ChangeTabsPlugin : BaseUnityPlugin
    {
        private const string GCLASS_FIELD_NAME = "gclass3521_0";
        private const string ALL_TABS_FIELD_NAME = "tab_0";
        private const string CURRENT_TAB_FIELD_NAME = "tab_2";

        private static FieldInfo _gclass = null;
        private static FieldInfo _servicesScreen = null;
        private static FieldInfo _background = null;
        private static FieldInfo _allTabs = null;
        private static FieldInfo _currentTab = null;
        int currentTraderIndex = 0;
        EEftScreenType eScreenType;
        ETraderMode eTraderMode = ETraderMode.Trade;

        private bool inventoryOpen = false;
        private bool tradersOpen = false;
        private bool fleaOpen = false;
        private bool hideOutOpen = false;

        private bool enableLogging = false;
            
        private void Awake()
        {
            Settings.Init(Config);
        }

        // Button clicking sound because it didn't seem right not to have it
        private void playButtonClick()
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
        }

        // Check if the input field is focused, if so, return true
        private bool isInputFieldFocused()
        {
            if (EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
            {
                if( enableLogging)
                {
                    Logger.LogInfo("Text field in focus.");
                }
                return true;
            }
            return false;
        }

        // Check if the inventory screen is focused, if so, return true
        private bool isInventoryScreenFocus()
        {
            var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
            if(inventoryScreen.isActiveAndEnabled)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("Inventory screen is focused.");
                }
                return true;
            }
            return false;
        }

        // Check if the trader screen is focused, if so, return true
        private bool isTraderScreenFocus()
        {
            var traderScreensGroup = getTraderScreensGroup();
            if (traderScreensGroup.isActiveAndEnabled)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("Trader screen is focused.");
                }
                return true;
            }
            return false;
        }

        // Get Tarkov application instance, if it exists, otherwise return null
        private TarkovApplication getTarkovApplication()
        {
            // OH MY GOD THIS WORKS!!!!
            // It took me two days to figure this out
            TarkovApplication _tarkovApplication;
            if(TarkovApplication.Exist(out _tarkovApplication))

            if (_tarkovApplication == null)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Tarkov application is null");
                }
                Logger.LogInfo("Tarkov application is null");
                return null;
            }
            return _tarkovApplication;
        }   

        // Get the TraderScreensGroup instance, if it exists, otherwise return null
        private TraderScreensGroup getTraderScreensGroup()
        {
            MenuUI menuUI = MenuUI.Instance;
            TraderScreensGroup traderScreensGroup = menuUI.TraderScreensGroup;
            return traderScreensGroup;
        }

        // Get the ServicesScreen instance, if it exists, otherwise return null
        private ServicesScreen getServicesScreen()
        {
            var _traderGroupScreen = getTraderScreensGroup();

            if (_traderGroupScreen == null)
            {
                return null;
            }

            if (!_traderGroupScreen.isActiveAndEnabled)
            {
                if (enableLogging)
                {
                    Logger.LogInfo($"inventory screen is not active");
                }
                return null;
            }

            Type type = typeof(TraderScreensGroup);
            if (_servicesScreen == null)
            {
                _servicesScreen = AccessTools.Field(type, "_servicesScreen");
            }

            return (ServicesScreen)_servicesScreen.GetValue(_traderGroupScreen);
        }

        // Get the current game world, if it is hideout, return true, otherwise return false
        private bool getCurrentGameWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if(gameWorld is HideoutGameWorld)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("Game world is hideout");
                }
                return true;
            }
            if (gameWorld is ClientGameWorld)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("Game world is main player");
                }
                return false;
            }
            return true;
        }

        // Check if the menutaskbar is active, if so check the current screen type
        private bool getButtonInteractable()
        {
            MenuTaskBar menuTaskBar = MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar;

            if (menuTaskBar.isActiveAndEnabled)
            {
                currentScreenSingletonClass = CurrentScreenSingletonClass.Instance;
                eScreenType = currentScreenSingletonClass.CurrentScreenController.ScreenType;

                if (eScreenType == EEftScreenType.MainMenu || eScreenType == EEftScreenType.Inventory || eScreenType == EEftScreenType.Trader || eScreenType == EEftScreenType.Hideout || eScreenType == EEftScreenType.FleaMarket)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        // Check if the current scene is EftMainScene, if so, return false, otherwise return true
        private bool getScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if( enableLogging)
            {
                Logger.LogInfo($"Current scene: {currentScene}");
            }

            if(currentScene == "EftMainScene" || currentScene == "LoginUIScene")
            {
                return false;
            }
            return true;
        }

        // Check if the hideout loading screen is active, if so, return false, otherwise return true
        private bool getHideoutLoading()
        {
            var _hideoutLoadingScreen = MonoBehaviourSingleton<PreloaderUI>.Instance.HideoutLoadingScreen;

            if (_hideoutLoadingScreen == null)
            {
                return true;
            }

            if (!_hideoutLoadingScreen.isActiveAndEnabled)
            {
                if (enableLogging)
                {
                    Logger.LogInfo($"loading screen is not active");
                }
                return  true;
            }

            Type type = typeof(HideoutLoadingScreen);
            if (_background == null)
            {
                _background = AccessTools.Field(type, "_background");
            }

            Image background =  (Image)_background.GetValue(_hideoutLoadingScreen);

            if (background.enabled)
            {
                return false;
            }
            return true;
        }

        // Call handleinput
        private void Update()
        {
            HandleInput();
        }

        // HandleInput method that checks for key presses and performs actions based on them
        private async void HandleInput()
        {
            // Fixes bug with input field not losing focus when pressing escape or enter
            if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (getButtonInteractable() && getCurrentGameWorld())
                {
                    inventoryOpen = false;
                    tradersOpen = false;
                    fleaOpen = false;
                    hideOutOpen = false;
                    eTraderMode = ETraderMode.Trade;
                    await clearSelection(getTraderScreensGroup(), false, 100);
                }
            }

            // Fixes bug with input field not losing focus when pressing space in trader screen
            if(Input.GetKeyDown(KeyCode.Space))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (getButtonInteractable() && getCurrentGameWorld())
                {
                    if (isTraderScreenFocus())
                    {
                        await clearSelection(getTraderScreensGroup(), isTraderScreenFocus(), 100);
                    }
                }
            }

            // Handle tabbing through trader tabs
            if (Input.GetKeyDown(Settings.TraderTabKey.Value.MainKey))
            {
                if(!getScene())
                {
                    return;
                }
                if(!getHideoutLoading())
                {
                    return;
                }
                if (getButtonInteractable() && getCurrentGameWorld())
                {
                    if (!isTraderScreenFocus())
                    {
                        return;
                    }
                    else if (isTraderScreenFocus())
                    {
                        getTraderTab(eTraderMode);
                        playButtonClick();
                        await clearSelection(getTraderScreensGroup(), isTraderScreenFocus(), 100);
                    }
                }
            }

            // Handle tabbing through inventory and trader screens
            if (Input.GetKeyDown(Settings.NexTabKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                // do not trigger if inventory screen is not focused or input field is focused
                if (isInventoryScreenFocus() && !isInputFieldFocused())
                {
                    ShiftTab(+1);
                    playButtonClick();
                }
                if (getButtonInteractable() && getCurrentGameWorld())
                {
                    if (isTraderScreenFocus())
                    { 
                        TraderTabbing(+1);
                        playButtonClick();
                        await clearSelection(getTraderScreensGroup(), isTraderScreenFocus(), 100);
                    }
                }
                else
                {
                    return;
                }
            }

            // Handle tabbing through inventory and trader screens
            if (Input.GetKeyDown(Settings.PrevTabKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                // do not trigger if inventory screen is not focused or input field is focused
                if (isInventoryScreenFocus() && !isInputFieldFocused())
                {
                    ShiftTab(-1);
                    playButtonClick();
                }
                else if (getButtonInteractable() && getCurrentGameWorld())
                {
                    if (isTraderScreenFocus())
                    {
                        TraderTabbing(-1);
                        playButtonClick();
                        await clearSelection(getTraderScreensGroup(), isTraderScreenFocus(), 100);
                    }
                }
                else
                {
                    return;
                }
            }

            // Open and close the hideout screen
            if (Input.GetKeyDown(Settings.HideOutKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (isInputFieldFocused())
                {
                    return;
                }
                if (!getButtonInteractable() || !getCurrentGameWorld())
                {
                    return;
                }
                else
                { 
                    TarkovApplication _tarkovApplication = getTarkovApplication();
                    playButtonClick();

                    if (_tarkovApplication == null)
                    {
                        if (enableLogging)
                        {
                            Logger.LogInfo("Tarkov application is null");
                        }
                        return;
                    }
                    if(!hideOutOpen)
                    {
                        _tarkovApplication.method_53(EMenuType.Hideout, true);

                        inventoryOpen = false;
                        tradersOpen = false;
                        fleaOpen = false;
                        hideOutOpen = true;
                    }
                    else
                    {
                        _tarkovApplication.method_53(EMenuType.Hideout, false);
                        _tarkovApplication.method_53(EMenuType.MainMenu, true);

                        inventoryOpen = false;
                        tradersOpen = false;
                        fleaOpen = false;
                        hideOutOpen = false;
                    }
                }
            }

            // Open and close player inventory
            if (Input.GetKeyDown(Settings.PlayerKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (isInputFieldFocused())
                {
                    return;
                }
                if (!getButtonInteractable() || !getCurrentGameWorld())
                {
                    return;
                }
                else
                {
                    TarkovApplication _tarkovApplication = getTarkovApplication();
                    playButtonClick();

                    if (_tarkovApplication == null)
                    {
                        if (enableLogging)
                        {
                            Logger.LogInfo("Tarkov application is null");
                        }
                        return;
                    }
                    if(!inventoryOpen)
                    {
                        _tarkovApplication.method_53(EMenuType.Player, true);

                        inventoryOpen = true;
                        tradersOpen = false;
                        fleaOpen = false;
                        hideOutOpen = false;
                    }
                    else
                    {
                        _tarkovApplication.method_53(EMenuType.Player, false);

                        inventoryOpen = false;
                        tradersOpen = false;
                        fleaOpen = false;
                        hideOutOpen = false;
                    }
                }
            }

            // Open and close traders screen
            if (Input.GetKeyDown(Settings.TradersKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (isInputFieldFocused())
                {
                    return;
                }
                if (!getButtonInteractable() || !getCurrentGameWorld())
                {
                    return;
                }
                else
                {
                    TarkovApplication _tarkovApplication = getTarkovApplication();
                    playButtonClick();

                    if (_tarkovApplication == null)
                    {
                        if (enableLogging)
                        {
                            Logger.LogInfo("Tarkov application is null");
                        }
                        return;
                    }
                    if(!tradersOpen)
                    {
                        _tarkovApplication.method_53(EMenuType.Trade, true);

                        inventoryOpen = false;
                        tradersOpen = true;
                        fleaOpen = false;
                        hideOutOpen = false;
                        currentTraderIndex = 0; // Reset trader index when opening traders screen
                        eTraderMode = ETraderMode.Trade;

                    }
                    else
                    {
                        
                        _tarkovApplication.method_53(EMenuType.Trade, false);

                        inventoryOpen = false;
                        tradersOpen = false;
                        fleaOpen = false;
                        hideOutOpen = false;
                        currentTraderIndex = 0; // Reset trader index when closing traders screen
                        eTraderMode = ETraderMode.Trade; // Reset trader mode to Trade when closing traders screen
                    }
                }
            }

            // Open and close flea market
            if (Input.GetKeyDown(Settings.FleaMarketKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (isInputFieldFocused())
                {
                    return;
                }
                if (!getButtonInteractable() || !getCurrentGameWorld())
                {
                    return;
                }
                else
                {
                    TarkovApplication _tarkovApplication = getTarkovApplication();
                    playButtonClick();

                    if (_tarkovApplication == null)
                    {
                        if (enableLogging)
                        {
                            Logger.LogInfo("Tarkov application is null");
                        }
                        return;
                    }
                    if(!fleaOpen)
                    {
                        _tarkovApplication.method_53(EMenuType.RagFair, true);

                        inventoryOpen = false;
                        tradersOpen = false;
                        fleaOpen = true;
                        hideOutOpen = false;
                    }
                    else
                    {
                        _tarkovApplication.method_53(EMenuType.RagFair, false);

                        inventoryOpen = false;
                        tradersOpen = false;
                        fleaOpen = false;
                        hideOutOpen = false;
                    }
                }
            }

            // Opens chat
            if (Input.GetKeyDown(Settings.ChatKey.Value.MainKey))
            {
                if (!getScene())
                {
                    return;
                }
                if (!getHideoutLoading())
                {
                    return;
                }
                if (isInputFieldFocused())
                {
                    return;
                }
                if(!getButtonInteractable() || !getCurrentGameWorld())
                {
                    return;
                }
                else
                {
                    TarkovApplication _tarkovApplication = getTarkovApplication();
                    playButtonClick();

                    if (_tarkovApplication == null)
                    {
                        if (enableLogging)
                        {
                            Logger.LogInfo("Tarkov application is null");
                        }
                        return;
                    }

                    _tarkovApplication.method_53(EMenuType.Chat, true);
                }
            }
        }

        // Clears various selections and resets the trader store selections
        private static async Task clearSelection(TraderScreensGroup traderScreensGroup, bool isTraderFocus, int wait)
        {
            await Task.Delay(wait); // Wait for the next frame to ensure the input field loses focus

            bool _isTraderFocus = false;
            _isTraderFocus = isTraderFocus;
            TraderScreensGroup _traderScreensGroup = null;
            _traderScreensGroup = traderScreensGroup;

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            
            if(_traderScreensGroup.TraderClass == null)
            {
                return;
            }

            if(_isTraderFocus == false)
            {
                return;
            }

            if(_isTraderFocus)
            {
                if((_traderScreensGroup.TraderClass.CurrentAssortment == null))
                {
                    return;
                }
                if(_traderScreensGroup.TraderClass.CurrentAssortment.SelectedItem != null)
                {
                    _traderScreensGroup.TraderClass.CurrentAssortment.SelectedItem = null;
                }
                if(_traderScreensGroup.TraderClass.CurrentAssortment.TransactionInProgress == true)
                {
                    _traderScreensGroup.TraderClass.CurrentAssortment.TransactionInProgress = false;
                }
            }
        }

        // Tabs through the inventory tabs
        private void ShiftTab(int shift)
        {
            GClass3521 gclass = GetInventroyScreenGclass();
            if (gclass == null)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("GClass is null");
                }
                return;
            }
            Tab currentTab = GetCurrentTab(gclass);
            Tab[] allTabs = GetAllTabs(gclass);

            int currentTabIndex = -1;
            for (int i = 0; i < allTabs.Length; i++)
            {
                if (allTabs[i].gameObject.name == currentTab.gameObject.name)
                {
                    currentTabIndex = i;
                }
            }

            if(enableLogging)
            {
                Logger.LogInfo($"Current tab index: {currentTab}");
            }

            if (currentTabIndex == -1)
            {
                // do nothing since bad shit happened, probably mod is incompatible anymore
                if(enableLogging)
                {
                    Logger.LogInfo("Could not find current tab index");
                }
                return;
            }

            int shiftedIndex = currentTabIndex + shift;
            if(shiftedIndex >= allTabs.Length)
            {
                shiftedIndex = 0;
            } 
            else if(shiftedIndex < 0)
            {
                shiftedIndex = allTabs.Length - 1;
            }

            SelectTab(gclass, allTabs[shiftedIndex]);
        }

        // Cycles through the selected traders on the trader screen
        private void TraderTabbing(int shift)
        {

            TraderScreensGroup traderScreensGroup = getTraderScreensGroup();
            TraderClass trader = traderScreensGroup.TraderClass;

            if (traderScreensGroup == null)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("TraderScreenGroup is null");
                }
                return;
            }

            IEnumerable<TraderClass> allTraders = GetAllTraders(traderScreensGroup);

            if (currentTraderIndex >= allTraders.Count())
            {
                currentTraderIndex = 0;
            }
            else if (currentTraderIndex < 0)
            {
                currentTraderIndex = allTraders.Count() - 1;
            }

            if (enableLogging)
            {
                Logger.LogInfo($"Number of traders: {allTraders.Count()}");
            }

            if (enableLogging)
            {
                Logger.LogInfo($"Current trader index: {currentTraderIndex}");
            }

            if (currentTraderIndex == -1)
            {
                // do nothing since bad shit happened, probably mod is incompatible anymore
                if (enableLogging)
                {
                    Logger.LogInfo("Could not find current trader index");
                }
                return;
            }
            currentTraderIndex = currentTraderIndex + shift;
            int shiftedIndex = currentTraderIndex;
            if (shiftedIndex >= allTraders.Count())
            {
                currentTraderIndex = 0;
                shiftedIndex = 0;
            }
            else if (shiftedIndex < 0)
            {
                currentTraderIndex = allTraders.Count() - 1;
                shiftedIndex = allTraders.Count() - 1;
            }

            traderScreensGroup.method_6(allTraders.ElementAt(shiftedIndex));

            if (eTraderMode == ETraderMode.Services && !getServicesScreen().CheckAvailableServices(allTraders.ElementAt(shiftedIndex)))
            {
                eTraderMode = ETraderMode.Trade;
            }
        }

        // Get the currently selected trader tab, select next tab based on the trader mode
        private void getTraderTab(ETraderMode traderMode)
        {
            ETraderMode _eTraderMode = traderMode;

            if(_eTraderMode == ETraderMode.Trade)
            {
                setTraderTabs(ETraderMode.Tasks);
            }
            else if(_eTraderMode == ETraderMode.Tasks)
            {
                setTraderTabs(ETraderMode.Services);
            }
            else if (_eTraderMode == ETraderMode.Services)
            {
                setTraderTabs(ETraderMode.Trade);
            }
            else
            {
                setTraderTabs(ETraderMode.Trade);
            }
        }

        // Set the trader tabs
        private void setTraderTabs(ETraderMode traderMode)
        {
            TraderScreensGroup traderScreensGroup = getTraderScreensGroup();
            TraderClass trader = traderScreensGroup.TraderClass;


            if (traderMode == ETraderMode.Trade)
            {
                traderScreensGroup.method_3(ETraderMode.Trade);
                eTraderMode = ETraderMode.Trade;
            }
            else if (traderMode == ETraderMode.Tasks)
            {
                traderScreensGroup.method_3(ETraderMode.Tasks);
                eTraderMode = ETraderMode.Tasks;
            }
            else if (traderMode == ETraderMode.Services && !getServicesScreen().CheckAvailableServices(trader))
            {
                traderScreensGroup.method_3(ETraderMode.Trade);
                eTraderMode = ETraderMode.Trade;
            }
            else if(traderMode == ETraderMode.Services)
            {
                traderScreensGroup.method_3(ETraderMode.Services);
                eTraderMode = ETraderMode.Services;
            }
        }

        // Gets the GClass3521 instance from the InventoryScreen, if it exists, otherwise returns null
        private GClass3521 GetInventroyScreenGclass()
        {
            var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;

            if (inventoryScreen == null)
            {
                return null;
            }

            if (!inventoryScreen.isActiveAndEnabled)
            {
                if(enableLogging)
                {
                    Logger.LogInfo($"inventory screen is not active");
                }
                return null;
            }

            Type type = typeof(InventoryScreen);
            if (_gclass == null)
            {
                _gclass = AccessTools.Field(type, GCLASS_FIELD_NAME);
            }

            return (GClass3521)_gclass.GetValue(inventoryScreen);
        }

        // Gets current tab from the GClass3521 instance
        private Tab GetCurrentTab(GClass3521 gclass)
        {
            Type type = typeof(GClass3521);

            if (_currentTab == null)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("caching type of _currentTab");
                }
                _currentTab = AccessTools.Field(type, CURRENT_TAB_FIELD_NAME);
            }

            return (Tab)_currentTab.GetValue(gclass);
        }

        // Gets all tabs from the GClass3521 instance
        private Tab[] GetAllTabs(GClass3521 gclass)
        {
            Type type = typeof(GClass3521);

            if (_allTabs == null)
            {
                if(enableLogging)
                {
                    Logger.LogInfo("caching type of _allTabs");
                }
                _allTabs = AccessTools.Field(type, ALL_TABS_FIELD_NAME);
            }

            return (Tab[])_allTabs.GetValue(gclass);
        }

        // Gets all traders from the TraderScreensGroup instance
        private IEnumerable<TraderClass> GetAllTraders(TraderScreensGroup traderScreenGroup)
        {

            IEnumerable<TraderClass>traderList = traderScreenGroup.IEnumerable_0;

            traderList.ElementAt(0);


            if (traderList == null)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("caching type of traderList");
                }
            }
            if(enableLogging)
            {
                Logger.LogInfo("Returning traderList");
            }
            return traderList;
        }

        // Selects a tab in the GClass3521 instance
        private void SelectTab(GClass3521 gclass, Tab tab)
        {
            gclass.method_0(tab, true);
        }
    }
}
