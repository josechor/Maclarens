using System.Collections.Generic;

// Estado persistente completo de UNA partida, serializable a JSON con UnityEngine.JsonUtility.
// Un único archivo en disco = una única ranura de guardado.
//
// Nota: JsonUtility NO sabe serializar Dictionary, por eso los contadores se guardan como dos
// listas paralelas (nombre[i] <-> valor[i]).
[System.Serializable]
public class SaveData
{
    // Sube este número si algún día cambia el formato, para poder migrar saves viejos.
    public int saveVersion = 1;

    public List<string> flags = new List<string>();            // flags booleanas activas

    public List<string> counterNames = new List<string>();     // contadores: nombres...
    public List<int> counterValues = new List<int>();          // ...y sus valores (misma posición)

    public List<string> playedConversations = new List<string>(); // conversaciones "una vez" ya jugadas

    // Posición del jugador en la escena de juego. hasPlayerPosition = false en una partida recién
    // creada (aún no se ha movido / no procede colocarlo en un sitio concreto).
    public bool hasPlayerPosition = false;
    public float playerX;
    public float playerY;
}
