using UnityEngine;
using UnityEngine.EventSystems;

public class ImageZoomPan : MonoBehaviour, IDragHandler
{
    public RectTransform imageRect;
    public float zoomSpeed = 0.005f;
    public float minZoom = 1f;
    public float maxZoom = 5f;
    
    [Header("Boundary Settings")]
    [Tooltip("Increases how far you can pull the image corners toward the center of the screen.")]
    public float cornerPadding = 150f; 

    private Vector2 originalPosition;
    private Canvas parentCanvas;

    void Start()
    {
        // Safety check to automatically grab the RectTransform
        if (imageRect == null)
        {
            imageRect = GetComponent<RectTransform>();
        }
        
        if (imageRect != null) 
        {
            originalPosition = imageRect.anchoredPosition;
            // Find the main UI canvas to fix the drag sensitivity
            parentCanvas = imageRect.GetComponentInParent<Canvas>();
        }
    }

    void Update()
    {
        // 1. Mobile Pinch-to-Zoom
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            Zoom(difference * zoomSpeed);
        }
        // 2. Editor Testing Fallback
        else
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                Zoom(scrollInput * 50f * zoomSpeed);
            }
        }
    }

    void Zoom(float increment)
    {
        if (imageRect == null) return; 

        Vector3 newScale = imageRect.localScale + new Vector3(increment, increment, 0);
        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        imageRect.localScale = newScale;
        
        ClampPosition();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (imageRect == null) return; 

        if (Input.touchCount <= 1 && imageRect.localScale.x > 1f)
        {
            // FIX: Divide by the Canvas scale factor so the drag matches finger speed 1:1
            float scaleFactor = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
            imageRect.anchoredPosition += eventData.delta / scaleFactor;
            
            ClampPosition();
        }
    }

    private void ClampPosition()
    {
        // Calculate the base maximum allowed movement
        float boundsX = (imageRect.rect.width * imageRect.localScale.x - imageRect.rect.width) / 2f;
        float boundsY = (imageRect.rect.height * imageRect.localScale.y - imageRect.rect.height) / 2f;

        // FIX: Add padding to the bounds so you can reach the corners comfortably
        boundsX += cornerPadding;
        boundsY += cornerPadding;

        // Apply the restriction
        Vector2 clampedPosition = imageRect.anchoredPosition;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, originalPosition.x - boundsX, originalPosition.x + boundsX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, originalPosition.y - boundsY, originalPosition.y + boundsY);
        imageRect.anchoredPosition = clampedPosition;
    }
}