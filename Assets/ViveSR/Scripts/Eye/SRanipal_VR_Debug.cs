using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SRanipal_VR_Debug : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public TMP_Text originText;
    [SerializeField] public TMP_Text directionText;
    [SerializeField] public TMP_Text hitText;
    [SerializeField] public TMP_Text hitTargetText;
    [SerializeField] public TMP_Text extraText;
    [SerializeField] public Canvas canvas;
    
    
    private void Start()
    {
        Disable();
    }

    public void Enable()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);
    }
    
    public void Disable()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    // Update is called once per frame
    public void UpdateOrigin(Vector3 origin)
    {
        originText.text = origin.ToString();
    }
    
    public void UpdateDirection(Vector3 direction)
    {
        directionText.text = direction.ToString();
    }
    
    public void UpdateHit(Transform hit)
    {
        if (hit == null)
            return;
        
        hitText.text = hit.name;
    }
    
    public void UpdateHitTarget(Transform hitTarget)
    {
        if (hitTarget == null)
            return;
        
        hitTargetText.text = hitTarget.name;
    }
    
    public void UpdateExtra(string extra)
    {
        extraText.text = extra;
    }
    
}
