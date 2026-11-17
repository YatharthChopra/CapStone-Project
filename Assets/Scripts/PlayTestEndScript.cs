using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayTestEndScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.name == "BoltCutters")
        {
            SceneManager.LoadScene("TheEnd");
        }
    }
}
