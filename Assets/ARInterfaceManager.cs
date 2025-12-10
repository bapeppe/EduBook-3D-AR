using UnityEngine;
using TMPro; // Libreria per gestire il testo del bottone

public class ARInterfaceManager : MonoBehaviour
{
    public TextMeshProUGUI buttonText; // Riferimento al testo del bottone

    // Questa funzione la colleghiamo al Click del bottone
    public void OnTogglePressed()
    {
        // 1. CERCA IL MODELLO: Chiede a Unity "C'è un oggetto con lo script AutoRotate nella stanza?"
        AutoRotate model = FindObjectOfType<AutoRotate>();

        // 2. CONTROLLO DI SICUREZZA: Se il modello è stato trovato...
        if (model != null)
        {
            // Cambia lo stato (da Play a Pausa)
            model.ToggleRotation();

            // 3. AGGIORNA IL TESTO DEL BOTTONE
            if (model.isRotating)
            {
                buttonText.text = "PAUSA";
            }
            else
            {
                buttonText.text = "PLAY";
            }
        }
        else
        {
            Debug.Log("Nessun modello trovato! Inquadra prima il libro.");
        }
    }
}