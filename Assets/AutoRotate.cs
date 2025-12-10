using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    public float speed = 50f; // Velocità di rotazione
    public bool isRotating = true; // Stato attuale

    void Update()
    {
        // Se la variabile è vera, ruota l'oggetto sull'asse Y (verticale)
        if (isRotating)
        {
            // Time.deltaTime rende la rotazione fluida su tutti i telefoni
            transform.Rotate(0, speed * Time.deltaTime, 0); 
            Debug.Log("STO RUOTANDO!");
        }
    }

    // Questa funzione sarà chiamata dal bottone per cambiare stato
    public void ToggleRotation()
    {
        isRotating = !isRotating; // Se è vero diventa falso, e viceversa
    }
}