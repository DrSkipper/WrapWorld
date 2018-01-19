using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/**
 * WorldWrapBehavior:
 * Handles the rendering of one side of a visual wrap or portal in the world.
 * Credit to the 'Gater (Portal System)' Unity package for a lot of the code (simplified here to focus soley on rendering).
 */
[ExecuteInEditMode]
public class WorldWrapBehavior : MonoBehaviour
{
    /**
     * PairedWrapObject:
     * The other end of this world wrap/portal
     */
    public WorldWrapBehavior PairedWrapObject;

    /**
     * Cameras:
     * The cameras to be used for rendering this side of the wrap. They are positioned automatically
     * Index 0: For in-game/player perspective
     * Index 1: For editor perspective
     */
    public Camera[] Cameras;

    /**
     * WrapRenderers:
     * The MeshRenderer object for rendering the view from the other side of the wrap.
     * Index 0: For in-game/player perspective
     * Index 1: For editor perspective
     */
    public MeshRenderer[] WrapRenderers;

    /**
     * ViewSettings:
     * Contains parameters for modifying the visual of the wrap to make it look wavy/shimmery and stuff.
     */
    [Serializable]
    public class ViewSettingsClass
    {
        [Serializable]
        public class ProjectionClass
        {
            public Vector2 Resolution = new Vector2(1280, 1024);

            public enum DepthQualityEnum { Fast, High };
            public DepthQualityEnum DepthQuality = DepthQualityEnum.High;
            [HideInInspector] public DepthQualityEnum CurrentDepthQuality;
        }
        public ProjectionClass Projection;

        [Serializable]
        public class RecursionClass
        {
            [Range(1, 20)] public int Steps = 1;
            public Material CustomFinalStep;
        }
        public RecursionClass Recursion;

        [Serializable]
        public class DistorsionClass
        {
            public bool EnableDistorsion;

            public Texture2D Pattern;
            public Color Color = new Color(1, 1, 1, 1);
            [Range(1, 100)] public int Tiling = 1;
            [Range(-10, 10)] public float SpeedX = .01f;
            [Range(-10, 10)] public float SpeedY = 0;
        }
        public DistorsionClass Distorsion;
    }
    public ViewSettingsClass ViewSettings;

    /**
     * ForceNoEditorUpdateDelegate:
     * Allows the wrap visualization to update in editor while the game isn't playing, may have side effects if the 'EditorApplication.update' callback is used somewhere, i.e. CSGModel code.
     */
    public bool ForceNoEditorUpdateDelegate = false;
    
    /**
     * Private
     */
    private Material[] PortalMaterials;
    private RenderTexture[] RenderTexts;
    private Vector2 CurrentProjectionResolution;

    void OnEnable()
    {
        // Create the render texture array. Index 0 is for in-game/player perspective, Index 1 is for the editor perspective.
#if UNITY_EDITOR
        RenderTexts = new RenderTexture[2];
#else
		RenderTexts = new RenderTexture[1];
#endif
        PortalMaterials = new Material[RenderTexts.Length];

        for (int i = 0; i < PortalMaterials.Length; i++) //Generate "Portal" and "Clipping plane" materials
            if (!PortalMaterials[i])
                PortalMaterials[i] = new Material(Shader.Find("Gater/UV Remap"));

        //Apply custom settings to the portal components
        for (int i = 0; i < this.WrapRenderers.Length; ++i)
        {
            this.WrapRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            this.WrapRenderers[i].receiveShadows = false;
            this.WrapRenderers[i].sharedMaterial = PortalMaterials[i];
        }

#if UNITY_EDITOR
        if (this.ForceNoEditorUpdateDelegate)
            EditorApplication.update = null;
#endif
    }

    private Mesh PortalMesh;
    private Camera InGameCamera;
    private RenderTexture TempRenderText;

