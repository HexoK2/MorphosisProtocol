using UnityEngine;
using System.Collections.Generic;

public class CombinedIsometricCamera : MonoBehaviour
{
    [Header("Cible de la Caméra")]
    public Transform cameraTarget;

    [Header("Paramètres de la Caméra")]
    public Vector3 cameraOffset = new Vector3(10f, 10f, -10f);
    [Range(0.01f, 1f)] public float cameraSmoothSpeed = 0.125f;

    [Header("Obstacles")]
    public LayerMask obstacleLayer;
    public float sphereCastRadius = 0.5f;
    public float transparentAlpha = 0.3f;
    public float fadeSpeed = 5f;

    [Header("Debug")]
    public bool showDebugRay = true;

    private List<Renderer> currentlyTransparentObjects = new();
    private Dictionary<Renderer, Color> originalAlbedoColors = new();
    private Renderer mouseOverObject = null;

    void LateUpdate()
    {
        if (cameraTarget == null) return;

        Vector3 targetPosition = cameraTarget.position;

        HandleCameraFollowing(targetPosition);
        HandleObstacleTransparency(targetPosition);
        HandleMouseOverTransparency();
        ResetNoLongerTransparentObjects();
    }

    void HandleCameraFollowing(Vector3 targetPosition)
    {
        Vector3 desiredPosition = targetPosition + cameraOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, cameraSmoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(targetPosition);
    }

    void HandleObstacleTransparency(Vector3 targetPosition)
    {
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = (targetPosition - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin, targetPosition);

        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, sphereCastRadius, rayDirection, rayDistance, obstacleLayer);

        List<Renderer> transparentThisFrame = new();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null && rend != mouseOverObject)
            {
                transparentThisFrame.Add(rend);
                if (!originalAlbedoColors.ContainsKey(rend))
                    originalAlbedoColors[rend] = rend.material.color;
                SetTransparent(rend);
            }
        }

        currentlyTransparentObjects = transparentThisFrame;
    }

    void HandleMouseOverTransparency()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, obstacleLayer))
        {
            Renderer newOver = hit.collider.GetComponent<Renderer>();
            if (mouseOverObject != newOver)
                mouseOverObject = newOver;

            if (mouseOverObject != null && !originalAlbedoColors.ContainsKey(mouseOverObject))
                originalAlbedoColors[mouseOverObject] = mouseOverObject.material.color;

            SetTransparent(mouseOverObject);
        }
    }

    void ResetNoLongerTransparentObjects()
    {
        foreach (var rend in new List<Renderer>(originalAlbedoColors.Keys))
        {
            if (rend == null || currentlyTransparentObjects.Contains(rend) || rend == mouseOverObject) continue;

            Material mat = rend.material;
            Color original = originalAlbedoColors[rend];
            mat.color = Color.Lerp(mat.color, original, Time.deltaTime * fadeSpeed);

            if (Mathf.Abs(mat.color.a - original.a) < 0.01f)
            {
                mat.color = original;
                originalAlbedoColors.Remove(rend);
            }
        }
    }

    void SetTransparent(Renderer rend)
    {
        Material mat = rend.material;
        Color baseColor = originalAlbedoColors.ContainsKey(rend) ? originalAlbedoColors[rend] : mat.color;
        Color transparentColor = new Color(baseColor.r, baseColor.g, baseColor.b, transparentAlpha);
        mat.color = Color.Lerp(mat.color, transparentColor, Time.deltaTime * fadeSpeed);
    }

    void OnDrawGizmos()
    {
        if (!showDebugRay || cameraTarget == null) return;

        Gizmos.color = Color.red;
        Vector3 targetPosition = cameraTarget.position;
        Gizmos.DrawLine(transform.position, targetPosition);

        Vector3 dir = (targetPosition - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, targetPosition);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sphereCastRadius);
        Gizmos.DrawWireSphere(transform.position + dir * dist, sphereCastRadius);
    }
}