using UnityEngine;

// Registra un conjunto de CharacterDef en el CharacterRegistry al arrancar la escena.
// Lo cablea TestSceneBuilder con los personajes del juego.
public class CharacterRegistryLoader : MonoBehaviour
{
    [SerializeField] private CharacterDef[] characters;

    private void Awake()
    {
        if (characters == null)
        {
            return;
        }

        foreach (var def in characters)
        {
            CharacterRegistry.Register(def);
        }
    }
}
