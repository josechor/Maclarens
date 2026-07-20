using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    private static readonly Color ChoiceNormalColor = new Color(0.6f, 0.9f, 1f);
    private static readonly Color ChoiceSelectedColor = new Color(1f, 0.9f, 0.3f);

    public static DialogueUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text lineText;
    [SerializeField] private Text continueHintText;
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private Text[] choiceTexts;

    private List<string> currentChoiceLabels;

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
        Text speakerRef,
        Text lineRef,
        Text continueHintRef,
        GameObject choicesContainerRef,
        Text[] choiceTextRefs)
    {
        panel = panelRef;
        speakerText = speakerRef;
        lineText = lineRef;
        continueHintText = continueHintRef;
        choicesContainer = choicesContainerRef;
        choiceTexts = choiceTextRefs;

        panel.SetActive(false);
    }

    public void Show(string speaker, string line, IReadOnlyList<string> choices)
    {
        panel.SetActive(true);
        speakerText.text = speaker;
        lineText.text = line;

        bool hasChoices = choices != null && choices.Count > 0;
        choicesContainer.SetActive(hasChoices);
        continueHintText.gameObject.SetActive(!hasChoices);

        currentChoiceLabels = hasChoices ? new List<string>(choices) : null;

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            bool show = hasChoices && i < choices.Count;
            choiceTexts[i].gameObject.SetActive(show);
        }

        SetSelectedChoice(hasChoices ? 0 : -1);
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
            choiceTexts[i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentChoiceLabels = null;
    }
}
