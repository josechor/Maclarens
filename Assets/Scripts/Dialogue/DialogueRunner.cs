using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueRunner : MonoBehaviour
{
    private const string PlayerSpeakerName = "Tú";

    public static DialogueRunner Instance { get; private set; }

    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Button continueButton;

    public bool IsActive { get; private set; }

    // Sigue en true un frame más que IsActive, para que Interactor no reprocese
    // la misma pulsación de E que acaba de cerrar el diálogo y lo reabra al instante.
    public bool IsBlockingInteraction { get; private set; }

    private DialogueData data;
    private int currentIndex;
    private int selectedChoiceIndex;
    private TopDownController playerController;
    private bool skipInputThisFrame;

    private bool awaitingPlayerLine;
    private int pendingNextIndex;

    public void ConfigureButtons(Button[] choices, Button continueBtn)
    {
        choiceButtons = choices;
        continueButton = continueBtn;
    }

    private void Awake()
    {
        Instance = this;

        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null)
                {
                    continue;
                }

                int index = i;
                choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(index));
            }
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartDialogue(DialogueData dialogue, GameObject player)
    {
        if (dialogue == null || dialogue.nodes == null || dialogue.nodes.Count == 0)
        {
            Debug.LogWarning("DialogueRunner.StartDialogue: dialogue nulo o sin nodos.");
            return;
        }

        if (DialogueUI.Instance == null)
        {
            Debug.LogError("DialogueRunner.StartDialogue: no hay ningún DialogueUI en la escena.");
            return;
        }

        data = dialogue;
        currentIndex = 0;
        IsActive = true;
        IsBlockingInteraction = true;
        skipInputThisFrame = true;
        awaitingPlayerLine = false;

        playerController = player != null ? player.GetComponent<TopDownController>() : null;
        if (playerController != null)
        {
            playerController.InputEnabled = false;
        }

        ShowCurrentNode();
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        if (skipInputThisFrame)
        {
            skipInputThisFrame = false;
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool confirmPressed = keyboard.eKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;

        if (awaitingPlayerLine)
        {
            if (confirmPressed)
            {
                OnContinueClicked();
            }
            return;
        }

        DialogueNode node = data.nodes[currentIndex];

        if (node.choices != null && node.choices.Count > 0)
        {
            int count = node.choices.Count;

            if (WasPreviousPressed(keyboard))
            {
                selectedChoiceIndex = (selectedChoiceIndex - 1 + count) % count;
                DialogueUI.Instance.SetSelectedChoice(selectedChoiceIndex);
                return;
            }

            if (WasNextPressed(keyboard))
            {
                selectedChoiceIndex = (selectedChoiceIndex + 1) % count;
                DialogueUI.Instance.SetSelectedChoice(selectedChoiceIndex);
                return;
            }

            if (confirmPressed)
            {
                OnChoiceClicked(selectedChoiceIndex);
                return;
            }

            for (int i = 0; i < count && i < 4; i++)
            {
                if (IsChoiceKeyPressed(keyboard, i))
                {
                    OnChoiceClicked(i);
                    return;
                }
            }
        }
        else if (confirmPressed)
        {
            OnContinueClicked();
        }
    }

    private void LateUpdate()
    {
        IsBlockingInteraction = IsActive;
    }

    private void OnChoiceClicked(int index)
    {
        if (!IsActive || awaitingPlayerLine)
        {
            return;
        }

        DialogueNode node = data.nodes[currentIndex];
        if (node.choices == null || index < 0 || index >= node.choices.Count)
        {
            return;
        }

        DialogueChoice choice = node.choices[index];
        pendingNextIndex = choice.nextNodeIndex;
        awaitingPlayerLine = true;

        foreach (var flag in choice.setFlagsOnSelect)
        {
            GameFlags.Set(flag);
        }

        DialogueUI.Instance.Show(PlayerSpeakerName, choice.resultingLine, null);
    }

    private void OnContinueClicked()
    {
        if (!IsActive)
        {
            return;
        }

        if (awaitingPlayerLine)
        {
            awaitingPlayerLine = false;
            Advance(pendingNextIndex);
            return;
        }

        DialogueNode node = data.nodes[currentIndex];
        if (node.choices != null && node.choices.Count > 0)
        {
            return;
        }

        Advance(node.nextNodeIndex);
    }

    private void Advance(int nextIndex)
    {
        currentIndex = nextIndex;
        ShowCurrentNode();
    }

    private void ShowCurrentNode()
    {
        if (currentIndex < 0 || currentIndex >= data.nodes.Count)
        {
            EndDialogue();
            return;
        }

        DialogueNode node = data.nodes[currentIndex];

        foreach (var flag in node.setFlagsOnEnter)
        {
            GameFlags.Set(flag);
        }

        List<string> choiceLabels = null;
        if (node.choices != null && node.choices.Count > 0)
        {
            choiceLabels = new List<string>();
            foreach (var choice in node.choices)
            {
                choiceLabels.Add(choice.moodLabel);
            }
        }

        selectedChoiceIndex = 0;
        DialogueUI.Instance.Show(node.speakerName, node.line, choiceLabels);
    }

    private void EndDialogue()
    {
        IsActive = false;
        awaitingPlayerLine = false;
        DialogueUI.Instance.Hide();

        if (playerController != null)
        {
            playerController.InputEnabled = true;
        }

        playerController = null;
        data = null;
    }

    private static bool WasPreviousPressed(Keyboard k)
    {
        return k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame
            || k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame;
    }

    private static bool WasNextPressed(Keyboard k)
    {
        return k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame
            || k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame;
    }

    private static bool IsChoiceKeyPressed(Keyboard keyboard, int index)
    {
        switch (index)
        {
            case 0: return keyboard.digit1Key.wasPressedThisFrame;
            case 1: return keyboard.digit2Key.wasPressedThisFrame;
            case 2: return keyboard.digit3Key.wasPressedThisFrame;
            case 3: return keyboard.digit4Key.wasPressedThisFrame;
            default: return false;
        }
    }
}
