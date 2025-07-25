using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using MenuHotKeys.UI_Helpers;
using SPT.Common.Models.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EFT.UI.TraderScreensGroup;

namespace MenuHotKeys
{
    [BepInPlugin("MenuHotKeysMod", "MenuHotKeys", "1.0.0")]
    [BepInDependency("com.SPT.core", "3.11.0")]
    [BepInDependency("com.kaeno.TraderScrolling", BepInDependency.DependencyFlags.SoftDependency)]
    public class MenuHotKeysPlugin : BaseUnityPlugin
    {
        #region Variables
        private const string GCLASS_FIELD_NAME = "gclass3521_0";
        private const string ALL_TABS_FIELD_NAME = "tab_0";
        private const string CURRENT_TAB_FIELD_NAME = "tab_2";

        private UI_Mappings mappings;
        private GameObject selectedButton = null;
        private GameObject[] currentButtons = null;

        private static FieldInfo _gclass = null;
        private static FieldInfo _servicesScreen = null;
        private static FieldInfo _background = null;
        private static FieldInfo _allTabs = null;
        private static FieldInfo _currentTab = null;
        private Vector3 mousePosition = Vector3.zero;
        float scrollFactor = 0.1f;
        float scrollProgress = 0f;
        int currentTraderIndex = 0;
        int currentButtonIndex = 0;
        int traderCount = 0;
        EEftScreenType previousScreenType = EEftScreenType.None;
        EEftScreenType eScreenType;
        ETraderMode eTraderMode = ETraderMode.Trade;
        CurrentScreenSingletonClass currentScreenSingletonClass = null;

        Timer buttonTimer = new Timer(2000);

        private bool traderScrollingInstalled = false;
        private bool inventoryOpen = false;
        private bool tradersOpen = false;
        private bool fleaOpen = false;
        private bool hideOutOpen = false;
        private bool buttonSelected = false;
        private bool buttonPressedBool = false;
        private bool escapePressedBool = false;

        private bool enableLogging = false;
        #endregion

        #region Test Methods
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
                eScreenType = getCurrentScreen();

                if (eScreenType == EEftScreenType.MainMenu || eScreenType == EEftScreenType.Inventory || eScreenType == EEftScreenType.Trader || eScreenType == EEftScreenType.Hideout || eScreenType == EEftScreenType.FleaMarket)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        // Check if the current scene is EftMainScene, if so, return false, otherwise return true
        private bool testScene()
        {
            string currentScene = getCurrentScene();
            if(currentScene == "Unkown")
            {
                return false;
            }

            if (currentScene == "EftMainScene" || currentScene == "LoginUIScene")
            {
                return false;
            }
            return true;
        }

        // Check if screen has changed
        private bool testScreenChange()
        {
            if(previousScreenType == getCurrentScreen())
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

        // Check current screen type
        private EEftScreenType getCurrentScreen()
        {
            if(currentScreenSingletonClass == null)
            {
                currentScreenSingletonClass = CurrentScreenSingletonClass.Instance;
            }

            MenuTaskBar menuTaskBar = MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar;

            if (menuTaskBar.isActiveAndEnabled)
            {
                EEftScreenType _eScreenType = currentScreenSingletonClass.CurrentScreenController.ScreenType;

                if (enableLogging)
                {
                    Logger.LogInfo($"Current screen type: {_eScreenType}");
                }
                return _eScreenType;
            }
            else
            {
                if (enableLogging)
                {
                    Logger.LogInfo("MenuTaskBar is not active, returning None");
                }
                return EEftScreenType.None;
            }
        }

        // Check current scene
        private string getCurrentScene()
        {
            string _currentScene = SceneManager.GetActiveScene().name;
            if (_currentScene == null || _currentScene == string.Empty)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Current scene is null or empty");
                }
                return "Unknown";
            }

            if (enableLogging)
            {
                Logger.LogInfo($"Current scene: {_currentScene}");
            }

            return _currentScene;
        }

