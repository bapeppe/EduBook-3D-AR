using UnityEngine;
using GLTFast;

public class WebModelLoader : MonoBehaviour
{
    // Togliamo il valore di default. Ora è vuoto.
    public string modelUrl = ""; 
    public AutoRotate rotateScript;

    // Togliamo "Start". Non deve partire da solo appena nasce.
    // Creiamo invece una funzione pubblica che chiama il "Capo".
    public async void LoadModel(string urlDaScaricare)
    {
        if (string.IsNullOrEmpty(urlDaScaricare)) return;

        modelUrl = urlDaScaricare; // Memorizziamo l'URL
        
        // --- Qui riparte il codice di prima ---
        var gltf = gameObject.AddComponent<GltfAsset>();
        gltf.Url = modelUrl;
        gltf.LoadOnStartup = false; // Importante: controlliamo noi il via

        bool success = await gltf.Load(modelUrl);

        if (success)
        {
            if (gltf.SceneInstance != null)
            {
                await System.Threading.Tasks.Task.Yield();
                CenterModel(); // La tua assicurazione sulla posizione ;)
            }
            if(rotateScript != null) rotateScript.isRotating = true;
        }
    }

    void CenterModel()
    {
        // ... (Mantieni pure il codice di centraggio che ti ho dato prima qui) ...
        // Se non ce l'hai più, dimmelo che te lo rimetto.
        // Serve a evitare che il modello appaia spostato.
    }
}