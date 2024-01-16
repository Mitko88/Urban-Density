using SuperGrid2D;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfViewRays : MonoBehaviour
{
    public float fieldOfView = 120f; // Total field of view in degrees
    public float rayDistance = 350f; // Distance for each ray
    public float raysOffset = 1f; // Rays will be cast 1 degree
    public int maxVisibleObjects = 25;
    private float verticalShift = 5f; // rays will be cast 5 meters from the ground horizontally
    private Gradient heatmapGradient;
    private string layerName = "points";
    private int layerMask;
    private StaticGrid2D<int> _staticGrid;
    private StaticGrid2D<Line> _linesGrid;
    private float distanceBetweenPoints = 10f;
    public LineRenderer line;
    public bool showRays; 

    void Start()
    {
        InitializeGradient();
        InitializeGrid();

        layerMask = ~LayerMask.GetMask(layerName);

        for (var i = 0; i < transform.childCount; i++)
        {
            var point = transform.GetChild(i);
            foreach (var closestObject in _linesGrid.Contact(new Circle(point.position.x, point.position.z, distanceBetweenPoints)))
            {
                var orientation = (closestObject.w - closestObject.v).normalized;
                var visibleObjectsCount = VisibleObjects(point, orientation);
                point.GetComponent<PointsData>().buildingsVisible = visibleObjectsCount;
                var heatmapValue = (float)visibleObjectsCount / maxVisibleObjects;
                Renderer renderer = point.gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Assign the new color to the material
                    renderer.material.color = heatmapGradient.Evaluate(heatmapValue);
                }
            }
        }
    }

    private int VisibleObjects(Transform trans, Vector2 orientation)
    {
        var origin = new Vector3(trans.position.x, trans.position.y + verticalShift, trans.position.z);
        float halfFOV = fieldOfView / 2;
        var uniqueObjectsHit = new List<string>();
        var orientation3D = new Vector3(orientation.x, 0, orientation.y);

        for (float angle = -halfFOV; angle <= halfFOV; angle += raysOffset)
        {
            // Calculate the direction for this angle
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * orientation3D;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, rayDistance, layerMask))
            {
                if (showRays)
                {
                    Debug.DrawLine(origin, hit.point, Color.yellow, 100); // Draws ray in the Scene View
                }
                // Ray hit something, handle hit
                var objectHitName = hit.collider.gameObject.name;
                if (!uniqueObjectsHit.Contains(objectHitName))
                {
                    uniqueObjectsHit.Add(objectHitName);
                }
            }
        }

        return uniqueObjectsHit.Count;
    }

    void InitializeGradient()
    {
        // Define 10 colors for the heatmap
        GradientColorKey[] colorKeys = new GradientColorKey[8];
        colorKeys[0] = new GradientColorKey(Color.blue, 0.0f);
        colorKeys[1] = new GradientColorKey(Color.cyan, 0.1f);
        colorKeys[2] = new GradientColorKey(Color.green, 0.2f);
        colorKeys[3] = new GradientColorKey(new Color(0.5f, 1f, 0f), 0.3f); // Yellow-Green
        colorKeys[4] = new GradientColorKey(Color.yellow, 0.4f);
        colorKeys[5] = new GradientColorKey(new Color(1f, 0.65f, 0f), 0.5f); // Orange
        colorKeys[6] = new GradientColorKey(new Color(1f, 0.3f, 0f), 0.6f); // Red-Orange
        colorKeys[7] = new GradientColorKey(Color.red, 0.7f);

        // Create the gradient
        heatmapGradient = new Gradient();
        heatmapGradient.colorKeys = colorKeys;
    }

    void InitializeGrid()
    {
        var pointsRenderers = GetComponentsInChildren<Renderer>();

        var bounds = GetBounds(pointsRenderers);

        var topLeft2d = new Vector2(bounds.min.x, bounds.min.z);

        _staticGrid = new StaticGrid2D<int>(topLeft2d, bounds.size.x, bounds.size.z, distanceBetweenPoints * 2);
        _linesGrid = new StaticGrid2D<Line>(topLeft2d, bounds.size.x, bounds.size.z, distanceBetweenPoints * 2);

        foreach (var pointRenderer in pointsRenderers)
        {
            _staticGrid.Add(pointRenderer.gameObject.GetInstanceID(), new Point(new Vector2(pointRenderer.gameObject.transform.position.x, pointRenderer.gameObject.transform.position.z)));
        }

        for (int i = 0; i < line.positionCount - 1; i++)
        {
            var origin = line.GetPosition(i);
            var end = line.GetPosition(i+1);
            var lineGrid = new Line(new Vector2(origin.x, origin.z), new Vector2(end.x, end.z));
            _linesGrid.Add(lineGrid, lineGrid);
        }

    }

    public static Bounds GetBounds(IEnumerable<Renderer> renderers)
    {
        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;

        foreach (var renderer in renderers)
        {
            min = Vector3.Min(min, renderer.bounds.min);
            max = Vector3.Max(max, renderer.bounds.max);
        }

        var center = (min + max) / 2;
        var size = max - min;

        return new Bounds(center, size);
    }
}
