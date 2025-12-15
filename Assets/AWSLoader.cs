using UnityEngine;
using GLTFast;
using System.Threading.Tasks;

public class AWSLoader : MonoBehaviour
{
    [Header("Impostazioni")]
    public float rotationSpeed = 50f;
    public float targetSize = 0.2f;

    private GameObject currentModel;
    private bool isRotating = true;

    // --- FUNZIONE PRINCIPALE ---
    public async void DownloadModelAtPosition(string url, Vector3 position, Quaternion rotation)
    {
        if (currentModel != null) Destroy(currentModel);

        // Creiamo il contenitore
        currentModel = new GameObject("Scaricato_da_AWS");
        currentModel.transform.position = position;
        currentModel.transform.rotation = rotation;

        // Setup glTFast
        var gltf = currentModel.AddComponent<GltfAsset>();
        gltf.Url = url;
        gltf.LoadOnStartup = false;

        Debug.Log("Download avviato...");

        // Avviamo il download
        bool success = await gltf.Load(url);

        if (success)
        {
            if (gltf.SceneInstance != null)
            {
                await Task.Yield(); // Aspettiamo un frame

                // 1. CORREZIONE MATERIALI (Metodo Diretto)
                // Passiamo il componente "gltf" per estrarre la texture dalla memoria
                await FixMaterialsDirectly(currentModel, gltf);

                // 2. CORREZIONE POSIZIONE
                RecenterModel(currentModel);
            }
        }
        else
        {
            Debug.LogError("Download fallito.");
        }
    }

    // --- NUOVA FUNZIONE DI FIX ---
    async Task FixMaterialsDirectly(GameObject model, GltfAsset gltfAsset)
    {
        // Troviamo lo shader sicuro (URP Lit)
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");

        // Recuperiamo la texture direttamente dalla "pancia" di glTFast (bypassando il materiale rotto)
        Texture2D textureDiretta = null;
        
        // Chiediamo all'importer se ha delle texture in memoria
        if (gltfAsset.Importer != null && gltfAsset.Importer.TextureCount > 0)
        {
            // Prendiamo la prima texture disponibile (solitamente è quella del colore base)
            textureDiretta = gltfAsset.Importer.GetTexture(0);
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                // Applichiamo lo shader sicuro
                mat.shader = standardShader;

                if (textureDiretta != null)
                {
                    // ABBIAMO VINTO: Applichiamo la texture che abbiamo estratto alla fonte
                    mat.SetTexture("_BaseMap", textureDiretta); 
                    mat.SetTexture("_MainTex", textureDiretta);
                    mat.color = Color.white; 
                }
                else
                {
                    // Se proprio il file non ha texture nemmeno in memoria, lo facciamo GIALLO per segnalarlo
                    mat.color = Color.yellow; 
                }
            }
        }
        
        await Task.Yield();
    }

    void Update()
    {
        if (currentModel != null && isRotating)
            currentModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    public void ToggleRotation() { isRotating = !isRotating; }

    void RecenterModel(GameObject parentObject)
    {
        Bounds bounds = new Bounds(parentObject.transform.position, Vector3.zero);
        Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        foreach (Renderer ren in renderers) bounds.Encapsulate(ren.bounds);

        Vector3 centerOffset = bounds.center - parentObject.transform.position;
        foreach (Transform child in parentObject.transform) child.position -= centerOffset;

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension > 0) parentObject.transform.localScale = Vector3.one * (targetSize / maxDimension);
    }

    // TASTO "RESET" 
    public void DestroyModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
    }
}