using UnityEngine;
using GLTFast;
using System.Threading.Tasks;

public class AWSLoader : MonoBehaviour
{
    [Header("Impostazioni")]
    public float autoRotationSpeed = 30f;
    public float sensitivity = 10f; // Sensibilità per Mouse/Dito
    public float targetSize = 0.2f;

    [Header("Interfaccia")]
    public GameObject loadingPanel;

    private GameObject currentModel;
    private bool isDragging = false; // Stiamo trascinando?
    private float lastMouseX;

    void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public async void DownloadModelAtPosition(string url, Vector3 position, Quaternion rotation)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (currentModel != null) Destroy(currentModel);

        currentModel = new GameObject("Scaricato_da_AWS");
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
                await FixMaterialsDirectly(currentModel, gltf);
                RecenterModel(currentModel);
                // Il collider non serve più con questa logica "globale"
            }
        }

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void Update()
    {
        if (currentModel == null) return;

        // --- INPUT UNIVERSALE (Funziona su PC e Telefono) ---

        // 1. Quando PREMI (Click Mouse o Tocco Dito)
        if (Input.GetMouseButtonDown(0)) 
        {
            isDragging = true; // Inizia il trascinamento
            lastMouseX = Input.mousePosition.x; // Memorizza dove hai cliccato
        }

        // 2. Quando RILASCI
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false; // Fine trascinamento
        }

        // 3. GESTIONE MOVIMENTO
        if (isDragging)
        {
            // Calcola quanto ti sei spostato rispetto al frame precedente
            float deltaX = Input.mousePosition.x - lastMouseX;
            lastMouseX = Input.mousePosition.x; // Aggiorna per il prossimo frame

            // Ruota il modello (asse Y invertito per sensazione naturale)
            // Moltiplichiamo per -1 per seguire il dito
            float rotationAmount = -deltaX * sensitivity * Time.deltaTime;
            currentModel.transform.Rotate(0, rotationAmount, 0, Space.World);
        }
        else
        {
            // Se NON stai toccando, gira da solo
            currentModel.transform.Rotate(Vector3.up, autoRotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    // --- PULSANTI E UTILITIES ---
    public void ToggleRotation() { /* Non serve più, gestito dal tocco */ }

    public void DestroyModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    async Task FixMaterialsDirectly(GameObject model, GltfAsset gltfAsset)
    {
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");
        Texture2D textureDiretta = null;
        if (gltfAsset.Importer != null && gltfAsset.Importer.TextureCount > 0)
            textureDiretta = gltfAsset.Importer.GetTexture(0);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                mat.shader = standardShader;
                if (textureDiretta != null) { mat.SetTexture("_BaseMap", textureDiretta); mat.SetTexture("_MainTex", textureDiretta); mat.color = Color.white; }
                else { mat.color = Color.white; }
            }
        }
        await Task.Yield();
    }

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
}