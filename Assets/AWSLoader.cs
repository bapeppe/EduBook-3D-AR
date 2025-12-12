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
        // 1. Troviamo lo shader URP sicuro
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                // 2. CACCIA ALLA TEXTURE: Cerchiamo l'immagine ovunque possa essersi nascosta
                Texture textureTrovata = null;

                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                {
                    textureTrovata = mat.GetTexture("_BaseMap");
                }
                else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                {
                    textureTrovata = mat.GetTexture("_MainTex"); // <--- ECCO IL FIX!
                }
                else if (mat.HasProperty("baseColorTexture") && mat.GetTexture("baseColorTexture") != null)
                {
                    textureTrovata = mat.GetTexture("baseColorTexture"); // Nome usato da glTF a volte
                }

                // 3. APPLICAZIONE
                if (textureTrovata != null)
                {
                    mat.shader = standardShader; // Cambia motore
                    mat.SetTexture("_BaseMap", textureTrovata); // Incolla la texture nel posto giusto per URP
                    mat.color = Color.white; // Resetta eventuali tinte strane
                }
                else
                {
                    // Se proprio non c'è texture, almeno mettiamo lo shader giusto
                    mat.shader = standardShader;
                }
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