using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobbingAnimationTargets : MonoBehaviour
{
    public float bobbingSpeed = 1f;     // Speed of the bobbing animation
    public float bobbingHeight = 0.5f;  // Height of the bobbing animation

    private Vector3 originalPosition;
    private float timer = 0f;

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        // Calculate the new Y position based on a sine wave
        float newY = originalPosition.y + Mathf.Sin(timer * bobbingSpeed) * bobbingHeight;

        // Update the object's position
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
