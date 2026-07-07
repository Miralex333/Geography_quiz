using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultipleImagesTrackingManager : MonoBehaviour
{
    [SerializeField] private QuizManager Q1;
    [SerializeField] private List<GameObject> prefabsToSpawn = new List<GameObject>();

    // Distance threshold in meters (0.5m = 50cm). 
    // Models further than this will be hidden to prevent floating depth errors.
    [SerializeField] private float maxReliableDistance = 0.5f;

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> arObjects;
    private ARTrackedImage currentTrackedImage;
    private GameObject currentObject;

    private string lastCountry = "";
    private string currentcat = "";

    private void Start()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();

        if (trackedImageManager == null)
        {
            Debug.LogError("ARTrackedImageManager not found!");
            return;
        }

        trackedImageManager.trackablesChanged.AddListener(OnImagesTrackedChanged);
        arObjects = new Dictionary<string, GameObject>();

        // Instantiate all prefabs and keep them hidden
        foreach (GameObject prefab in prefabsToSpawn)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = prefab.name;
            obj.SetActive(false);

            if (!arObjects.ContainsKey(obj.name))
                arObjects.Add(obj.name, obj);
        }
    }

    private void Update()
    {
        if (Q1 == null) return;

        // Update when either the country or category changes
        if (Q1.CurrentCountry != lastCountry || Q1.arCategory != currentcat)
        {
            lastCountry = Q1.CurrentCountry;
            currentcat = Q1.arCategory;
            UpdateDisplayedModel();
        }

        // Manual Transform updating is removed. 
        // Parenting natively handles the AR position/rotation smoothing.
    }

    private void OnDestroy()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnImagesTrackedChanged);
    }

    private void OnImagesTrackedChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
            HandleTrackedImage(trackedImage);

        foreach (var trackedImage in eventArgs.updated)
            HandleTrackedImage(trackedImage);

        foreach (var trackedImage in eventArgs.removed)
        {
            if (currentObject != null)
                currentObject.SetActive(false);

            currentTrackedImage = null;
        }
    }

    private void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        if (trackedImage == null) return;

        // 1. Check if tracking is completely lost
        if (trackedImage.trackingState == TrackingState.None)
        {
            if (currentObject != null)
                currentObject.SetActive(false);

            currentTrackedImage = null;
            return;
        }

        // 2. Check the distance to prevent the model from floating due to poor depth estimation
        if (Camera.main != null)
        {
            float distanceToTarget = Vector3.Distance(Camera.main.transform.position, trackedImage.transform.position);

            if (distanceToTarget > maxReliableDistance)
            {
                // Image is recognized, but it's too far away for accurate depth. Hide the model.
                if (currentObject != null)
                    currentObject.SetActive(false);

                // Keep the reference so it instantly appears when the user moves closer
                currentTrackedImage = trackedImage;
                return;
            }
        }

        // 3. Tracking is solid and distance is good. Prepare to show the model.
        currentTrackedImage = trackedImage;
        UpdateDisplayedModel();
    }

    private void UpdateDisplayedModel()
    {
        if (currentTrackedImage == null) return;
        if (string.IsNullOrEmpty(Q1.CurrentCountry) || string.IsNullOrEmpty(Q1.arCategory)) return;

        string prefabName = $"{Q1.arCategory.ToLower()}_{Q1.CurrentCountry}";

        // Prevent unnecessary re-parenting if the correct object is already active
        if (currentObject != null && currentObject.name == prefabName && currentObject.activeSelf)
        {
            return;
        }

        // Hide previous model
        if (currentObject != null)
            currentObject.SetActive(false);

        if (!arObjects.TryGetValue(prefabName, out currentObject))
        {
            Debug.LogWarning($"No prefab found for: {prefabName}");
            currentObject = null;
            return;
        }

        // Set the active object as a child of the tracked image
        currentObject.transform.SetParent(currentTrackedImage.transform);

        // Reset local position and rotation so it snaps perfectly to the image center
        currentObject.transform.localPosition = Vector3.zero;
        currentObject.transform.localRotation = Quaternion.identity;

        currentObject.SetActive(true);
    }
}