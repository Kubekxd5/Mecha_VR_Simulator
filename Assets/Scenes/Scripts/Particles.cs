using UnityEngine;

public class Particles : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] myParticleSystems;
    [SerializeField] private GameObject[] objectsToToggle;

    public float interval;
    private float _nextFireTime;
    private float _lightInterval = 1f, _nextLight;
    private void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            foreach(var pSystem in myParticleSystems)
            {
                pSystem.Play();
            }
            _nextFireTime = Time.time + interval;
        }

        if (Input.GetButton("Fire2") && Time.time >= _nextLight)
        {
            foreach (var var in objectsToToggle)
            {
                var.SetActive(!var.activeSelf);
                _nextLight = Time.time + _lightInterval;
            }
        }
    }
}
