using System;
using UnityEngine;
using TMPro;
using Platformer.Player;
using Platformer.UI;

namespace Platformer.Common
{
    /// <summary>
    /// Old Wanderer NPC resting by the campfire.
    /// Interacting shares vital lore, boss strategies, and restores the player's health & energy!
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CampfireNPC : MonoBehaviour
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcName = "Old Wanderer";
        [SerializeField] private float interactionRadius = 2.5f;

        [Header("Dialogue Content")]
        [SerializeField]
        [TextArea(2, 4)]
        private string[] dialogueLines = new string[]
        {
            "Greetings, traveler. You have braved the perilous chasms to reach this sanctum.",
            "Deep within the fortress sleeps Ignis, the Volcanic Titan. His rocky hide is impenetrable, but timing your Parries and deflecting his own magma back at him will crack his armor!",
            "Here, drink this elixir of vitality. May your blade strike true!"
        };

        [Header("Prompt Visual")]
        [SerializeField] private GameObject promptObject;

        private Transform playerTransform;
        private bool isPlayerNearby = false;
        private bool hasGifted = false;

        private void Start()
        {
            if (promptObject != null)
            {
                promptObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
                return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool wasNearby = isPlayerNearby;
            isPlayerNearby = dist <= interactionRadius;

            if (promptObject != null && wasNearby != isPlayerNearby)
            {
                promptObject.SetActive(isPlayerNearby);
            }

            if (isPlayerNearby)
            {
                bool interact = false;
                bool openCharms = false;
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) interact = true;
                    if (kb.cKey.wasPressedThisFrame) openCharms = true;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F)) interact = true;
                if (Input.GetKeyDown(KeyCode.C)) openCharms = true;
#endif

                if (interact)
                {
                    TriggerDialogue();
                }
                else if (openCharms)
                {
                    CharmSocketUI.Instance?.OpenShop();
                }
            }
        }

        private void TriggerDialogue()
        {
            if (DialogueBoxUI.Instance != null)
            {
                DialogueBoxUI.Instance.StartDialogue(npcName, dialogueLines, OnDialogueFinished);
            }
        }

        private void OnDialogueFinished()
        {
            if (!hasGifted && playerTransform != null)
            {
                hasGifted = true;

                // Restore full player health
                Health health = playerTransform.GetComponent<Health>();
                if (health != null)
                {
                    health.Heal(health.MaxHealth);
                }

                // Restore full player energy
                PlayerEnergy energy = playerTransform.GetComponent<PlayerEnergy>();
                if (energy != null)
                {
                    energy.AddEnergy(100f);
                }

                UI.FloatingTextManager.ShowDamage(playerTransform.position, 0, true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
