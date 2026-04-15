using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadLobbyScene()
    {
        SceneManager.LoadScene("Lobby");
    }
}
