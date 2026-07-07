using UnityEngine;
using UnityEngine.EventSystems;

public class ImageZoomPan : MonoBehaviour, IDragHandler
{
    public RectTransform imageRect;
    public float zoomSpeed = 0.005f;
    public float minZoom = 1f;
    public float maxZoom = 5f;

    private Vector2 originalPosition;
    private bool isInitialized = false;

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        // Automatically snaps scale and position back to perfect defaults whenever the panel opens!
        ResetZoom();
    }

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;
        
        if (imageRect == null)
        {
            imageRect = GetComponent<RectTransform>();
        }
        
        if (imageRect != null) 
        {
            originalPosition = imageRect.anchoredPosition;
            isInitialized = true;
        }
    }

    public void ResetZoom()
    {
        Initialize();
        if (imageRect != null)
        {
            imageRect.localScale = Vector3.one;
            imageRect.anchoredPosition = originalPosition;
        }
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
        // Desktop testing fallback
        #if UNITY_EDITOR
        else
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                Zoom(scrollInput * 50f * zoomSpeed);
            }
        }
        #endif
    }

    void Zoom(float increment)
    {
        if (imageRect == null) return; 

        Vector3 newScale = imageRect.localScale + new Vector3(increment, increment, 0);
        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        imageRect.localScale = newScale;
        
        if (newScale.x <= minZoom) 
        {
            imageRect.anchoredPosition = originalPosition;
        }
        else 
        {
            ClampPosition();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (imageRect == null) return; 

        if (Input.touchCount <= 1 && imageRect.localScale.x > 1f)
        {
            imageRect.anchoredPosition += eventData.delta;
            ClampPosition(); 
        }
    }

    private void ClampPosition()
    {
        if (imageRect == null) return;

        float boundsX = (imageRect.rect.width * imageRect.localScale.x - imageRect.rect.width) / 2f;
        float boundsY = (imageRect.rect.height * imageRect.localScale.y - imageRect.rect.height) / 2f;

        Vector2 clampedPosition = imageRect.anchoredPosition;
        
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, originalPosition.x - boundsX, originalPosition.x + boundsX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, originalPosition.y - boundsY, originalPosition.y + boundsY);

        imageRect.anchoredPosition = clampedPosition;
    }
}