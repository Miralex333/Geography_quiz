using UnityEngine;
using UnityEngine.EventSystems;

public class ImageZoomPan : MonoBehaviour, IDragHandler
{
    public RectTransform imageRect;
    public float zoomSpeed = 0.005f;
    public float minZoom = 1f;
    public float maxZoom = 5f;

    private Vector2 originalPosition;

    void Start()
    {
        // 1. SAFETY NET: If anyone forgot to link it on another PC, 
        // the script automatically finds itself so it never breaks!
        if (imageRect == null)
        {
            imageRect = GetComponent<RectTransform>();
        }
        
        if (imageRect != null) originalPosition = imageRect.anchoredPosition;
    }

    void Update()
    {
        // Pinch-to-zoom loop
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
    }

    void Zoom(float increment)
    {
        if (imageRect == null) return; // 2. Prevents background crashes if missing

        Vector3 newScale = imageRect.localScale + new Vector3(increment, increment, 0);
        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        imageRect.localScale = newScale;
        
        if (newScale.x <= minZoom) imageRect.anchoredPosition = originalPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (imageRect == null) return; // 2. Safety check

        if (Input.touchCount <= 1 && imageRect.localScale.x > 1f)
        {
            imageRect.anchoredPosition += eventData.delta;
        }
    }
}