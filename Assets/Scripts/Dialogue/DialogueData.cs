using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    [Tooltip("Lo que ve el jugador en el botón: una actitud/tono, no el texto literal.")]
    public string moodLabel;

    [Tooltip("Lo que el personaje dice realmente. Se revela después de elegir la actitud.")]
    [TextArea]
    public string resultingLine;

    public int nextNodeIndex = -1;
}

[System.Serializable]
public class DialogueNode
{
    public string speakerName;

    [TextArea]
    public string line;

    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Tooltip("Siguiente nodo cuando el nodo NO tiene choices (línea seguida con 'pulsa E'). -1 termina la conversación.")]
    public int nextNodeIndex = -1;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "McClarens/Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<DialogueNode> nodes = new List<DialogueNode>();
}
