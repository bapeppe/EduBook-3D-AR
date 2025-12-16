using System.Collections;
using System.Collections.Generic;
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
    public ARRaycastManager raycastManager;
    public AWSLoader loaderScript;
    public TextMeshProUGUI statusText;

    // MODIFICA 1: Inizia disattivato per sicurezza
    private bool isScanning = false; 
    private MultiFormatReader reader = new MultiFormatReader();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // MODIFICA 2: Usiamo Start come Coroutine per il ritardo iniziale
    IEnumerator Start()
    {
        if(statusText != null) statusText.text = "Avvio fotocamera...";
        
        // Aspetta 2 secondi reali prima di attivare il cervello
        yield return new WaitForSeconds(2.0f);
        
        isScanning = true;
        if(statusText != null) statusText.text = "Inquadra un QR Code...";
        
        // Colleghiamo l'evento solo ORA, non prima
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    // Importante: Rimuovere l'evento quando si chiude/cambia scena
    void OnDestroy()
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
            
            // Controllo extra: scansione valida solo se è un link web
            if (!string.IsNullOrEmpty(scannedText) && scannedText.StartsWith("http"))
            {
                // Lanciamo un raggio dal centro dello schermo
                Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                
                if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
                {
                    Pose hitPose = hits[0].pose;
                    
                    if(statusText != null) statusText.text = "Trovato! Posiziono...";
                    
                    isScanning = false; // Stop scansione
                    loaderScript.DownloadModelAtPosition(scannedText, hitPose.position, hitPose.rotation);
                }
                else
                {
                     if(statusText != null) statusText.text = "QR Trovato! Inquadra un piano...";
                }
            }
        }
        yield return null;
    }

    private unsafe void ConvertImageSafe(XRCpuImage image, XRCpuImage.ConversionParams paramsData, NativeArray<byte> buffer)
    {
        image.Convert(paramsData, new System.IntPtr(buffer.GetUnsafePtr()), buffer.Length);
    }

    public void RestartExperience()
    {
        loaderScript.DestroyModel();
        
        // Piccolo ritardo anche nel reset per evitare doppie letture immediate
        StartCoroutine(ResetDelay());
    }

    IEnumerator ResetDelay()
    {
        if(statusText != null) statusText.text = "Reset in corso...";
        yield return new WaitForSeconds(1.0f);
        
        isScanning = true;
        hits.Clear();
        if(statusText != null) statusText.text = "Inquadra un nuovo QR Code...";
    }
}