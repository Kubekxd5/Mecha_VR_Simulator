using System.Collections.Generic;
using UnityEngine;

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
                control.currentValue += adjustmentSpeed * Time.deltaTime;

            if (Input.GetKey(control.decreaseKey))
                control.currentValue -= adjustmentSpeed * Time.deltaTime;

            control.currentValue = Mathf.Clamp01(control.currentValue);

            foreach (var anim in animators)
            {
                if (!AnimatorHasParameter(anim, control.parameterName, AnimatorControllerParameterType.Float)) continue;

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
}

#region helper classes

[System.Serializable]
public class FloatControl
{
    public string parameterName;
    public KeyCode increaseKey = KeyCode.None;
    public KeyCode decreaseKey = KeyCode.None;
    [HideInInspector] public float currentValue = 0f;
}

#endregion
