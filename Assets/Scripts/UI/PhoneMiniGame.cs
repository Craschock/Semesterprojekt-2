using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class PhoneMinigame : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Die Nadel")]
    public RectTransform needle;
    [Tooltip("Die Trefferzone")]
    public Image successZone;

    [Header("Base Difficulty")]
    public float baseRotationSpeed = 250f;
    [Range(0.1f, 0.5f)]
    public float baseZoneSize = 0.2f;

    [Header("Difficulty Ramp")]
    public float speedIncreasePerHit = 10f;
    public float maxRotationSpeed = 500f;
    public float sizeDecreasePerHit = 0.03f;
    public float minZoneSize = 0.05f;

    [Header("Hit puffer")]
    [Range(0f, 0.1f)]
    public float hitBufferPercent = 0.03f;

    [Header("Stats")]
    public float fearReduction = 10f;
    public float fearPenalty = 10f;

    [Header("Fail Feedback (Stutter if missed)")]
    public float failPauseDuration = 0.5f;
    public float stutterIntensity = 5f;

    // References
    public PlayerStats stats;

    // Internal State
    private float currentZoneAngle = 0f;
    private float currentSpeedVal;
    private float currentSizeVal;
    private float currentDirection = 1f;
    private bool isInputLocked = false;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ResetDifficulty();
        currentDirection = (Random.value > 0.5f) ? 1f : -1f;
        StartNewRound();
        isInputLocked = false;
    }

    private void Update()
    {
        if (needle == null || isInputLocked) return;

        // Rotate Needle
        float finalSpeed = currentSpeedVal * currentDirection * -1f;
        needle.Rotate(0, 0, finalSpeed * Time.deltaTime);

        // Input Check
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckHit();
        }
    }

    public void SetGameActive(bool active)
    {
        if (active == gameObject.activeSelf) return;
        gameObject.SetActive(active);
    }

    private void ResetDifficulty()
    {
        currentSpeedVal = baseRotationSpeed;
        currentSizeVal = baseZoneSize;
    }

    private void StartNewRound()
    {
        currentDirection *= -1f;

        if (successZone != null)
        {
            successZone.fillAmount = currentSizeVal;
            float randomAngle = Random.Range(45f, 315f);
            successZone.rectTransform.localEulerAngles = new Vector3(0, 0, randomAngle);
            currentZoneAngle = randomAngle;
        }

        if (needle != null) needle.localEulerAngles = Vector3.zero;

        isInputLocked = false;
    }

    private void CheckHit()
    {
        // Lots of math 
        float needleAngle = NormalizeAngle(needle.localEulerAngles.z);
        float zoneStart = NormalizeAngle(currentZoneAngle);
        float zoneMiddle = zoneStart + (currentSizeVal * 360f * 0.5f);
        float visualWidthDegrees = currentSizeVal * 360f;
        float bufferDegrees = hitBufferPercent * 360f;
        float totalAllowedWidth = visualWidthDegrees + bufferDegrees;
        float difference = Mathf.DeltaAngle(needleAngle, zoneMiddle);

        if (Mathf.Abs(difference) <= (totalAllowedWidth / 2f))
        {
            OnSuccess();
        }
        else
        {
            StartCoroutine(FailRoutine());
        }
    }

    private void OnSuccess()
    {
        Debug.Log($"Minigame HIT!");
        if (stats != null) stats.ReduceFear(fearReduction);

        // Ramp Up
        currentSpeedVal += speedIncreasePerHit;
        currentSpeedVal = Mathf.Min(currentSpeedVal, maxRotationSpeed);

        currentSizeVal -= sizeDecreasePerHit;
        currentSizeVal = Mathf.Max(currentSizeVal, minZoneSize);

        StartNewRound();
    }

    private IEnumerator FailRoutine()
    {
        isInputLocked = true;

        if (stats != null) stats.AddFear(fearPenalty);

        Quaternion frozenRotation = needle.rotation;
        float timer = 0f;

        while (timer < failPauseDuration)
        {
            float zJitter = Random.Range(-stutterIntensity, stutterIntensity);
            needle.rotation = frozenRotation * Quaternion.Euler(0, 0, zJitter);

            timer += Time.deltaTime;
            yield return null;
        }

        needle.rotation = frozenRotation;

        ResetDifficulty();
        StartNewRound();
    }

    private float NormalizeAngle(float a)
    {
        a = a % 360f;
        if (a < 0) a += 360f;
        return a;
    }
}