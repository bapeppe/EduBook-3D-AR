using UnityEngine;
using UnityEngine.SceneManagement; // Libreria necessaria per cambiare scena

public class MainMenuManager : MonoBehaviour
{
    // Questa funzione verrà chiamata quando premi il bottone
    public void AvviaApp()
    {
        // Carica la scena che si chiama "ARScene"
        SceneManager.LoadScene("ARScene");
    }
}