using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

// handles the global ui and settings
// expose static variables (associated with the class, initialized at game start) for easy access to settings
public class GameManager : MonoBehaviour
{
    [Header("Player Settings")]
    // settings
    public static float mouseSens = 0.4f, soundVolume, musicVolume;
    enum MenuState {
        Menu,
        Singleplayer,
        Multiplayer, 
        Options
    }
    private UIDocument doc;
    // in c#, 
    public VisualTreeAsset[] menuLayouts;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        doc = GetComponent<UIDocument>(); 
        // todo: load menus without changing the visual tree asset
        Debug.Log(doc.rootVisualElement);

        foreach(VisualTreeAsset layout in menuLayouts) {
            Debug.Log(layout);
        }

        LoadMenu(MenuState.Menu);
        // we have default values, but we immediately load in the previous values if they exist
        // finally, initiate a save every time (will take effect during onapplicationquit) 
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return)) Screen.fullScreen = !Screen.fullScreen;
    }

    // various functions for loading menus 
    void LoadMenu(MenuState menu) {
        // load the corresponding menu layout
        // rootvisualelement finds the first button with name equal to th string using dfs
        doc.visualTreeAsset = menuLayouts[(int) menu];

        switch(menu) {
            case MenuState.Menu:
            doc.rootVisualElement.Q<Button>("singleplayer-button").clicked += () => LoadMenu(MenuState.Singleplayer);
            // doc.rootVisualElement.Q<Button>("quit-button").clicked += () => {
            //     Debug.Log("hi");
            //     Application.Quit();
            // };
            break;
            case MenuState.Singleplayer:
            doc.rootVisualElement.Q<Button>("level1-button").clicked += () => {
                Debug.Log("Transitioning to level 1");
                SceneManager.LoadScene("Level1");
                doc.enabled = false;
                // doc.visualTreeAsset = null;
            };
            doc.rootVisualElement.Q<Button>("back-button").clicked += () => LoadMenu(MenuState.Menu);
            break;
        }
    }
}
