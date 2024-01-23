//========= Copyright 2018, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace ViveSR
{
    namespace anipal
    {
        namespace Eye
        {
            public class SRanipal_GazeRay : MonoBehaviour
            {
                public bool renderGazeRay = true;
                public int lengthOfRay = 25;
                private LineRenderer _gazeRayRenderer;

                private static EyeData_v2 _eyeData;
                private bool _eyeCallbackRegistered;
                [SerializeField] private List<string> targetTags;

                public Transform lastHitTransform;
                public Transform lastHitTargetTransform;
                public Vector3 gazeDirection;
                public Vector3 gazeOrigin;
                
                [SerializeField] private bool debug;
                private SRanipal_VR_Debug _debugModule;

                private void Start()
                {
                    if (!SRanipal_Eye_Framework.Instance.EnableEye)
                    {
                        enabled = false;
                        return;
                    }
                    
                    if (!renderGazeRay) return;
                    
                    // Try get the LineRenderer component
                    if (TryGetComponent(typeof(LineRenderer), out var gazeRayRenderer))
                    {
                        _gazeRayRenderer = (LineRenderer) gazeRayRenderer;
                    }
                    else
                    {
                        // Instantiate gaze ray renderer
                        _gazeRayRenderer = gameObject.AddComponent<LineRenderer>();
                        // Set gaze ray renderer properties
                        _gazeRayRenderer.startWidth = 0.01f;
                        _gazeRayRenderer.endWidth = 0.01f;
                        _gazeRayRenderer.positionCount = 2;
                        _gazeRayRenderer.startColor = Color.red;
                        _gazeRayRenderer.endColor = Color.red;
                    }
                    
                    if (targetTags == null || targetTags.Count == 0)
                    {
                        targetTags = new List<string> {"TargetObject"};
                    }

                    if (debug)
                    {
                        _debugModule = FindObjectOfType<SRanipal_VR_Debug>();
                        if (_debugModule == null)
                        {
                            Debug.LogError("SRanipal_VR_Debug not found");
                        }
                        else
                        {
                            _debugModule.Enable();
                        }
                        
                    }
                    
                    
                    
                }

                private void Update()
                {
                    if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                        SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

#if UNITY_STANDALONE
                    if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && _eyeCallbackRegistered == false)
                    {
                        SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                        _eyeCallbackRegistered = true;
                    }
                    else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && _eyeCallbackRegistered == true)
                    {
                        SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                        _eyeCallbackRegistered = false;
                    }

                    Vector3 gazeOriginCombinedLocal, gazeDirectionCombinedLocal;

                    if (_eyeCallbackRegistered)
                    {
                        if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out gazeOriginCombinedLocal, out gazeDirectionCombinedLocal, _eyeData)) { }
                        else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out gazeOriginCombinedLocal, out gazeDirectionCombinedLocal, _eyeData)) { }
                        else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out gazeOriginCombinedLocal, out gazeDirectionCombinedLocal, _eyeData)) { }
                        else return;
                    }
                    else
                    {
                        if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out gazeOriginCombinedLocal, out gazeDirectionCombinedLocal)) { }
                        else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out gazeOriginCombinedLocal, out gazeDirectionCombinedLocal)) { }
                        else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out gazeOriginCombinedLocal, out gazeDirectionCombinedLocal)) { }
                        else return;
                    }
                    
                    

                    Vector3 gazeDirectionCombined = Camera.main.transform.TransformDirection(gazeDirectionCombinedLocal);
                    
                    gazeDirection = gazeDirectionCombined;
                    gazeOrigin = Camera.main.transform.TransformPoint(gazeOriginCombinedLocal);
                    
                    if (renderGazeRay)
                    {
                        _gazeRayRenderer.SetPosition(0, Camera.main.transform.position - Camera.main.transform.up * 0.1f);
                        _gazeRayRenderer.SetPosition(1, Camera.main.transform.position + gazeDirectionCombined * lengthOfRay);
                    }
                    
                    // Cast the gaze ray and if it hits something, log it in the console
                    
                    // var hits = Physics.RaycastAll(Camera.main.transform.position, gazeDirectionCombined, lengthOfRay);
                    //
                    // foreach (var raycastHit in hits)
                    // {
                    //     var hitTransform = raycastHit.transform;
                    //     if ( hitTransform == lastHitTransform) return;
                    //     
                    //     var exitObjectName = hitTransform == null ? "null" : hitTransform.name;
                    //     lastHitTransform = hitTransform;
                    //     
                    //     Debug.Log("Exit " + exitObjectName);
                    //     Debug.Log("Entering " + hitTransform.name);
                    //     
                    //     if (targetTags.Any(targetTag => hitTransform.CompareTag(targetTag)))
                    //     {
                    //         Debug.Log("Hit Target:" + hitTransform.name);
                    //         lastHitTargetTransform = hitTransform;
                    //     }
                    // }
                    
                    if (Physics.Raycast(Camera.main.transform.position, gazeDirectionCombined, out var hit, lengthOfRay))
                    {
                        var hitTransform = hit.transform;
                        if ( hitTransform == lastHitTransform) return;
                        
                        var exitObjectName = hitTransform == null ? "null" : hitTransform.name;
                        lastHitTransform = hitTransform;
                        
                        Debug.Log("Exit " + exitObjectName);
                        Debug.Log("Entering " + hitTransform.name);
                        
                        if (targetTags.Any(targetTag => hitTransform.CompareTag(targetTag)))
                        {
                            Debug.Log("Hit Target:" + hitTransform.name);
                            lastHitTargetTransform = hitTransform;
                        }
                    }
                    
                    if (debug)
                    {
                        _debugModule.Enable();
                        _debugModule.UpdateOrigin(gazeOrigin);
                        _debugModule.UpdateDirection(gazeDirection);
                        _debugModule.UpdateHit(lastHitTransform);
                        _debugModule.UpdateHitTarget(lastHitTargetTransform);
                    }
                    
#endif
                }

                private void Release()
                {
                    if (_eyeCallbackRegistered == true)
                    {
                        SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                        _eyeCallbackRegistered = false;
                    }
                }

                private static void EyeCallback(ref EyeData_v2 eyeData)
                {
                    _eyeData = eyeData;
                }
            }
        }
    }
}
