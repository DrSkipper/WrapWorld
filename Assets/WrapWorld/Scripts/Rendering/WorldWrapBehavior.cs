using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WorldWrapBehavior : MonoBehaviour
{
    public WorldWrapBehavior PairedWrapObject;
    public Material PairedPortalSkybox;
    [HideInInspector] public Material NullPairedPortalSkybox;
    public GameObject[] PortalCamObjs;
    public GameObject SceneviewRender;

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

    //----------

    private Material[] PortalMaterials;
    private RenderTexture[] RenderTexts;
    private Vector2 CurrentProjectionResolution;
    private int[] InitPortalCamObjsCullingMask;

    void OnEnable()
    {
#if UNITY_EDITOR
        RenderTexts = new RenderTexture[2];
#else
			RenderTexts = new RenderTexture [1];
#endif
        PortalMaterials = new Material[RenderTexts.Length];
        
        InitPortalCamObjsCullingMask = new int[PortalCamObjs.Length];

        for (int i = 0; i < PortalMaterials.Length; i++) //Generate "Portal" and "Clipping plane" materials
            if (!PortalMaterials[i])
                PortalMaterials[i] = new Material(Shader.Find("Gater/UV Remap"));
        
        if (!NullPairedPortalSkybox)
            NullPairedPortalSkybox = new Material(Shader.Find("Standard"));

        //Apply custom settings to the portal components
        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GetComponent<MeshRenderer>().receiveShadows = false;
        GetComponent<MeshRenderer>().sharedMaterial = PortalMaterials[0];

#if UNITY_EDITOR
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
            if (InGameCamera.nearClipPlane > .01f)
                Debug.LogWarning("The nearClipPlane of 'Main Camera' is not equal to 0.01");

            PortalMesh = GetComponent<MeshFilter>().sharedMesh; //Acquire current portal mesh

            ViewSettings.Projection.Resolution = new Vector2(ViewSettings.Projection.Resolution.x < 1 ? 1 : ViewSettings.Projection.Resolution.x, ViewSettings.Projection.Resolution.y < 1 ? 1 : ViewSettings.Projection.Resolution.y);

            //Generate render texture for the portal camera
            if (CurrentProjectionResolution.x != ViewSettings.Projection.Resolution.x || CurrentProjectionResolution.y != ViewSettings.Projection.Resolution.y || ViewSettings.Projection.CurrentDepthQuality != ViewSettings.Projection.DepthQuality)
            {
                if (TempRenderText)
                {
#if UNITY_EDITOR
                    DestroyImmediate(TempRenderText, false);
#else
						Destroy (TempRenderText);
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
							Destroy (RenderTexts [i]);
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
            if (this.SceneviewRender != null)
            {
                SceneviewRender.layer = 4;

                SceneviewRender.transform.localPosition = new Vector3(0, 0, .0001f);

                SceneviewRender.GetComponent<MeshFilter>().sharedMesh = PortalMesh;
                SceneviewRender.GetComponent<MeshRenderer>().sharedMaterial = PortalMaterials[1];

                //Apply render texture to the scene portal material
                if (PortalMaterials.Length > 1)
                    SceneviewRender.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", InGameCamera && PairedWrapObject.RenderTexts[1] ? PairedWrapObject.RenderTexts[1] : null);
            }
#endif

            //Apply render texture to the game portal material
            if (PortalMaterials.Length > 0)
                GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", InGameCamera && PairedWrapObject.RenderTexts[0] ? PairedWrapObject.RenderTexts[0] : null);
            
            //Manage distorstion pattern settings
            GetComponent<MeshRenderer>().sharedMaterial.SetInt("_EnableDistorsionPattern", ViewSettings.Distorsion.EnableDistorsion ? 1 : 0);
            GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_DistorsionPattern", ViewSettings.Distorsion.Pattern);
            GetComponent<MeshRenderer>().sharedMaterial.SetColor("_DistorsionPatternColor", ViewSettings.Distorsion.Color);
            GetComponent<MeshRenderer>().sharedMaterial.SetInt("_DistorsionPatternTiling", ViewSettings.Distorsion.Tiling);
            GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DistorsionPatternSpeedX", -ViewSettings.Distorsion.SpeedX);
            GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_DistorsionPatternSpeedY", -ViewSettings.Distorsion.SpeedY);

            //Generate camera for the portal rendering
            /*for (int j = 0; j < PortalCamObjs.Length; j++)
            {
                if (j < ViewSettings.Recursion.Steps + 1)
                {
                    if (!PortalCamObjs[j])
                    {
                        PortalCamObjs[j] = new GameObject(transform.name + " Camera " + j);

                        PortalCamObjs[j].tag = "Untagged";

                        PortalCamObjs[j].transform.parent = transform;
                        PortalCamObjs[j].AddComponent<Camera>();
                        PortalCamObjs[j].GetComponent<Camera>().enabled = false;
                        InitPortalCamObjsCullingMask[j] = PortalCamObjs[j].GetComponent<Camera>().cullingMask;
                        PortalCamObjs[j].GetComponent<Camera>().nearClipPlane = .01f;

                        PortalCamObjs[j].AddComponent<Skybox>();
                    }
                    else
                    {
                        if (PortalCamObjs[j].name != transform.name + " Camera " + j)
                            PortalCamObjs[j].name = transform.name + " Camera " + j;

                        if (PortalCamObjs[j].GetComponent<Camera>().depth != InGameCamera.depth - 1)
                            PortalCamObjs[j].GetComponent<Camera>().depth = InGameCamera.depth - 1;

                        //Acquire settings from Scene/Game camera, to apply on Portal camera
                        if (InGameCamera)
                        {
                            PortalCamObjs[j].GetComponent<Camera>().renderingPath = InGameCamera.renderingPath;
                            PortalCamObjs[j].GetComponent<Camera>().useOcclusionCulling = InGameCamera.useOcclusionCulling;
                            PortalCamObjs[j].GetComponent<Camera>().allowHDR = InGameCamera.allowHDR;
                        }
                    }

                    if (PairedWrapObject.PortalCamObjs[j])
                        PairedWrapObject.PortalCamObjs[j].GetComponent<Skybox>().material = ViewSettings.Recursion.CustomFinalStep && (j > 0 && j == ViewSettings.Recursion.Steps) ? ViewSettings.Recursion.CustomFinalStep : (!PairedPortalSkybox && (j > 0 && j == ViewSettings.Recursion.Steps) ? NullPairedPortalSkybox : PairedPortalSkybox);
                }
                else
                {
#if UNITY_EDITOR
                    if (!EditorApplication.isPlaying)
                        DestroyImmediate(PortalCamObjs[j], false);
                    if (EditorApplication.isPlaying)
                        Destroy(PortalCamObjs[j]);
#else
						Destroy (PortalCamObjs [j]);
#endif
                }
            }*/

            gameObject.layer = 1;

            //Move portal cameras and render it to rendertexture
            if (PairedWrapObject != null)
            {
                Vector3[] PortalCamPos = new Vector3[PortalCamObjs.Length];
                Quaternion[] PortalCamRot = new Quaternion[PortalCamObjs.Length];
                
                for (int i = 0; i < RenderTexts.Length; i++)
                {
                    if (RenderTexts[i])
                    {
                        for (int j = ViewSettings.Recursion.Steps; j >= 0; j--)
                        {
                            if (PortalCamObjs[j])
                            {
                                //Move portal camera to position/rotation of Scene/Game camera
                                Camera SceneCamera = null;

#if UNITY_EDITOR
                                SceneCamera = SceneView.GetAllSceneCameras().Length > 0 ? SceneView.GetAllSceneCameras()[0] : null;
#endif

                                PortalCamObjs[j].GetComponent<Camera>().aspect = (i == 1 && SceneCamera ? SceneCamera.aspect : InGameCamera.aspect);
                                PortalCamObjs[j].GetComponent<Camera>().fieldOfView = (i == 1 && SceneCamera ? SceneCamera.fieldOfView : InGameCamera.fieldOfView);
                                PortalCamObjs[j].GetComponent<Camera>().farClipPlane = (i == 1 && SceneCamera ? SceneCamera.farClipPlane : InGameCamera.farClipPlane);

                                PortalCamPos[j] = PairedWrapObject.transform.InverseTransformPoint(i == 1 && SceneCamera ? SceneCamera.transform.position : InGameCamera.transform.position);

                                PortalCamPos[j].x = -PortalCamPos[j].x;
                                PortalCamPos[j].z = -PortalCamPos[j].z + j * (Vector3.Distance(transform.position, PairedWrapObject.transform.position) / 5);

                                PortalCamRot[j] = Quaternion.Inverse(PairedWrapObject.transform.rotation) * (i == 1 && SceneCamera ? SceneCamera.transform.rotation : InGameCamera.transform.rotation);

                                PortalCamRot[j] = Quaternion.AngleAxis(180.0f, new Vector3(0, 1, 0)) * PortalCamRot[j];

                                PortalCamObjs[j].transform.localPosition = PortalCamPos[j];
                                PortalCamObjs[j].transform.localRotation = PortalCamRot[j];

                                //Render inside portal cameras to render texture
                                if (j > 0 && j == ViewSettings.Recursion.Steps)
                                    PortalCamObjs[j].GetComponent<Camera>().cullingMask = 0;
                                else
                                {
                                    PortalCamObjs[j].GetComponent<Camera>().cullingMask = InGameCamera.cullingMask;
                                }

                                PortalCamObjs[j].GetComponent<Camera>().targetTexture = TempRenderText;

                                PortalCamObjs[j].GetComponent<Camera>().Render();

                                Graphics.Blit(TempRenderText, RenderTexts[i]);

                                PortalCamObjs[j].GetComponent<Camera>().targetTexture = null;
                            }
                        }
                    }
                }
            }
        }
    }

    class InitMaterialsList { public Material[] Materials; }
    private bool AcquireNextPos;
    private bool[] StandardObjShader = new bool[0];
}