        private bool testMouseMovement(Vector3 position)
        {
            mousePosition = Input.mousePosition;
            if (mousePosition != position)
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Get Methods
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
        #endregion

        private void Awake()
        {
            traderScrollingInstalled = Chainloader.PluginInfos.Keys.Contains("com.kaeno.TraderScrolling");
            // Initialize the plugin settings
            Settings.Init(Config);
            mappings = new UI_Mappings();
        }

        // Call handleinput
        private void Update()
        {
            HandleInput();
            setDefaultButton();
        }

        // Button clicking sound because it didn't seem right not to have it
        private void playBottomButtonClick()
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
        }

        private void playButtonClick()
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
        }

        // HandleInput method that checks for key presses and performs actions based on them
        private async void HandleInput()
        {
            // Fixes bug with input field not losing focus when pressing escape or enter
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                escapePressedBool = true;
                //mappings.purgeReferences();
                if (!testScene())
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
                escapePressedBool = true;
                if (!testScene())
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

            if(Input.GetKeyDown(Settings.MenuUpKey.Value.MainKey))
            {
                if (!testScene())
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
                if (EEftScreenType.MainMenu == getCurrentScreen())
                {
                    // Handle menu up key press
                    //playButtonMouseOver();
                    navigateUI(-1, "Vertical");
                }
                else if(EEftScreenType.SelectRaidSide == getCurrentScreen())
                {
                    navigateUI(-1, "Vertical");
                }
                else if(EEftScreenType.SelectLocation == getCurrentScreen())
                {
                    if (mappings.locationNextButton.activeSelf)
                    {
                        navigateUI(-1, "Vertical");
                    }
                }
                else if(EEftScreenType.OfflineRaid == getCurrentScreen())
                {
                    navigateUI(-1, "Vertical");
                }
                else if(EEftScreenType.Insurance == getCurrentScreen())
                {
                    navigateUI(-1, "Vertical");
                }
                else if(EEftScreenType.MatchMakerAccept == getCurrentScreen())
                {
                    navigateUI(-1, "Vertical");
                }
            }

            if (Input.GetKeyDown(Settings.MenuDownKey.Value.MainKey))
            {
                if (!testScene())
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
                if (EEftScreenType.MainMenu == getCurrentScreen())
                {
                    // Handle menu down key press
                    //playButtonMouseOver();
                    navigateUI(+1, "Vertical");
                }
                else if (EEftScreenType.SelectRaidSide == getCurrentScreen())
                {
                    navigateUI(+1, "Vertical");
                }
                else if (EEftScreenType.SelectLocation == getCurrentScreen())
                {
                    if(mappings.locationNextButton.activeSelf)
                    {
                        navigateUI(+1, "Vertical");
                    }
                    return;
                }
                else if (EEftScreenType.OfflineRaid == getCurrentScreen())
                {
                    navigateUI(+1, "Vertical");
                }
                else if (EEftScreenType.Insurance == getCurrentScreen())
                {
                    navigateUI(+1, "Vertical");
                }
                else if (EEftScreenType.MatchMakerAccept == getCurrentScreen())
                {
                    navigateUI(+1, "Vertical");
                }
            }

            if(Input.GetKeyDown(Settings.MenuSelectKey.Value.MainKey))
            {
                if (!testScene())
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
                if(selectedButton == null)
                {
                    return;
                }
                if (EEftScreenType.MainMenu == getCurrentScreen())
                {
                    // Handle menu select key press
                    // Actually performs the click event
                    mappings.getButton(selectedButton).OnClick.Invoke();
                    deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);
                    playButtonClick();
                }
                else if (EEftScreenType.SelectRaidSide == getCurrentScreen())
                {
                    // Handle menu select key press
                    // Actually performs the click event
                    mappings.getButton(selectedButton).OnClick.Invoke();
                    deselectButton(mappings.sideSelectDefaultButtons, currentButtonIndex, false);
                    playButtonClick();
                }
                else if(EEftScreenType.SelectLocation == getCurrentScreen())
                {
                    mappings.getButton(selectedButton).OnClick.Invoke();
                    deselectButton(mappings.locationDefaultButtons, currentButtonIndex, false);
                    playButtonClick();
                }
                else if (EEftScreenType.OfflineRaid == getCurrentScreen())
                {
                    mappings.getButton(selectedButton).OnClick.Invoke();
                    deselectButton(mappings.offlineRaidDefaultButtons, currentButtonIndex, false);
                    playButtonClick();
                }
                else if (EEftScreenType.Insurance == getCurrentScreen())
                {
                    mappings.getButton(selectedButton).OnClick.Invoke();
                    deselectButton(mappings.insuranceDefaultButtons, currentButtonIndex, false);
                    playButtonClick();
                }
                else
                {
                    return;
                }
            }

            // Handle tabbing through trader tabs
            if (Input.GetKeyDown(Settings.TraderTabKey.Value.MainKey))
            {
                if(!testScene())
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
                        playBottomButtonClick();
                        await clearSelection(getTraderScreensGroup(), isTraderScreenFocus(), 100);
                    }
                }
            }

            // Handle tabbing through inventory and trader screens
            if (Input.GetKeyDown(Settings.NexTabKey.Value.MainKey))
            {
                if (!testScene())
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
                    playBottomButtonClick();
                }
                if (getButtonInteractable() && getCurrentGameWorld())
                {
                    if (isTraderScreenFocus())
                    { 
                        TraderTabbing(+1);
                        playBottomButtonClick();
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
                if (!testScene())
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
                    playBottomButtonClick();
                }
                else if (getButtonInteractable() && getCurrentGameWorld())
                {
                    if (isTraderScreenFocus())
                    {
                        TraderTabbing(-1);
                        playBottomButtonClick();
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
                if (!testScene())
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
                    playBottomButtonClick();
                    deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);

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
                if (!testScene())
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
                    playBottomButtonClick();
                    if(EEftScreenType.MainMenu == getCurrentScreen())
                    {
                        deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);
                    }

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
                if (!testScene())
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
                    playBottomButtonClick();
                    deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);

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
                if (!testScene())
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
                    playBottomButtonClick();
                    deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);

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
                if (!testScene())
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
                    playBottomButtonClick();

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

            // Test Key
            //if (Input.GetKeyDown(KeyCode.T))
            //{
            //    //EEftScreenType _eScreenType = getCurrentScreen();
            //    Logger.LogInfo(getCurrentScreen());
            //    Logger.LogInfo(previousScreenType);
            //    Logger.LogInfo(getCurrentScene());
            //}
        }

        #region Utility
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

        private static async Task pauseWait(int wait)
        {
            // A pause method that waits for a specified amount of time
            await Task.Delay(wait);
        }

        private void ButtonTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            buttonPressedBool = false;
            buttonTimer.Stop();
        }
        #endregion

        #region Button Navigation
        private void navigateUI(int change, string layout)
        {
            if(EEftScreenType.MainMenu == getCurrentScreen())
            {
                if (mappings.mainMenuButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting main menu mappings");
                    }
                    mappings.setMainMenu_Mappings();
                    mappings.setMainMenuButtonsArray();
                    currentButtons = mappings.mainMenuButtons;
                    currentButtonIndex = 0;
                    selectButton(mappings.mainMenuButtons, currentButtonIndex, false);
                    return;
                }
                else
                {
                    setDefaultButton();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting main menu mappings");
                
                    }
                }
            }
            else if(EEftScreenType.SelectRaidSide == getCurrentScreen())
            {
                if (mappings.sideSelectDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting side selection mappings");
                    }
                    mappings.setSideMenu_Mappings();
                    mappings.setSideDefaultButtonsArray();
                    currentButtons = mappings.sideSelectDefaultButtons;
                    currentButtonIndex = 0;
                    selectButton(mappings.sideSelectDefaultButtons, currentButtonIndex, false);
                    return;
                }
                else
                {
                    setDefaultButton();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting side selection mappings");
                    }
                }
            }
            else if(EEftScreenType.SelectLocation == getCurrentScreen())
            {
                if (mappings.locationDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting location mappings");
                    }
                    mappings.setLocationMenu_Mappings();
                    mappings.setLocationDefaultButtonsArray();
                    currentButtons = mappings.locationDefaultButtons;
                    currentButtonIndex = 0;
                    if (mappings.locationNextButton.activeSelf)
                    {
                        selectButton(mappings.locationDefaultButtons, currentButtonIndex, false);
                    }
                    else
                    {
                        selectButton(mappings.locationDefaultButtons, 1, false);
                        currentButtonIndex = 1;
                    }
                    return;
                }
                else
                {
                    setDefaultButton();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting location mappings");
                    }
                }
            }
            else if(EEftScreenType.OfflineRaid == getCurrentScreen())
            {
                if (mappings.offlineRaidDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting offline raid mappings");
                    }
                    mappings.setOfflineRaidMenu_Mappings();
                    mappings.setOfflineRaidDefaultButtonsArray();
                    currentButtons = mappings.offlineRaidDefaultButtons;
                    currentButtonIndex = 0;
                    selectButton(mappings.offlineRaidDefaultButtons, currentButtonIndex, false);
                    return;
                }
                else
                {
                    setDefaultButton();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting offline raid mappings");
                    }
                }
            }
            else if(EEftScreenType.Insurance == getCurrentScreen())
            {
                if (mappings.insuranceDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting insurance mappings");
                    }
                    mappings.setInsuranceMenu_Mappings();
                    mappings.setInsuranceDefaultButtonsArray();
                    currentButtons = mappings.insuranceDefaultButtons;
                    currentButtonIndex = 0;
                    selectButton(mappings.insuranceDefaultButtons, currentButtonIndex, false);
                    return;
                }
                else
                {
                    setDefaultButton();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting insurance mappings");
                    }
                }
            }

            int newIndex = currentButtonIndex + change;
            if (newIndex >= currentButtons.Length)
            {
                newIndex = currentButtons.Length - 1;
            }
            else if (newIndex <= 0)
            {
                newIndex = 0;
            }
            if(newIndex != currentButtonIndex)
            {
                deselectButton(currentButtons, currentButtonIndex, true);
            }
            currentButtonIndex = newIndex;
            selectButton(currentButtons, newIndex, true);
        }

        private async void setDefaultButton()
        {
            if(!testScene())
            {
                return;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene")
            {
                return;
            }
            if (!getHideoutLoading())
            {
                return;
            }
            await pauseWait(100);

            // Sets the default button for main menu screen on screen change
            if (EEftScreenType.MainMenu == getCurrentScreen())
            {
                if(!buttonPressedBool)
                {
                    Vector3 position = Input.mousePosition;
                    await pauseWait(100);
                    if (!testMouseMovement(position) && buttonSelected)
                    {
                        await pauseWait(1000);
                        deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);
                    }
                }
                currentButtons = mappings.mainMenuButtons;
                if(escapePressedBool)
                {
                    selectButton(mappings.mainMenuButtons, 0, false);
                    currentButtonIndex = 0;
                }
                if(!testScreenChange())
                {
                    return;
                }
                await pauseWait(100);
                if (mappings.mainMenuButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting main menu mappings");
                    }
                    mappings.setMainMenu_Mappings();
                    mappings.setMainMenuButtonsArray();
                    selectButton(mappings.mainMenuButtons, 0, false);
                    currentButtons = mappings.mainMenuButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    return;
                }
                else
                {
                    deselectButton(mappings.mainMenuButtons, currentButtonIndex, false);
                    selectButton(mappings.mainMenuButtons, 0, false);
                    currentButtons = mappings.mainMenuButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting main menu mappings");
                    }
                }
            }

            // Sets the default button for side selection on screen change
            else if(EEftScreenType.SelectRaidSide == getCurrentScreen())
            {
                if (!buttonPressedBool)
                {
                    Vector3 position = Input.mousePosition;
                    await pauseWait(100);
                    if (!testMouseMovement(position) && buttonSelected)
                    {
                        await pauseWait(1000);
                        deselectButton(mappings.sideSelectDefaultButtons, currentButtonIndex, false);
                    }
                }
                currentButtons = mappings.sideSelectDefaultButtons;
                if (escapePressedBool)
                {
                    selectButton(mappings.sideSelectDefaultButtons, 0, false);
                    currentButtonIndex = 0;
                }
                if (!testScreenChange())
                {
                    return;
                }
                await pauseWait(100);
                if (mappings.sideSelectDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting mappings");
                    }
                    mappings.setSideMenu_Mappings();
                    mappings.setSideDefaultButtonsArray();
                    selectButton(mappings.sideSelectDefaultButtons, 0, false);
                    currentButtons = mappings.sideSelectDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    return;
                }
                else
                {
                    deselectButton(mappings.sideSelectDefaultButtons, currentButtonIndex, false);
                    selectButton(mappings.sideSelectDefaultButtons, 0, false);
                    currentButtons = mappings.sideSelectDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting raid side mappings");
                    }
                }
            }
            else if(EEftScreenType.SelectLocation == getCurrentScreen())
            {
                if (!buttonPressedBool)
                {
                    Vector3 position = Input.mousePosition;
                    await pauseWait(100);
                    if (!testMouseMovement(position) && buttonSelected)
                    {
                        await pauseWait(1000);
                        deselectButton(mappings.locationDefaultButtons, currentButtonIndex, false);
                    }
                }
                currentButtons = mappings.locationDefaultButtons;
                if (escapePressedBool)
                {
                    selectButton(mappings.locationDefaultButtons, 0, false);
                    currentButtonIndex = 0;
                }
                if (!testScreenChange())
                {
                    return;
                }
                await pauseWait(100);
                if (mappings.locationDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting mappings");
                    }
                    mappings.setLocationMenu_Mappings();
                    mappings.setLocationDefaultButtonsArray();
                    selectButton(mappings.locationDefaultButtons, 1, false);
                    currentButtons = mappings.locationDefaultButtons;
                    currentButtonIndex = 1;
                    previousScreenType = getCurrentScreen();
                    return;
                }
                else if(mappings.locationNextButton.activeSelf)
                {
                    deselectButton(mappings.locationDefaultButtons, currentButtonIndex, false);
                    selectButton(mappings.locationDefaultButtons, 0, false);
                    currentButtons = mappings.locationDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting location mappings");
                    }
                }
                else
                {
                    deselectButton(mappings.locationDefaultButtons, currentButtonIndex, false);
                    selectButton(mappings.locationDefaultButtons, 1, false);
                    currentButtons = mappings.locationDefaultButtons;
                    currentButtonIndex = 1;
                    previousScreenType = getCurrentScreen();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping setting location mappings");
                    }
                }
            }
            else if(EEftScreenType.OfflineRaid == getCurrentScreen())
            {
                if (!buttonPressedBool)
                {
                    Vector3 position = Input.mousePosition;
                    await pauseWait(100);
                    if (!testMouseMovement(position) && buttonSelected)
                    {
                        await pauseWait(1000);
                        deselectButton(mappings.offlineRaidDefaultButtons, currentButtonIndex, false);
                    }
                }
                currentButtons = mappings.offlineRaidDefaultButtons;
                if (escapePressedBool)
                {
                    selectButton(mappings.offlineRaidDefaultButtons, 0, false);
                    currentButtonIndex = 0;
                }
                if (!testScreenChange())
                {
                    return;
                }
                await pauseWait(100);
                if (mappings.offlineRaidDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting mappings");
                    }
                    mappings.setOfflineRaidMenu_Mappings();
                    mappings.setOfflineRaidDefaultButtonsArray();
                    selectButton(mappings.offlineRaidDefaultButtons, 0, false);
                    currentButtons = mappings.offlineRaidDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    return;
                }
                else
                {
                    deselectButton(mappings.offlineRaidDefaultButtons, currentButtonIndex, false);
                    selectButton(mappings.offlineRaidDefaultButtons, 0, false);
                    currentButtons = mappings.offlineRaidDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping offline raid mappings");
                    }
                }
            }
            else if(EEftScreenType.Insurance == getCurrentScreen())
            {
                if (!buttonPressedBool)
                {
                    Vector3 position = Input.mousePosition;
                    await pauseWait(100);
                    if (!testMouseMovement(position) && buttonSelected)
                    {
                        await pauseWait(1000);
                        deselectButton(mappings.insuranceDefaultButtons, currentButtonIndex, false);
                    }
                }
                currentButtons = mappings.insuranceDefaultButtons;
                if (escapePressedBool)
                {
                    selectButton(mappings.insuranceDefaultButtons, 0, false);
                    currentButtonIndex = 0;
                }
                if (!testScreenChange())
                {
                    return;
                }
                await pauseWait(100);
                if (mappings.insuranceDefaultButtons[0] == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is null, setting mappings");
                    }
                    mappings.setInsuranceMenu_Mappings();
                    mappings.setInsuranceDefaultButtonsArray();
                    selectButton(mappings.insuranceDefaultButtons, 0, false);
                    currentButtons = mappings.insuranceDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    return;
                }
                else
                {
                    deselectButton(mappings.insuranceDefaultButtons, currentButtonIndex, false);
                    selectButton(mappings.insuranceDefaultButtons, 0, false);
                    currentButtons = mappings.insuranceDefaultButtons;
                    currentButtonIndex = 0;
                    previousScreenType = getCurrentScreen();
                    if (enableLogging)
                    {
                        Logger.LogInfo("UI Mappings is not null, skipping insurance mappings");
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void deselectButton(GameObject[] bArray, int button, bool play)
        {
            if(bArray == null)
            {
                return;
            }
            if(button <= -1 || button > bArray.Length)
            {
                return;
            }
            if (!bArray[button].activeSelf)
            {
                return;
            }
            // Plays button mouse over sound
            if (play)
            {
                mappings.getButton(bArray[button]).OnPointerEnter(new PointerEventData(EventSystem.current));
            }

            // Resets button background color to default state
            if (mappings.getBackground(bArray[button]) == null)
            {
                return;
            }
            mappings.getBackground(bArray[button]).color = new Color(1f, 1f, 1f, 0f);
            // Resets button icon color to default state
            if (mappings.getIcon(bArray[button]) == null)
            {
                return;
            }
            mappings.getIcon(bArray[button]).color = new Color(1f, 1f, 1f, 0f);
            // Resets button label color to default state
            if (mappings.getLabel(bArray[button]) == null)
            {
                return;
            }
            mappings.getLabel(bArray[button]).faceColor = new Color(1f, 1f, 1f, 1f);
            selectedButton = null;
            buttonSelected = false;
            escapePressedBool = false;
        }

        private void selectButton(GameObject[] bArray, int button, bool play)
        {
            if (bArray == null)
            {
                return;
            }
            if (button <= -1 || button > bArray.Length)
            {
                return;
            }
            if (!bArray[button].activeSelf)
            {
                return;
            }
            // Plays button mouse over sound
            if (play)
            {
                mappings.getButton(bArray[button]).OnPointerEnter(new PointerEventData(EventSystem.current));
            }

            // Sets button background color to highlighted state
            if (mappings.getBackground(bArray[button]) == null)
            {
                return;
            }
            mappings.getBackground(bArray[button]).color = new Color(1f, 1f, 1f, 1f);
            // Sets button icon color to highlighted state
            if (mappings.getIcon(bArray[button]) == null)
            {
                return;
            }
            mappings.getIcon(bArray[button]).color = new Color(1f, 1f, 1f, 1f);
            // Sets button label color to highlighted state
            if (mappings.getLabel(bArray[button]) == null)
            {
                return;
            }
            mappings.getLabel(bArray[button]).faceColor = new Color(0f, 0f, 0f, 1f);
            //playBottomButtonClick();
            selectedButton = bArray[button];
            buttonTimer.Elapsed += ButtonTimer_Elapsed;
            buttonTimer.Start();
            buttonSelected = true;
            buttonPressedBool = true;
            escapePressedBool = false;
        }
        #endregion

        #region Inventory Tab Methods
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

        // Selects a tab in the GClass3521 instance
        private void SelectTab(GClass3521 gclass, Tab tab)
        {
            gclass.method_0(tab, true);
        }
        #endregion

        #region Trader Tab Methods
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

            traderCount = allTraders.Count();
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
            if(traderScrollingInstalled) ScrollTraders(shift, traderCount, shiftedIndex);
            
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
        
        private void ScrollTraders(int direction, int count, int index)
        {
            float _scrollLength = 1f;
            int _currentTrader = index + 1;
            scrollFactor = (((_scrollLength / count)) * direction);
            scrollProgress += scrollFactor;

            if (_currentTrader == 1) scrollProgress = 0f;

            if (_currentTrader == count) scrollProgress = 1f;

            //Logger.LogInfo(_scrollLength / count);
            //Logger.LogInfo(scrollProgress);
            //Logger.LogInfo(scrollFactor);
            mappings.getScrollRect().horizontalNormalizedPosition = scrollProgress;
        }
        #endregion
    }
}
