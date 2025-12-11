using UnityEngine;
using UnityEngine.XR.ARFoundation; // Serve per parlare con l'AR

[System.Serializable]
public struct LinkData
{
    public string imageName; // Es: "Pagina1"
    public string modelUrl;  // Es: "https://.../dino.glb"
}

public class ModelDatabase : MonoBehaviour
{
    public ARTrackedImageManager imageManager;
    
    // Qui creeremo la nostra lista nell'Inspector
    public LinkData[] database; 

    void OnEnable()
    {
        // Ci iscriviamo all'evento: "Avvisami quando trovi un'immagine"
        imageManager.trackedImagesChanged += OnImageChanged;
    }

    void OnDisable()
    {
        imageManager.trackedImagesChanged -= OnImageChanged;
    }

    void OnImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Per ogni NUOVA immagine trovata...
        foreach (var trackedImage in eventArgs.added)
        {
            string nameFound = trackedImage.referenceImage.name;
            Debug.Log("Trovata immagine: " + nameFound);

            // Cerchiamo nel nostro database l'URL corrispondente
            string urlTrovato = GetUrlByName(nameFound);

            if (!string.IsNullOrEmpty(urlTrovato))
            {
                // Cerchiamo lo script Loader sull'oggetto appena apparso
                WebModelLoader loader = trackedImage.GetComponent<WebModelLoader>();
                
                if (loader != null)
                {
                    // Diamo l'ordine di scaricare QUEL modello specifico
                    loader.LoadModel(urlTrovato);
                }
            }
        }
    }

    string GetUrlByName(string nameKey)
    {
        // Scorre la lista e trova l'URL giusto
        foreach (var data in database)
        {
            if (data.imageName == nameKey)
            {
                return data.modelUrl;
            }
        }
        return ""; // Nessun URL trovato
    }
}