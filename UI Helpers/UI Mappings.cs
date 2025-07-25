using Comfort.Common;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MenuHotKeys.UI_Helpers
{
    internal class UI_Mappings
    {
        #region Variables
        // Main Menu UI Elements
        public MenuUI menuUI = null;
        public GameObject playButton = null;
        public GameObject characterButton = null;
        public GameObject tradeButton = null;
        public GameObject hideoutButton = null;
        public GameObject exitButton = null;

        public GameObject[] mainMenuButtons = new GameObject[5];

        // Side selection Menu UI Elements
        public GameObject sideNextButton = null;
        public GameObject sideBackButton = null;

        public GameObject[] sideSelectDefaultButtons = new GameObject[2];

        // Location selection Menu UI Elements
        public GameObject locationNextButton = null;
        public GameObject locationBackButton = null;

        public GameObject[] locationDefaultButtons = new GameObject[2];

        // Offline Raid Menu UI Elements
        public GameObject offlineRaidNextButton = null;
        public GameObject offlineRaidBackButton = null;

        public GameObject[] offlineRaidDefaultButtons = new GameObject[2];

        // Insurance Menu UI Elements
        public GameObject insuranceNextButton = null;
        public GameObject insuranceBackButton = null;

        public GameObject[] insuranceDefaultButtons = new GameObject[2];
        #endregion

        #region Button Setters
        public void setMainMenuButtonsArray()
        {
            mainMenuButtons[0] = playButton;
            mainMenuButtons[1] = characterButton;
            mainMenuButtons[2] = tradeButton;
            mainMenuButtons[3] = hideoutButton;
            mainMenuButtons[4] = exitButton;
        }

        public void setSideDefaultButtonsArray()
        {
            sideSelectDefaultButtons[0] = sideNextButton;
            sideSelectDefaultButtons[1] = sideBackButton;
        }

        public void setLocationDefaultButtonsArray()
        {
            locationDefaultButtons[0] = locationNextButton;
            locationDefaultButtons[1] = locationBackButton;
        }

        public void setOfflineRaidDefaultButtonsArray()
        {
            offlineRaidDefaultButtons[0] = offlineRaidNextButton;
            offlineRaidDefaultButtons[1] = offlineRaidBackButton;
        }

        public void setInsuranceDefaultButtonsArray()
        {
            insuranceDefaultButtons[0] = insuranceNextButton;
            insuranceDefaultButtons[1] = insuranceBackButton;
        }

        public void setMainMenu_Mappings()
        {
            playButton = GameObject.Find("Common UI/Common UI/MenuScreen/PlayButton")?.gameObject;
            characterButton = GameObject.Find("Common UI/Common UI/MenuScreen/CharacterButton")?.gameObject;
            tradeButton = GameObject.Find("Common UI/Common UI/MenuScreen/TradeButton")?.gameObject;
            hideoutButton = GameObject.Find("Common UI/Common UI/MenuScreen/HideoutButton")?.gameObject;
            exitButton = GameObject.Find("Common UI/Common UI/MenuScreen/ExitButtonGroup/ExitButton")?.gameObject;
        }

        public void setSideMenu_Mappings()
        {
            sideNextButton = GameObject.Find("Menu UI/UI/MatchMaker Side Selection Screen/ScreenDefaultButtons/NextButton")?.gameObject;
            sideBackButton = GameObject.Find("Menu UI/UI/MatchMaker Side Selection Screen/ScreenDefaultButtons/BackButton")?.gameObject;
        }

        public void setLocationMenu_Mappings()
        {
            locationNextButton = GameObject.Find("Menu UI/UI/Matchmaker Location Selection/ScreenDefaultButtons/NextButton")?.gameObject;
            locationBackButton = GameObject.Find("Menu UI/UI/Matchmaker Location Selection/ScreenDefaultButtons/BackButton")?.gameObject;
        }

        public void setOfflineRaidMenu_Mappings()
        {
            offlineRaidNextButton = GameObject.Find("Menu UI/UI/Matchmaker Offline Raid Screen/ScreenDefaultButtons/NextButton")?.gameObject;
            offlineRaidBackButton = GameObject.Find("Menu UI/UI/Matchmaker Offline Raid Screen/ScreenDefaultButtons/BackButton")?.gameObject;
        }

        public void setInsuranceMenu_Mappings()
        {
            insuranceNextButton = GameObject.Find("Menu UI/UI/Matchmaker Insurance/ScreenDefaultButtons/NextButton")?.gameObject;
            insuranceBackButton = GameObject.Find("Menu UI/UI/Matchmaker Insurance/ScreenDefaultButtons/BackButton")?.gameObject;
        }
        #endregion

        #region Button Getters
        public DefaultUIButton getButton(GameObject button)
        {
            return button.GetComponentInChildren<DefaultUIButton>();
        }

        public Image getBackground(GameObject button)
        {
            return button.transform.GetChild(1).GetComponent<Image>();
        }

        public TextMeshProUGUI getLabel(GameObject button)
        {
            GameObject _sizeLabel = button.transform.GetChild(2).gameObject;
            return _sizeLabel.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        }

        public Image getIcon(GameObject button)
        {
            GameObject _sizeLabel = button.transform.GetChild(2).gameObject;
            GameObject _iconContainer = _sizeLabel.transform.GetChild(1).gameObject;
            return _iconContainer.transform.GetChild(1).GetComponent<Image>();
        }

        public ScrollRect getScrollRect() 
        {
            if(menuUI == null)
            {
                menuUI = Singleton<MenuUI>.Instance;
            }

            GameObject _traderCardsContainer = menuUI.transform.Find("UI/Trader Screens Group/TopPanel/Container/TraderCards").gameObject;
            return _traderCardsContainer.GetComponent<ScrollRect>();
        }
        #endregion
    }
}