    void LateUpdate()
    {
        if (!InGameCamera)
        {
            InGameCamera = Camera.main; //Fill empty "InGameCamera" variable with main camera
        }
        else
        {
            if (this.WrapRenderers.Length > 0)
                PortalMesh = this.WrapRenderers[0].GetComponent<MeshFilter>().sharedMesh; //Acquire current portal mesh

            ViewSettings.Projection.Resolution = new Vector2(ViewSettings.Projection.Resolution.x < 1 ? 1 : ViewSettings.Projection.Resolution.x, ViewSettings.Projection.Resolution.y < 1 ? 1 : ViewSettings.Projection.Resolution.y);

            //Generate render texture for the portal camera
            if (CurrentProjectionResolution.x != ViewSettings.Projection.Resolution.x || CurrentProjectionResolution.y != ViewSettings.Projection.Resolution.y || ViewSettings.Projection.CurrentDepthQuality != ViewSettings.Projection.DepthQuality)
            {
                if (TempRenderText)
                {
#if UNITY_EDITOR
                    DestroyImmediate(TempRenderText, false);
#else
					Destroy(TempRenderText);
#endif
                }
                else
                    TempRenderText = new RenderTexture(Convert.ToInt32(ViewSettings.Projection.Resolution.x), Convert.ToInt32(ViewSettings.Projection.Resolution.y), ViewSettings.Projection.DepthQuality == ViewSettingsClass.ProjectionClass.DepthQualityEnum.Fast ? 16 : 24);

                int RenderTextsLength = 0;

                for (int i = 0; i < RenderTexts.Length; i++)
                {
                    if (RenderTexts[i])
                    {
#if UNITY_EDITOR
                        if (!EditorApplication.isPlaying)
                            DestroyImmediate(RenderTexts[i], false);
                        if (EditorApplication.isPlaying)
                            Destroy(RenderTexts[i]);
#else
						Destroy(RenderTexts[i]);
#endif
                    }
                    else
                    {
                        RenderTexts[i] = new RenderTexture(Convert.ToInt32(ViewSettings.Projection.Resolution.x), Convert.ToInt32(ViewSettings.Projection.Resolution.y), ViewSettings.Projection.DepthQuality == ViewSettingsClass.ProjectionClass.DepthQualityEnum.Fast ? 16 : 24);
                        RenderTexts[i].name = this.gameObject.name + " RenderTexture " + i;

                        RenderTextsLength += 1;
                    }
                }

                if (RenderTextsLength == RenderTexts.Length)
                {
                    CurrentProjectionResolution = new Vector2(ViewSettings.Projection.Resolution.x, ViewSettings.Projection.Resolution.y);

                    ViewSettings.Projection.CurrentDepthQuality = ViewSettings.Projection.DepthQuality;
                }
            }

#if UNITY_EDITOR
            LayerMask SceneTabLayerMask = Tools.visibleLayers;

            SceneTabLayerMask &= ~(1 << 1); //Disable SceneviewRender layer on Sceneview

            Tools.visibleLayers = SceneTabLayerMask;

            //Generate projection plane for Sceneview
            if (this.WrapRenderers.Length > 1)
            {
                this.WrapRenderers[1].transform.localPosition = new Vector3(0, 0, .0001f);

                this.WrapRenderers[1].GetComponent<MeshFilter>().sharedMesh = PortalMesh;
                this.WrapRenderers[1].sharedMaterial = PortalMaterials[1];

                //Apply render texture to the scene portal material
                if (this.PortalMaterials.Length > 1)
                    this.WrapRenderers[1].sharedMaterial.SetTexture("_MainTex", InGameCamera && PairedWrapObject.RenderTexts[1] ? PairedWrapObject.RenderTexts[1] : null);
            }
#endif

            //Apply render texture to the game portal material
            if (PortalMaterials.Length > 0)
                this.WrapRenderers[0].sharedMaterial.SetTexture("_MainTex", InGameCamera && PairedWrapObject.RenderTexts[0] ? PairedWrapObject.RenderTexts[0] : null);

            //Manage distorstion pattern settings
            this.WrapRenderers[0].sharedMaterial.SetInt("_EnableDistorsionPattern", ViewSettings.Distorsion.EnableDistorsion ? 1 : 0);
            this.WrapRenderers[0].sharedMaterial.SetTexture("_DistorsionPattern", ViewSettings.Distorsion.Pattern);
            this.WrapRenderers[0].sharedMaterial.SetColor("_DistorsionPatternColor", ViewSettings.Distorsion.Color);
            this.WrapRenderers[0].sharedMaterial.SetInt("_DistorsionPatternTiling", ViewSettings.Distorsion.Tiling);
            this.WrapRenderers[0].sharedMaterial.SetFloat("_DistorsionPatternSpeedX", -ViewSettings.Distorsion.SpeedX);
            this.WrapRenderers[0].sharedMaterial.SetFloat("_DistorsionPatternSpeedY", -ViewSettings.Distorsion.SpeedY);

            //Generate camera for the portal rendering
            for (int j = 0; j < this.Cameras.Length; j++)
            {
                if (j < ViewSettings.Recursion.Steps + 1)
                {
                    if (this.Cameras[j] != null)
                    {
                        //Acquire settings from Scene/Game camera, to apply on Portal camera
                        if (InGameCamera != null)
                        {
                            this.Cameras[j].depth = InGameCamera.depth - 1;
                            this.Cameras[j].renderingPath = InGameCamera.renderingPath;
                            this.Cameras[j].useOcclusionCulling = InGameCamera.useOcclusionCulling;
                            this.Cameras[j].allowHDR = InGameCamera.allowHDR;
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR
                    if (!EditorApplication.isPlaying)
                        DestroyImmediate(this.Cameras[j].gameObject, false);
                    if (EditorApplication.isPlaying)
                        Destroy(this.Cameras[j].gameObject);
#else
						Destroy(this.Cameras[j].gameObject);
#endif
                }
            }

            //Move portal cameras and render it to rendertexture
            if (PairedWrapObject != null)
            {
                Vector3 PortalCamPos;
                Quaternion PortalCamRot;

                Camera SceneCamera = null;
#if UNITY_EDITOR
                SceneCamera = SceneView.GetAllSceneCameras().Length > 0 ? SceneView.GetAllSceneCameras()[0] : null;
#endif

                for (int i = 0; i < RenderTexts.Length; i++)
                {
                    if (RenderTexts[i])
                    {
                        for (int j = ViewSettings.Recursion.Steps; j >= 0; j--)
                        {
                            if (this.Cameras[i] != null)
                            {
                                //Move portal camera to position/rotation of Scene/Game camera
                                this.Cameras[i].aspect = (i == 1 && SceneCamera ? SceneCamera.aspect : InGameCamera.aspect);
                                this.Cameras[i].fieldOfView = (i == 1 && SceneCamera ? SceneCamera.fieldOfView : InGameCamera.fieldOfView);
                                this.Cameras[i].farClipPlane = (i == 1 && SceneCamera ? SceneCamera.farClipPlane : InGameCamera.farClipPlane);

                                PortalCamPos = PairedWrapObject.transform.InverseTransformPoint(i == 1 && SceneCamera ? SceneCamera.transform.position : InGameCamera.transform.position);

                                PortalCamPos.x = -PortalCamPos.x;
                                PortalCamPos.z = -PortalCamPos.z + j * (Vector3.Distance(transform.position, PairedWrapObject.transform.position) / 5);

                                PortalCamRot = Quaternion.Inverse(PairedWrapObject.transform.rotation) * (i == 1 && SceneCamera ? SceneCamera.transform.rotation : InGameCamera.transform.rotation);

                                PortalCamRot = Quaternion.AngleAxis(180.0f, new Vector3(0, 1, 0)) * PortalCamRot;

                                this.Cameras[i].transform.localPosition = PortalCamPos;
                                this.Cameras[i].transform.localRotation = PortalCamRot;

                                //Render inside portal cameras to render texture
                                /*if (j > 0 && j == ViewSettings.Recursion.Steps)
                                    this.Cameras[j].cullingMask = 0;
                                else
                                {
                                    this.Cameras[j].cullingMask = InGameCamera.cullingMask;
                                }*/

                                this.Cameras[i].targetTexture = TempRenderText;
                                this.Cameras[i].Render();

                                Graphics.Blit(TempRenderText, RenderTexts[i]);

                                this.Cameras[i].targetTexture = null;
                            }
                        }
                    }
                }
            }
        }
    }
}