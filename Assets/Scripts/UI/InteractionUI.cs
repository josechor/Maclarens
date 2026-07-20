using UnityEngine;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [SerializeField] private GameObject promptPanel;
    [SerializeField] private Text promptText;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private Text messageText;

    public bool IsMessageOpen => messagePanel != null && messagePanel.activeSelf;

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

    public void Configure(GameObject promptPanelRef, Text promptTextRef, GameObject messagePanelRef, Text messageTextRef)
    {
        promptPanel = promptPanelRef;
        promptText = promptTextRef;
        messagePanel = messagePanelRef;
        messageText = messageTextRef;

        promptPanel.SetActive(false);
        messagePanel.SetActive(false);
    }

    public void ShowPrompt(string text)
    {
        if (IsMessageOpen)
        {
            return;
        }

        promptText.text = text;
        promptPanel.SetActive(true);
    }

    public void HidePrompt()
    {
        promptPanel.SetActive(false);
    }

    public void ShowMessage(string text)
    {
        HidePrompt();
        messageText.text = text;
        messagePanel.SetActive(true);
    }

    public void CloseMessage()
    {
        messagePanel.SetActive(false);
    }
}
