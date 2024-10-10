using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    public void resume()
    {
        GameManager.Instance.stateUnpaused();
    }
    public void restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        GameManager.Instance.isWallDestroyed = false;
        GameManager.Instance.stateUnpaused();
    }

    public void exit()
    {
        GameManager.Instance.isWallDestroyed = false;
        GameManager.Instance.stateUnpaused();
        SceneManager.LoadScene("MainMenu");
    }
}
