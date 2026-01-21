using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SimpleWaterStream : MonoBehaviour
{
    [Header("Physik Einstellungen")]
    public Vector3 startVelocity = new Vector3(0, 0, 2f);
    public float gravity = -9.81f;

    [Header("Visuals (Geometrie)")]
    public int resolution = 20;
    public float maxSimulationTime = 1.5f;
    public float textureScrollSpeed = 2f;

    [Header("Wellen-Form (Dicke)")]
    public float baseWidth = 0.1f;      // Die normale Dicke
    public float waveAmount = 0.03f;    // Wie stark der Strahl dicker/dünner wird
    public float waveFrequency = 10f;   // Wie viele "Beulen" der Strahl hat
    public float waveSpeed = 5f;        // Wie schnell die Beulen nach unten wandern

    // Performance-Optimierung: Wir nutzen weniger Keys für die Kurve als Punkte für die Linie
    private const int CURVE_RESOLUTION = 10;

    private LineRenderer lr;
    private Material waterMat;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr.material != null) waterMat = lr.material;
    }

    private void Update()
    {
        DrawStreamPhysics();
        AnimateWaterTexture();
        AnimateThicknessCurve();
    }

    // 1. Berechnet die Flugbahn (Wurfparabel)
    private void DrawStreamPhysics()
    {
        Vector3[] points = new Vector3[resolution];
        Vector3 startPosition = transform.position;
        Vector3 worldVelocity = transform.TransformDirection(startVelocity);

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1) * maxSimulationTime;

            // P = Start + v*t + 0.5*g*t^2
            Vector3 displacement = worldVelocity * t;
            displacement.y += 0.5f * gravity * t * t;

            points[i] = startPosition + displacement;
        }

        lr.positionCount = resolution;
        lr.SetPositions(points);
    }

    // 2. Bewegt die Textur, damit es fließt
    private void AnimateWaterTexture()
    {
        if (waterMat != null)
        {
            float offset = Time.time * textureScrollSpeed;
            waterMat.mainTextureOffset = new Vector2(offset, 0);
        }
    }

    // 3. Berechnet die wellenförmige Dicke ("Traveling Wave")
    private void AnimateThicknessCurve()
    {
        Keyframe[] keys = new Keyframe[CURVE_RESOLUTION + 1];

        for (int i = 0; i <= CURVE_RESOLUTION; i++)
        {
            // t geht von 0.0 (Start der Pipe) bis 1.0 (Ende des Strahls)
            float t = (float)i / CURVE_RESOLUTION;

            // Sinus-Welle berechnen:
            // (t * frequency) -> macht mehrere Wellen entlang der Linie
            // (- Time.time * waveSpeed) -> schiebt die Welle nach "unten"
            float sineValue = Mathf.Sin(t * waveFrequency - Time.time * waveSpeed);

            // Den Sinus (-1 bis 1) auf unsere Dicke anwenden
            float currentWidth = baseWidth + (sineValue * waveAmount);

            keys[i] = new Keyframe(t, currentWidth);
        }

        // Neue Kurve zuweisen
        lr.widthCurve = new AnimationCurve(keys);
    }
}