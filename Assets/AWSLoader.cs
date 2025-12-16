using UnityEngine;
using GLTFast;
using System.Threading.Tasks;

public class AWSLoader : MonoBehaviour
{
    [Header("Impostazioni Modello")]
    public float rotationSpeed = 50f;
    public float targetSize = 0.2f;

    [Header("Interfaccia UI")]
    public GameObject loadingPanel; // Trascina qui il pannello con la rotellina

    private GameObject currentModel;
    private bool isRotating = true;

    // --- PUNTO CRUCIALE: Usiamo Awake() ---
    // Awake viene eseguito prima di Start(), appena l'oggetto viene caricato in memoria.
    // Questo spegne il pannello istantaneamente, evitando che si veda all'avvio dell'app.
    void Awake()
    {
        if (loadingPanel != null) 
            loadingPanel.SetActive(false);
    }

    // --- FUNZIONE DI DOWNLOAD ---
    public async void DownloadModelAtPosition(string url, Vector3 position, Quaternion rotation)
    {
        // 1. ACCENDIAMO IL CARICAMENTO
        // Ora appare solo perché lo abbiamo chiamato esplicitamente qui
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // Pulizia vecchio modello se presente
        if (currentModel != null) Destroy(currentModel);

        // Creiamo il contenitore vuoto nel punto del QR
        currentModel = new GameObject("Scaricato_da_AWS");
        currentModel.transform.position = position;
        currentModel.transform.rotation = rotation;

        // Setup glTFast
        var gltf = currentModel.AddComponent<GltfAsset>();
        gltf.Url = url;
        gltf.LoadOnStartup = false; // Fermo! Gestiamo noi il caricamento.

        Debug.Log("Download avviato da: " + url);

        // Avviamo il download e aspettiamo la fine
        bool success = await gltf.Load(url);

        if (success)
        {
            if (gltf.SceneInstance != null)
            {
                // Aspettiamo un frame per sicurezza grafica
                await Task.Yield(); 
                
                // PASSO A: Recuperiamo la texture dalla memoria e coloriamo il modello
                await FixMaterialsDirectly(currentModel, gltf);
                
                // PASSO B: Centriamo il modello per non farlo "orbitare"
                RecenterModel(currentModel);
            }
        }
        else
        {
            Debug.LogError("Download fallito. Controlla il link o la connessione.");
        }

        // 2. SPEGNIAMO IL CARICAMENTO (Operazione finita)
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // --- FUNZIONE RESET (Tasto "Nuova Scansione") ---
    public void DestroyModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
        
        // Se l'utente resetta mentre sta caricando, nascondiamo comunque il pannello
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // --- RIPARAZIONE MATERIALI (Metodo Diretto alla Fonte) ---
    async Task FixMaterialsDirectly(GameObject model, GltfAsset gltfAsset)
    {
        // Cerchiamo lo shader sicuro (URP Lit)
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");

        Texture2D textureDiretta = null;
        
        // Entriamo nella memoria di glTFast per prendere la texture originale
        if (gltfAsset.Importer != null && gltfAsset.Importer.TextureCount > 0)
        {
            textureDiretta = gltfAsset.Importer.GetTexture(0);
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                mat.shader = standardShader; // Applica motore grafico sicuro

                if (textureDiretta != null)
                {
                    // Applica la texture estratta
                    mat.SetTexture("_BaseMap", textureDiretta); 
                    mat.SetTexture("_MainTex", textureDiretta);
                    mat.color = Color.white; 
                }
                else
                {
                    // Se non c'è texture, lascia bianco pulito
                    mat.color = Color.white; 
                }
            }
        }
        await Task.Yield();
    }

    // --- ROTAZIONE ---
    void Update()
    {
        if (currentModel != null && isRotating)
        {
            // Ruota sull'asse Y locale
            currentModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void ToggleRotation() { isRotating = !isRotating; }

    // --- CENTRAGGIO ---
    void RecenterModel(GameObject parentObject)
    {
        Bounds bounds = new Bounds(parentObject.transform.position, Vector3.zero);
        Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        foreach (Renderer ren in renderers) bounds.Encapsulate(ren.bounds);

        Vector3 centerOffset = bounds.center - parentObject.transform.position;
        
        // Sposta i figli all'indietro per centrarli sul pivot
        foreach (Transform child in parentObject.transform)
        {
            child.position -= centerOffset;
        }

        // Scala a 20cm (o targetSize)
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension > 0)
        {
            parentObject.transform.localScale = Vector3.one * (targetSize / maxDimension);
        }
    }
}