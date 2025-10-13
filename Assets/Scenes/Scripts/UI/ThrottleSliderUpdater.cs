using UnityEngine;
using UnityEngine.UI;

public class ThrottleSliderUpdater : MonoBehaviour
{
    public enum ThrottleSide { Left, Right }

    [SerializeField] private ThrottleSide side;
    [SerializeField] private Slider throttleSlider;
    [SerializeField] private float throttleSpeed = 5f;

    private float currentThrottle;

    private void OnValidate()
    {
        if (throttleSlider == null)
        {
            throttleSlider = GetComponent<Slider>();
        }
    }

    private void Start()
    {
        if (throttleSlider != null)
        {
            throttleSlider.minValue = 0;
            throttleSlider.maxValue = 100;
        }
        else
        {
            Debug.LogError("Throttle Slider component not found on " + gameObject.name);
            this.enabled = false;
        }
    }

    private void Update()
    {
        float targetThrottle = 0f;

        if (side == ThrottleSide.Left)
        {
            targetThrottle = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        }
        else 
        {
            targetThrottle = Input.GetKey(KeyCode.I) ? 1f : Input.GetKey(KeyCode.K) ? -1f : 0f;
        }

        currentThrottle = Mathf.Lerp(currentThrottle, targetThrottle, Time.deltaTime * throttleSpeed);

        float sliderValue = (currentThrottle + 1) * 50f;

        throttleSlider.value = sliderValue;
    }
}