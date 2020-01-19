using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneSwitch : MonoBehaviour
{
    //public int nextScene = 0;
    //public int sceneToLoad;

    /* void Update()
    {
        Scene scene = SceneManager.GetActiveScene();
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        
        if (scene.name == "lobby")
        {
            if (buttonName == "Level1")
            {
                nextScene = 1;
            }
            
            else if (buttonName == "Level2")
            {
                nextScene = 2;
            }
        }
        else
        {
            nextScene = 0;
        }
        Debug.Log("scene name: "+scene.name);
        Debug.Log("buttonName: "+buttonName);
        Debug.Log("scene switch to: "+nextScene);
    } */

    // Start is called before the first frame update
    public void SceneSwitcher(int sceneToLoad)
    {
        SceneManager.LoadScene(sceneToLoad);
        Debug.Log("scene switch to: "+sceneToLoad);
    }

}
