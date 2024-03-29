using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// handles the global ui and settings
// expose static variables (associated with the class, initialized at game start) for easy access to settings
public class GameManager : MonoBehaviour
{
    [Header("Player Settings")]
    // settings
    public static float mouseSens = 0.4f, soundVolume, musicVolume;
    public enum MenuState {
        Menu,
        Singleplayer,
        Multiplayer, 
        Options,
        Game,
        Paused
    }
    public static MenuState menuState;
    private UIDocument doc;
    const int NUM_LEVELS = 1;
    public VisualTreeAsset[] menuLayouts;

    
    public TemplateContainer easy;

    private bool pressedPauseThisFrame = false;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        doc = GetComponent<UIDocument>(); 

        LoadMenu(MenuState.Menu);
        // we have default values, but we immediately load in the previous values if they exist
        // finally, initiate a save every time (will take effect during onapplicationquit) 

        SceneManager.sceneLoaded += OnSceneLoad; 
    }

    // Update is called once per frame
    void Update()
    {
        // if(Input.GetKeyDown(KeyCode.Return)) Screen.fullScreen = !Screen.fullScreen;
        switch(menuState) {
            case MenuState.Game:
            if(pressedPauseThisFrame) {
            }     
            break;
        }
        pressedPauseThisFrame = false;
    }

    // various functions for loading menus 
    void LoadMenu(MenuState menu) {
        // load the corresponding menu layout
        // rootvisualelement finds the first button with name equal to th string using dfs
        doc.visualTreeAsset = menuLayouts[(int) menu];

        menuState = menu;

        // log all buttons? 
        doc.rootVisualElement.Query<Button>().ForEach(btn => {
            btn.focusable = false;
        });

        // doc.rootVisualElement.Query<VisualElement>().ForEach(v => {
        //     v.pickingMode = PickingMode.Ignore;
        // });

        switch(menu) {
            case MenuState.Menu:
            doc.rootVisualElement.Q<Button>("singleplayer-button").clicked += () => LoadMenu(MenuState.Singleplayer);
            doc.rootVisualElement.Q<Button>("multiplayer-button").clicked += () => LoadMenu(MenuState.Multiplayer);
            doc.rootVisualElement.Q<Button>("options-button").clicked += () => LoadMenu(MenuState.Options);
            doc.rootVisualElement.Q<Button>("quit-button").clicked += () => Application.Quit();
            break;
            case MenuState.Singleplayer:
            for(int i = 1; i <= NUM_LEVELS; i++) {
                Button btn = doc.rootVisualElement.Q<Button>("level" + i + "-button");
                btn.clicked += () => {
                    Debug.Log("Transitioning to level 1");
                    // release focus from this button (otherwise the uidocument becomes buggy)
                    btn.Blur();
                    SceneManager.LoadScene("Level" + i);
                    LoadMenu(MenuState.Game);
                };
            }

            doc.rootVisualElement.Q<Button>("back-button").clicked += () => LoadMenu(MenuState.Menu);
            break;

            case MenuState.Multiplayer:
            TextField ip = doc.rootVisualElement.Q<TextField>("ip-textfield");
            TextField port = doc.rootVisualElement.Q<TextField>("port-textfield");
            doc.rootVisualElement.Q<Button>("connect-button").clicked += () => {
                Debug.Log(string.Format("Connecting to {0}:{1}", ip.value, port.value));
            };
            // bruh.RegisterCallback<ClickEvent>(evt => bruh.Focus());

            doc.rootVisualElement.Q<Button>("back-button").clicked += () => LoadMenu(MenuState.Menu);
            break;

            case MenuState.Options:
            doc.rootVisualElement.Q<Button>("back-button").clicked += () => LoadMenu(MenuState.Menu);
            break;
        }
    }
    void OnPause(InputValue value) {
        Debug.Log("pressed pause");
        pressedPauseThisFrame = value.isPressed;
    }

    void OnSceneLoad(Scene scene, LoadSceneMode mode) {
        // Debug.Log("sceneloaded");
        // // automatically load the correct menu when the scene loads
        // if(scene.name.StartsWith("Level")) {
        // }
    }
}
