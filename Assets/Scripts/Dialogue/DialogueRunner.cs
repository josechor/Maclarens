using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ejecuta una conversación recorriendo sus pasos. Usa una PILA de "frames" para entrar y salir
// de los bloques de elección: cuando el bloque de una opción termina, se vuelve automáticamente
// al paso siguiente a la elección (reconvergencia), sin índices que mantener.
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [SerializeField] private InputActionAsset controls;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Button continueButton;

    public bool IsActive { get; private set; }

    // Sigue en true un frame más que IsActive, para que Interactor no reprocese la misma
    // pulsación que acaba de cerrar el diálogo y lo reabra al instante.
    public bool IsBlockingInteraction { get; private set; }

    private class Frame
    {
        public List<DialogueStep> Steps;
        public int Index;
    }

    private readonly Stack<Frame> stack = new Stack<Frame>();

    private ConversationAsset currentConversation;
    private TopDownController playerController;
    private bool skipInputThisFrame;

    private bool awaitingChoice;
    private ChoiceStep currentChoice;
    private int selectedChoiceIndex;

    private InputActionMap dialogueMap;
    private InputAction advanceAction;
    private InputAction navUpAction;
    private InputAction navDownAction;
    private InputAction[] choiceActions;

    public void ConfigureButtons(Button[] choices, Button continueBtn)
    {
        choiceButtons = choices;
        continueButton = continueBtn;
    }

    public void ConfigureControls(InputActionAsset controlsAsset)
    {
        controls = controlsAsset;
    }

    private void Awake()
    {
        Instance = this;
        ResolveActions();
        WireButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ResolveActions()
    {
        if (controls == null)
        {
            Debug.LogError("DialogueRunner: no hay InputActionAsset de controles asignado.");
            return;
        }

        dialogueMap = controls.FindActionMap("Dialogue", false);
        if (dialogueMap == null)
        {
            Debug.LogError("DialogueRunner: el InputActionAsset no tiene un mapa 'Dialogue'.");
            return;
        }

        advanceAction = dialogueMap.FindAction("Advance");
        navUpAction = dialogueMap.FindAction("NavigateUp");
        navDownAction = dialogueMap.FindAction("NavigateDown");
        choiceActions = new[]
        {
            dialogueMap.FindAction("Choice1"),
            dialogueMap.FindAction("Choice2"),
            dialogueMap.FindAction("Choice3"),
            dialogueMap.FindAction("Choice4")
        };
    }

    private void WireButtons()
    {
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
            continueButton.onClick.AddListener(OnAdvanceClicked);
        }
    }

    public void StartConversation(ConversationAsset conversation, GameObject player)
    {
        if (conversation == null)
        {
            Debug.LogWarning("DialogueRunner.StartConversation: conversación nula.");
            return;
        }

        if (DialogueUI.Instance == null)
        {
            Debug.LogError("DialogueRunner.StartConversation: no hay ningún DialogueUI en la escena.");
            return;
        }

        List<DialogueStep> steps = conversation.ParseSteps();
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning($"DialogueRunner: la conversación '{conversation.conversationId}' está vacía o tiene errores.");
            return;
        }

        currentConversation = conversation;
        stack.Clear();
        stack.Push(new Frame { Steps = steps, Index = 0 });

        IsActive = true;
        IsBlockingInteraction = true;
        skipInputThisFrame = true;
        awaitingChoice = false;

        playerController = player != null ? player.GetComponent<TopDownController>() : null;
        if (playerController != null)
        {
            playerController.InputEnabled = false;
        }

        dialogueMap?.Enable();

        ShowNext();
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

        if (awaitingChoice)
        {
            HandleChoiceInput();
        }
        else
        {
            HandleLineInput();
        }
    }

    private void LateUpdate()
    {
        IsBlockingInteraction = IsActive;
    }

    private void HandleLineInput()
    {
        if (Pressed(advanceAction))
        {
            OnAdvanceClicked();
        }
    }

    private void HandleChoiceInput()
    {
        int count = currentChoice.Options.Count;

        if (Pressed(navUpAction))
        {
            selectedChoiceIndex = (selectedChoiceIndex - 1 + count) % count;
            DialogueUI.Instance.SetSelectedChoice(selectedChoiceIndex);
            return;
        }

        if (Pressed(navDownAction))
        {
            selectedChoiceIndex = (selectedChoiceIndex + 1) % count;
            DialogueUI.Instance.SetSelectedChoice(selectedChoiceIndex);
            return;
        }

        if (choiceActions != null)
        {
            for (int i = 0; i < choiceActions.Length && i < count; i++)
            {
                if (Pressed(choiceActions[i]))
                {
                    SelectChoice(i);
                    return;
                }
            }
        }

        if (Pressed(advanceAction))
        {
            SelectChoice(selectedChoiceIndex);
        }
    }

    // Click de ratón sobre la pista "continuar" o sobre una línea.
    private void OnAdvanceClicked()
    {
        if (!IsActive || awaitingChoice)
        {
            return;
        }

        if (DialogueUI.Instance.IsTyping)
        {
            DialogueUI.Instance.CompleteTyping();
        }
        else
        {
            ShowNext();
        }
    }

    // Click de ratón sobre un botón de opción.
    private void OnChoiceClicked(int index)
    {
        if (!IsActive || !awaitingChoice)
        {
            return;
        }

        SelectChoice(index);
    }

    private void SelectChoice(int index)
    {
        if (currentChoice == null || index < 0 || index >= currentChoice.Options.Count)
        {
            return;
        }

        ChoiceOption option = currentChoice.Options[index];
        awaitingChoice = false;
        currentChoice = null;

        // El bloque de la opción se ejecuta y, al agotarse, la pila vuelve al tronco común.
        stack.Push(new Frame { Steps = option.Body, Index = 0 });
        ShowNext();
    }

    private void ShowNext()
    {
        while (stack.Count > 0)
        {
            Frame frame = stack.Peek();

            if (frame.Index >= frame.Steps.Count)
            {
                stack.Pop();
                continue;
            }

            DialogueStep step = frame.Steps[frame.Index];
            frame.Index++;

            switch (step)
            {
                case FlagStep flagStep:
                    if (flagStep.Value)
                    {
                        GameFlags.Set(flagStep.Flag);
                    }
                    else
                    {
                        GameFlags.Clear(flagStep.Flag);
                    }
                    continue;

                case CounterStep counterStep:
                    if (counterStep.Reset)
                    {
                        GameFlags.SetCount(counterStep.Counter, 0);
                    }
                    else
                    {
                        GameFlags.Increment(counterStep.Counter, counterStep.Delta);
                    }
                    continue;

                case CommandStep commandStep:
                    // Tanda 2: aquí irán cinemáticas ([wait], [move], Timeline...).
                    Debug.Log($"DialogueRunner: comando '{commandStep.Name}' ignorado (pendiente Tanda 2).");
                    continue;

                case LineStep lineStep:
                    DialogueUI.Instance.ShowLine(lineStep.Speaker, lineStep.Expression, lineStep.Text);
                    return;

                case ChoiceStep choiceStep:
                    currentChoice = choiceStep;
                    selectedChoiceIndex = 0;
                    awaitingChoice = true;
                    DialogueUI.Instance.ShowChoices(BuildLabels(choiceStep));
                    return;
            }
        }

        EndConversation();
    }

    private static List<string> BuildLabels(ChoiceStep choice)
    {
        var labels = new List<string>(choice.Options.Count);
        foreach (var option in choice.Options)
        {
            labels.Add(option.MoodLabel);
        }

        return labels;
    }

    private void EndConversation()
    {
        IsActive = false;
        awaitingChoice = false;
        currentChoice = null;

        DialogueUI.Instance.Hide();
        dialogueMap?.Disable();

        if (currentConversation != null && currentConversation.IsOnce)
        {
            ConversationHistory.MarkPlayed(currentConversation.conversationId);
        }

        if (playerController != null)
        {
            playerController.InputEnabled = true;
        }

        playerController = null;
        currentConversation = null;
        stack.Clear();
    }

    private static bool Pressed(InputAction action)
    {
        return action != null && action.WasPerformedThisFrame();
    }
}
