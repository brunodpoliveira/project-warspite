using UnityEngine;

namespace Warspite.World
{
    /// <summary>
    /// Visualizes projectile trajectory before firing.
    /// Shows predicted path using LineRenderer with physics simulation.
    /// Useful for planning throws in slow-motion.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryIndicator : MonoBehaviour
    {
        [Header("Trajectory Settings")]
        [SerializeField] private int maxPoints = 50;
        [SerializeField] private float timeStep = 0.1f;
        [SerializeField] private float maxDistance = 50f;

        [Header("Visual Settings")]
        [SerializeField] private Color trajectoryColor = new Color(1f, 1f, 0f, 0.6f);
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private bool showImpactPoint = true;
        [SerializeField] private GameObject impactMarkerPrefab;

        [Header("Physics")]
        [SerializeField] private LayerMask collisionMask = -1;

        private LineRenderer lineRenderer;
        private GameObject impactMarker;
        private bool isVisible = false;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        void Start()
        {
            // Create impact marker
            if (showImpactPoint && impactMarkerPrefab == null)
            {
                CreateDefaultImpactMarker();
            }
            else if (showImpactPoint && impactMarkerPrefab != null)
            {
                impactMarker = Instantiate(impactMarkerPrefab);
                impactMarker.SetActive(false);
            }

            Hide();
        }

        private void SetupLineRenderer()
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = trajectoryColor;
            lineRenderer.endColor = trajectoryColor;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;
        }

        private void CreateDefaultImpactMarker()
        {
            impactMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactMarker.name = "ImpactMarker";
            impactMarker.transform.localScale = Vector3.one * 0.2f;

            // Remove collider
            Collider col = impactMarker.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set color
            Renderer renderer = impactMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.red;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.red);
                renderer.material = mat;
            }

            impactMarker.SetActive(false);
        }

        /// <summary>
        /// Shows trajectory prediction for a projectile with given initial velocity
        /// </summary>
        public void ShowTrajectory(Vector3 startPosition, Vector3 initialVelocity, float mass = 0.1f)
        {
            if (!isVisible)
            {
                Show();
            }

            // Simulate trajectory
            Vector3[] points = SimulateTrajectory(startPosition, initialVelocity, mass, out Vector3 impactPoint, out bool didHit);

            // Update line renderer
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);

            // Update impact marker
            if (impactMarker != null && didHit)
            {
                impactMarker.transform.position = impactPoint;
                impactMarker.SetActive(true);
            }
            else if (impactMarker != null)
            {
                impactMarker.SetActive(false);
            }
        }

        /// <summary>
        /// Hides the trajectory visualization
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            lineRenderer.positionCount = 0;
            if (impactMarker != null)
            {
                impactMarker.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the trajectory visualization
        /// </summary>
        public void Show()
        {
            isVisible = true;
        }

        private Vector3[] SimulateTrajectory(Vector3 startPos, Vector3 velocity, float mass, out Vector3 impactPoint, out bool didHit)
        {
            Vector3[] points = new Vector3[maxPoints];
            Vector3 currentPos = startPos;
            Vector3 currentVel = velocity;
            int pointCount = 0;

            impactPoint = startPos;
            didHit = false;

            float gravity = Physics.gravity.magnitude;
            Vector3 gravityVector = Physics.gravity;

            for (int i = 0; i < maxPoints; i++)
            {
                // Store current position
                points[pointCount] = currentPos;
                pointCount++;

                // Calculate next position
                Vector3 nextPos = currentPos + currentVel * timeStep;

                // Check for collision
                if (Physics.Linecast(currentPos, nextPos, out RaycastHit hit, collisionMask))
                {
                    // Hit something
                    impactPoint = hit.point;
                    points[pointCount] = hit.point;
                    pointCount++;
                    didHit = true;
                    break;
                }

                // Check max distance
                if (Vector3.Distance(startPos, nextPos) > maxDistance)
                {
                    break;
                }

                // Update position and velocity
                currentPos = nextPos;
                currentVel += gravityVector * timeStep;
            }

            // Trim array to actual point count
            Vector3[] result = new Vector3[pointCount];
            System.Array.Copy(points, result, pointCount);
            return result;
        }

        void OnDestroy()
        {
            if (impactMarker != null)
            {
                Destroy(impactMarker);
            }
        }

        public bool IsVisible => isVisible;
    }
}
