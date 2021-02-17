using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes; //Unity AssetStore plug-in

//changing a shaders based on camera and player
public class CameraShaderChanger : MonoBehaviour
{
    //parameters that can be set by the user
    [SerializeField]
    private RefTransform _Player;                        //reference to the player
    [SerializeField]
    private RefCamera _Cam;                             //reference to the camera
    [SerializeField, Required, BoxGroup("Wall Mask")]
    private RefWallMaskShaderData _ShaderData;          //the offsetXYZ, size, smoothness, and softness defined by the user
    [SerializeField, BoxGroup("Wall Mask")]
    private LayerMask _WallsLayer;                      //the objects' layer considered as a wall to change to the dissolve shader 
    [SerializeField, Label("Objects Layer"), BoxGroup("Wall Mask")]
    private LayerMask _Obstacles;                       //the objects' layer to use the Always Visible shader


    private GameObject _Obj;
    private Renderer _Rend;
    private Shader _Standard;
    private Shader _AlwaysVisible;
    private Shader _BehindWalls;
    private Shader _Previous;
    private Vector3 _MaskSmoothPoint;
    


    private void Awake()
    {
        _Standard = Shader.Find("PlayerCharacter");
        _AlwaysVisible = Shader.Find("AlwaysVisible");
        _BehindWalls = Shader.Find("WallDissolve");
        _Rend = GetComponent<Renderer>();
    }

    private void FixedUpdate()
    {
        //checks if an object was found between player and camera on the previous FixedUpdate,
        //if yes, it applies the previous shader+texture of the object - this is done because the object could lose its texture reference
        if (_Obj != null && _Obj.GetComponentInChildren<Renderer>() != null)
        {
            _Obj.GetComponentInChildren<Renderer>().material.shader = _Previous;
        }

        // raycasts a vector between object and camera to see if it hits the Objects' Layer
        // if not it keeps the default shader
        RaycastHit hit;
        var dir = _Cam.Camera.transform.position - _Player.Value.position;

        if (Physics.Raycast(transform.position, dir, out hit, Mathf.Infinity, _Obstacles))
        {
            // Debug.DrawLine(_Cam.Camera.transform.position, (_Player.Value.position + (_Player.Value.forward)), Color.green, hit.distance);
            // Debug.Log("Did Hit NOT WALL");
            // Debug.Log(hit.transform.gameObject.name);
            _Rend.material.shader = _AlwaysVisible;
        }
        else
        {
            // Debug.DrawLine(_Cam.Camera.transform.position, (_Player.Value.position + (_Player.Value.forward)), Color.red, hit.distance);
            // Debug.Log("Did not Hit");
            _Rend.material.shader = _Standard;
        }


        //raycasts a vector between object and camera to see if it hits the the Walls' Layer
        if (Physics.Raycast(transform.position, dir, out hit, Mathf.Infinity, _WallsLayer))
        {
            //specific check used in case of another object in game that had a shader that we did not want to change
            if (hit.transform.gameObject.tag != "Barrier")
            {
                //applies the default shader on the player in case the current shader is not the default - e.g. it could be the Always Visible Shader
                _Rend.material.shader = _Standard;
                //gets the reference from the object between player and camera
                _Obj = hit.transform.gameObject;
                if (_Obj.GetComponentInChildren<Renderer>() == null)
                {
                    return;
                }


                //saves the previous shader before switching shaders to apply it back in the begining when no longer is behind soemthing
                _Previous = _Obj.GetComponentInChildren<Renderer>().material.shader;
                //applies the dissolve mask shader
                _Obj.GetComponentInChildren<Renderer>().material.shader = _BehindWalls;
                
                //Debug.DrawLine(_Cam.Camera.transform.position, (_Player.Value.position + (_Player.Value.forward)), Color.blue, hit.distance);
                //Debug.Log("Did Hit a WALL");



                //updates the Shader data set - dissolve mask - based on the player's position
                //Note: to be able to visualize where the mask it is in 3D space in Unity, try using a gizmo based of the size and positionXYZ of the mask
                //if the mask is to small or if the player is too far away from the object but still behind an object, it will give not the desired outcome
                _MaskSmoothPoint = Vector3.MoveTowards(_Player.Value.position, hit.point, _ShaderData.MaskSmoothSpeed * Time.deltaTime);
                Vector4 pos = new Vector4(_MaskSmoothPoint.x, _MaskSmoothPoint.y, _MaskSmoothPoint.z, 0);
                Shader.SetGlobalVector("Globalmask_Position", pos);
                Shader.SetGlobalVector("Globalmask_Offset", _ShaderData.MaskOffset);
                Shader.SetGlobalFloat("Globalmask_Radius", _ShaderData.MaskRadius);
                Shader.SetGlobalFloat("Globalmask_Softness", _ShaderData.MaskSoftness);

                return;
            }
        }
    }
}
