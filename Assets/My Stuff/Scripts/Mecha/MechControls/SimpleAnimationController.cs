using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SimpleAnimationController : MonoBehaviour
{
    [Header("Animation Reference")]
    [SerializeField] private Animator[] animators;

    [Header("Float Controls")]
    [SerializeField] private List<FloatControl> floatControls = new();

    [Header("Control Settings")]
    [SerializeField] private float adjustmentSpeed = 1f;

    private void Update()
    {
        foreach (var control in floatControls)
        {
            if (Input.GetKey(control.increaseKey))
                ModifyControl(control, adjustmentSpeed * Time.deltaTime);

            if (Input.GetKey(control.decreaseKey))
                ModifyControl(control, -adjustmentSpeed * Time.deltaTime);

            ApplyToAnimators(control);
        }
    }

    private void ModifyControl(FloatControl control, float amount)
    {
        control.currentValue += amount;
        control.currentValue = Mathf.Clamp01(control.currentValue);
    }

    private void ApplyToAnimators(FloatControl control)
    {
        foreach (var anim in animators)
        {
            if (anim == null) continue;

            if (AnimatorHasParameter(anim, control.parameterName, AnimatorControllerParameterType.Float))
            {
                anim.SetFloat(control.parameterName, control.currentValue);
            }
        }
    }

    private bool AnimatorHasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var param in anim.parameters)
        {
            if (param.type == type && param.name == paramName)
                return true;
        }
        return false;
    }

    public void IncreaseParameter(string paramName)
    {
        var control = floatControls.FirstOrDefault(x => x.parameterName == paramName);
        if (control != null)
        {
            ModifyControl(control, adjustmentSpeed * Time.deltaTime);
        }
        else
        {
            Debug.LogWarning($"SimpleAnimationController: Parameter '{paramName}' not found in FloatControls list.");
        }
    }

    public void DecreaseParameter(string paramName)
    {
        var control = floatControls.FirstOrDefault(x => x.parameterName == paramName);
        if (control != null)
        {
            ModifyControl(control, -adjustmentSpeed * Time.deltaTime);
        }
    }
}

#region helper classes
[System.Serializable]
public class FloatControl
{
    public string parameterName;
    public KeyCode increaseKey = KeyCode.None;
    public KeyCode decreaseKey = KeyCode.None;
    [Range(0f, 1f)] public float currentValue = 0f;
}
#endregion