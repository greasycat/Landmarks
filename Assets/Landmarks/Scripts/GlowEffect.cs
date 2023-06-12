using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowEffect : MonoBehaviour
{
    public float glowIntensity = 1.0f;
    public float glowSpeed = 1.0f;

    private Material glowMaterial;
    private float initialEmission;

    // Start is called before the first frame update
    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        glowMaterial = renderer.material;
        initialEmission = glowMaterial.GetFloat("EmissionIntensity");
    }

    // Update is called once per frame
    private void Update()
    {
        float emission = Mathf.PingPong(Time.time * glowSpeed, 1.0f) * glowIntensity;
        glowMaterial.SetFloat("EmissionIntensity", emission);
    }
}
