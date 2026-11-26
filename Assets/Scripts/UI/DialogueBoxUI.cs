using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Platformer.Common;
using Platformer.Managers;
using Platformer.Player;

namespace Platformer.UI
{
    /// <summary>
    /// Interactive typewriter dialogue box system for NPCs, lore obelisks, and story cutscenes.
    /// </summary>
    public class DialogueBoxUI : MonoBehaviour
    {
        public static DialogueBoxUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject dialogContainer;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueBodyText;
        [SerializeField] private TextMeshProUGUI advancePromptText;

        [Header("Settings")]
        [SerializeField] private float typeSpeed = 0.03f;

        private string[] currentLines;
        private int currentLineIndex = 0;
        private bool isTyping = false;
        private Action onDialogueCompleted;
        private Coroutine typeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            if (dialogContainer != null)
            {
                dialogContainer.SetActive(false);
            }
        }

        private void Update()
        {
            if (dialogContainer == null || !dialogContainer.activeSelf) return;

            bool advance = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) advance = true;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) advance = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) advance = true;
#endif

            // Advance dialogue with E, Space, or Left Mouse
            if (advance)
            {
                if (isTyping)
                {
                    // Fast-forward line
                    if (typeRoutine != null) StopCoroutine(typeRoutine);
                    if (dialogueBodyText != null && currentLines != null && currentLineIndex < currentLines.Length)
                    {
                        dialogueBodyText.text = currentLines[currentLineIndex];
                    }
                    isTyping = false;
                }
                else
                {
                    // Next line
                    currentLineIndex++;
                    if (currentLines != null && currentLineIndex < currentLines.Length)
                    {
                        ShowCurrentLine();
                    }
                    else
                    {
                        CloseDialogue();
                    }
                }
            }
        }

        public void StartDialogue(string speaker, string[] lines, Action onComplete = null)
        {
            if (lines == null || lines.Length == 0) return;

            currentLines = lines;
            currentLineIndex = 0;
            onDialogueCompleted = onComplete;

            if (speakerNameText != null) speakerNameText.text = speaker;
            if (dialogContainer != null) dialogContainer.SetActive(true);

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypewriterRoutine(currentLines[currentLineIndex]));
        }

        private IEnumerator TypewriterRoutine(string line)
        {
            isTyping = true;
            if (dialogueBodyText != null) dialogueBodyText.text = "";

            for (int i = 0; i < line.Length; i++)
            {
                if (dialogueBodyText != null) dialogueBodyText.text += line[i];

                if (i % 2 == 0 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("dialogue_blip", 0.45f);
                }

                yield return new WaitForSecondsRealtime(typeSpeed);
            }

            isTyping = false;
        }

        public void CloseDialogue()
        {
            if (dialogContainer != null) dialogContainer.SetActive(false);
            onDialogueCompleted?.Invoke();
            onDialogueCompleted = null;
        }
    }
}
