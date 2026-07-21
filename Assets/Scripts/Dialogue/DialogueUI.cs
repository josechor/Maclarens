using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentación del diálogo: TextMeshPro (para rich-text), efecto typewriter saltable y retratos.
// No conoce la lógica de la conversación; solo muestra lo que el DialogueRunner le pide.
public class DialogueUI : MonoBehaviour
{
    private static readonly Color ChoiceNormalColor = new Color(0.6f, 0.9f, 1f);
    private static readonly Color ChoiceSelectedColor = new Color(1f, 0.9f, 0.3f);
    private static readonly Color PortraitActive = Color.white;
    private static readonly Color PortraitInactive = new Color(0.5f, 0.5f, 0.5f, 1f);

    public static DialogueUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private TMP_Text continueHintText;
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private TMP_Text[] choiceTexts;
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private float charsPerSecond = 45f;

    private List<string> currentChoiceLabels;
    private Coroutine typingRoutine;
    private int totalVisibleChars;

    public bool IsTyping { get; private set; }
    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Configure(
        GameObject panelRef,
        TMP_Text speakerRef,
        TMP_Text lineRef,
        TMP_Text continueHintRef,
        GameObject choicesContainerRef,
        TMP_Text[] choiceTextRefs,
        Image leftPortraitRef,
        Image rightPortraitRef)
    {
        panel = panelRef;
        speakerText = speakerRef;
        lineText = lineRef;
        continueHintText = continueHintRef;
        choicesContainer = choicesContainerRef;
        choiceTexts = choiceTextRefs;
        leftPortrait = leftPortraitRef;
        rightPortrait = rightPortraitRef;

        panel.SetActive(false);
    }

    // Muestra una línea hablada con typewriter y coloca el retrato del que habla.
    public void ShowLine(string speaker, string expression, string text)
    {
        panel.SetActive(true);
        choicesContainer.SetActive(false);
        continueHintText.gameObject.SetActive(true);
        currentChoiceLabels = null;

        ShowPortrait(speaker, expression, out string displayName);
        speakerText.text = displayName;

        lineText.text = text;
        lineText.ForceMeshUpdate();
        totalVisibleChars = lineText.textInfo.characterCount;
        lineText.maxVisibleCharacters = 0;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypewriterRoutine());
    }

    // Completa la línea al instante (acelerar el texto).
    public void CompleteTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        lineText.maxVisibleCharacters = totalVisibleChars;
        IsTyping = false;
    }

    private IEnumerator TypewriterRoutine()
    {
        IsTyping = true;
        float delay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

        for (int shown = 1; shown <= totalVisibleChars; shown++)
        {
            lineText.maxVisibleCharacters = shown;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        IsTyping = false;
        typingRoutine = null;
    }

    public void ShowChoices(IReadOnlyList<string> labels)
    {
        panel.SetActive(true);
        continueHintText.gameObject.SetActive(false);
        choicesContainer.SetActive(true);

        currentChoiceLabels = new List<string>(labels);

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            choiceTexts[i].gameObject.SetActive(i < labels.Count);
        }

        SetSelectedChoice(0);
    }

    public void SetSelectedChoice(int index)
    {
        if (currentChoiceLabels == null)
        {
            return;
        }

        for (int i = 0; i < choiceTexts.Length && i < currentChoiceLabels.Count; i++)
        {
            bool selected = i == index;
            string cursor = selected ? "> " : "   ";

            choiceTexts[i].text = $"{cursor}{i + 1}. {currentChoiceLabels[i]}";
            choiceTexts[i].color = selected ? ChoiceSelectedColor : ChoiceNormalColor;
            choiceTexts[i].fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentChoiceLabels = null;

        if (leftPortrait != null)
        {
            leftPortrait.enabled = false;
        }

        if (rightPortrait != null)
        {
            rightPortrait.enabled = false;
        }
    }

    private void ShowPortrait(string speaker, string expression, out string displayName)
    {
        CharacterDef def = CharacterRegistry.Get(speaker);

        if (def == null)
        {
            Debug.LogError($"DialogueUI: personaje '{speaker}' no está registrado (falta CharacterDef o CharacterRegistryLoader).");
            displayName = speaker;
            return;
        }

        displayName = def.characterName;

        string expr = string.IsNullOrEmpty(expression) ? def.DefaultExpressionId : expression;
        if (!def.HasExpression(expr))
        {
            Debug.LogError($"DialogueUI: la expresión '{expr}' no existe en {speaker}.");
            expr = def.DefaultExpressionId;
        }

        Sprite sprite = def.GetPortrait(expr);
        Image active = def.defaultSide == PortraitSide.Left ? leftPortrait : rightPortrait;
        Image other = def.defaultSide == PortraitSide.Left ? rightPortrait : leftPortrait;

        if (active != null)
        {
            active.sprite = sprite;
            active.enabled = sprite != null;
            active.color = PortraitActive;
        }

        // El retrato del otro lado (si ya había alguien) se atenúa, no se borra: así el NPC
        // sigue visible mientras responde el jugador y viceversa.
        if (other != null && other.enabled)
        {
            other.color = PortraitInactive;
        }
    }
}
