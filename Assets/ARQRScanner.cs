using System.Collections;
using System.Collections.Generic; // Serve per le liste
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using TMPro;

public class ARQRScanner : MonoBehaviour
{
    [Header("Componenti")]
    public ARCameraManager cameraManager;
    public ARRaycastManager raycastManager; // <--- NUOVO: Serve per toccare il mondo fisico
    public AWSLoader loaderScript;
    public TextMeshProUGUI statusText;

    private bool isScanning = true;
    private MultiFormatReader reader = new MultiFormatReader();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>(); // Lista dei punti colpiti

    void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!isScanning) return;

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        StartCoroutine(ProcessImage(image));
    }

    IEnumerator ProcessImage(XRCpuImage image)
    {
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            outputFormat = TextureFormat.R8,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        ConvertImageSafe(image, conversionParams, buffer);
        image.Dispose();

        int width = conversionParams.outputDimensions.x;
        int height = conversionParams.outputDimensions.y;
        
        var luminanceSource = new RGBLuminanceSource(buffer.ToArray(), width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        var binarizer = new HybridBinarizer(luminanceSource);
        var binaryBitmap = new BinaryBitmap(binarizer);
        var result = reader.decode(binaryBitmap);
        
        buffer.Dispose();

        if (result != null)
        {
            string scannedText = result.Text;
            if (scannedText.StartsWith("http"))
            {
                // ABBIAMO IL LINK! ORA CERCHIAMO DOVE PIAZZARLO.
                
                // Lanciamo un raggio dal centro dello schermo (0.5, 0.5)
                Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                
                // Cerchiamo piani (tavoli/libri) o punti caratteristici
                if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
                {
                    // Abbiamo toccato qualcosa di solido!
                    Pose hitPose = hits[0].pose; // La posizione esatta nello spazio 3D
                    
                    Debug.Log("QR Trovato e Posizione Fisica Trovata!");
                    if(statusText != null) statusText.text = "Posiziono modello...";
                    
                    isScanning = false;
                    
                    // Passiamo sia il link CHE la posizione fisica al Loader
                    loaderScript.DownloadModelAtPosition(scannedText, hitPose.position, hitPose.rotation);
                }
                else
                {
                    // Se non trova superfici, ti dice di avvicinarti
                     if(statusText != null) statusText.text = "Inquadra meglio il piano...";
                }
            }
        }
        yield return null;
    }

    private unsafe void ConvertImageSafe(XRCpuImage image, XRCpuImage.ConversionParams paramsData, NativeArray<byte> buffer)
    {
        image.Convert(paramsData, new System.IntPtr(buffer.GetUnsafePtr()), buffer.Length);
    }

    public void ResetScanner()
    {
        isScanning = true;
        if(statusText != null) statusText.text = "Cerca QR...";
    }
}