using UnityEngine;
using GLTFast;
using System.Threading.Tasks;

public class AWSLoader : MonoBehaviour
{
    private GameObject currentModel;
    private bool isRotating = true;
    public float rotationSpeed = 50f;

    // NUOVA FIRMA: Accetta posizione e rotazione
    public async void DownloadModelAtPosition(string url, Vector3 position, Quaternion rotation)
    {
        if (currentModel != null) Destroy(currentModel);

        currentModel = new GameObject("Scaricato_da_AWS");
        
        // Applichiamo la posizione rilevata sul tavolo/libro
        currentModel.transform.position = position;
        currentModel.transform.rotation = rotation;

        var gltf = currentModel.AddComponent<GltfAsset>();
        gltf.Url = url;
        gltf.LoadOnStartup = false; 

        bool success = await gltf.Load(url);

        if (success)
        {
            if (gltf.SceneInstance != null)
            {
                await Task.Yield(); 
                FixMaterials(currentModel);
                RecenterModel(currentModel); // Questo fisserà la rotazione strana
            }
        }
    }

    void Update()
    {
        if (currentModel != null && isRotating)
        {
            // Ruota attorno al SUO asse locale, non attorno al mondo
            currentModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void ToggleRotation() { isRotating = !isRotating; }

    void FixMaterials(GameObject model)
    {
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");
        
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    Texture texture = mat.GetTexture("_BaseMap");
                    mat.shader = standardShader;
                    mat.SetTexture("_BaseMap", texture);
                }
                else { mat.shader = standardShader; }
            }
        }
    }

    void RecenterModel(GameObject parentObject)
    {
        Bounds bounds = new Bounds(parentObject.transform.position, Vector3.zero);
        Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        foreach (Renderer ren in renderers) bounds.Encapsulate(ren.bounds);

        // Calcolo preciso del centro visivo
        Vector3 centerOffset = bounds.center - parentObject.transform.position;
        
        // IMPORTANTE: Spostiamo i figli, così il PIVOT del genitore resta dov'è (sul QR)
        foreach (Transform child in parentObject.transform)
        {
            child.position -= centerOffset;
        }

        // Scala fissa a 20cm
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension > 0)
        {
            parentObject.transform.localScale = Vector3.one * (0.2f / maxDimension);
        }
    }
}