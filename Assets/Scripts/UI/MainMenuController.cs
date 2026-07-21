using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal. "Jugar" comprueba si hay partida guardada:
//   - Si NO hay: ofrece "Nueva partida".
//   - Si SÍ hay: ofrece "Continuar" y "Borrar partida".
// El builder de la escena (MainMenuBuilder) crea los botones y llama a Configure() con ellos.
public class MainMenuController : MonoBehaviour
{
    // [SerializeField] para que las referencias que asigna el builder por código se guarden en la
    // escena y sigan ahí al entrar en Play (si fueran private a secas, se perderían y Awake fallaría).
    [SerializeField] private string gameSceneName = "TestRoom";
    [SerializeField] private Button playButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Text statusText;

    public void Configure(Button play, Button cont, Button newGame, Button delete, Button back, Button quit, Text status, string gameScene)
    {
        playButton = play;
        continueButton = cont;
        newGameButton = newGame;
        deleteButton = delete;
        backButton = back;
        quitButton = quit;
        statusText = status;
        gameSceneName = gameScene;
    }

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlay);
        continueButton.onClick.AddListener(OnContinue);
        newGameButton.onClick.AddListener(OnNewGame);
        deleteButton.onClick.AddListener(OnDelete);
        backButton.onClick.AddListener(ShowMain);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void Start()
    {
        ShowMain();
    }

    // --- Estados de la pantalla ---

    private void ShowMain()
    {
        statusText.text = "McClarens";
        SetVisible(play: true, cont: false, newGame: false, delete: false, back: false, quit: true);
    }

    private void OnPlay()
    {
        if (!SaveSystem.HasSave())
        {
            statusText.text = "No hay ninguna partida guardada.";
            SetVisible(play: false, cont: false, newGame: true, delete: false, back: true, quit: false);
        }
        else if (SaveSystem.SavedGameIsCompleted())
        {
            // Partida terminada: existe, pero no se puede continuar. Solo borrarla (o volver).
            statusText.text = "Ya terminaste el juego. Esta partida no se puede continuar.";
            SetVisible(play: false, cont: false, newGame: false, delete: true, back: true, quit: false);
        }
        else
        {
            statusText.text = "Tienes una partida guardada.";
            SetVisible(play: false, cont: true, newGame: false, delete: true, back: true, quit: false);
        }
    }

    private void OnContinue()
    {
        SaveSystem.Load();
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnNewGame()
    {
        SaveSystem.NewGame();
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnDelete()
    {
        SaveSystem.Delete();
        // Tras borrar ya no hay partida: se ofrece crear una nueva.
        statusText.text = "Partida borrada.";
        SetVisible(play: false, cont: false, newGame: true, delete: false, back: true, quit: false);
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetVisible(bool play, bool cont, bool newGame, bool delete, bool back, bool quit)
    {
        playButton.gameObject.SetActive(play);
        continueButton.gameObject.SetActive(cont);
        newGameButton.gameObject.SetActive(newGame);
        deleteButton.gameObject.SetActive(delete);
        backButton.gameObject.SetActive(back);
        quitButton.gameObject.SetActive(quit);
    }
}
