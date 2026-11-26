using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// Interactive Socketing UI for the Relic / Charm system (Hollow Knight style).
    /// Allows the player to view, equip, and unequip charms into 3 available sockets.
    /// Toggleable via Key [C], Campfire NPC interaction, or the Pause Menu.
    /// </summary>
    public class CharmSocketUI : MonoBehaviour
    {
        public static CharmSocketUI Instance { get; private set; }

        [Header("UI Containers")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private TextMeshProUGUI slotCountText;
        [SerializeField] private Transform charmCardsContainer;
        [SerializeField] private Button closeButton;

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        private readonly List<GameObject> activeCardObjects = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (CharmManager.Instance != null)
            {
                CharmManager.Instance.OnCharmsChanged += RefreshUI;
            }

            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        private void Update()
        {
            bool toggleShop = false;
            bool escapePressed = false;
            bool key1 = false, key2 = false, key3 = false, key4 = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.cKey.wasPressedThisFrame) toggleShop = true;
                if (kb.escapeKey.wasPressedThisFrame) escapePressed = true;
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) key1 = true;
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) key2 = true;
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) key3 = true;
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) key4 = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.C)) toggleShop = true;
            if (Input.GetKeyDown(KeyCode.Escape)) escapePressed = true;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) key1 = true;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) key2 = true;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) key3 = true;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) key4 = true;
#endif

            // Toggle with C key
            if (toggleShop)
            {
                if (isOpen)
                {
                    CloseShop();
                }
                else
                {
                    // Do not open if Game Over or Dialogue is active
                    if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
                    if (DialogueBoxUI.Instance != null && DialogueBoxUI.Instance.gameObject.activeSelf)
                    {
                        var cont = DialogueBoxUI.Instance.transform.Find("Dialogue_Container");
                        if (cont != null && cont.gameObject.activeSelf) return;
                    }

                    OpenShop();
                }
            }

            if (isOpen && escapePressed)
            {
                CloseShop();
            }

            // Quick number keys 1-4 to toggle charms while panel is open
            if (isOpen)
            {
                if (key1) CharmManager.Instance?.ToggleCharm(CharmType.VampiricFang);
                if (key2) CharmManager.Instance?.ToggleCharm(CharmType.ThunderStep);
                if (key3) CharmManager.Instance?.ToggleCharm(CharmType.MagnetRing);
                if (key4) CharmManager.Instance?.ToggleCharm(CharmType.BerserkerCrest);
            }
        }

        public void OpenShop()
        {
            isOpen = true;
            if (modalPanel != null)
            {
                modalPanel.SetActive(true);
            }
            RefreshUI();
        }

        public void CloseShop()
        {
            isOpen = false;
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            if (CharmManager.Instance == null) return;

            int equipped = CharmManager.Instance.EquippedCount;
            if (slotCountText != null)
            {
                slotCountText.text = $"CHARM SOCKETS: <color=#FFD700>{equipped} / {CharmManager.MaxSlots}</color>";
            }

            // Update card button states
            if (charmCardsContainer != null)
            {
                int index = 1;
                foreach (var info in CharmManager.Instance.GetAllCharms())
                {
                    string cardName = $"CharmCard_{info.type}";
                    Transform cardTr = charmCardsContainer.Find(cardName);
                    if (cardTr != null)
                    {
                        bool isEq = CharmManager.Instance.IsEquipped(info.type);
                        var statusTmp = cardTr.Find("Status_Text")?.GetComponent<TextMeshProUGUI>();
                        if (statusTmp != null)
                        {
                            statusTmp.text = isEq 
                                ? "<color=#2ecc71>• EQUIPPED</color>" 
                                : (equipped >= CharmManager.MaxSlots ? "<color=#888888>[NO SLOTS]</color>" : "<color=#e67e22>[CLICK TO EQUIP]</color>");
                        }

                        var borderImg = cardTr.GetComponent<Image>();
                        if (borderImg != null)
                        {
                            borderImg.color = isEq ? new Color(0.18f, 0.45f, 0.28f, 0.95f) : new Color(0.12f, 0.14f, 0.18f, 0.9f);
                        }
                    }
                    index++;
                }
            }
        }

        public void SetReferences(GameObject panel, TextMeshProUGUI slotText, Transform cardsRoot, Button closeBtn)
        {
            modalPanel = panel;
            slotCountText = slotText;
            charmCardsContainer = cardsRoot;
            closeButton = closeBtn;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);
            }
        }
    }
}
