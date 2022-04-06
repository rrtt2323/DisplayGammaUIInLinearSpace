/*
 * AuthorNote:
 * Created By: WangYu  Date: 2022-03-24
*/

using UnityEngine;
using UnityEngine.Rendering;

namespace rrtt_2323.DisplayGammaUIInLinearSpace
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class SceneCameraCombiner : MonoBehaviour
    {
        public UICameraGammaConverter uiConverter;
        
        public float showMeshDistance = 10;

        public bool biasGammaSpaceColor;

        
        private Material m_blendMat;
        private Camera m_camera;
        
        private RenderTexture m_sceneColorRT;
        private RenderTexture m_sceneDepthRT;

        private Mesh m_cameraShowMesh;
        private Material m_cameraShowMaterial;

        private static readonly int _ScreenColorTexture = Shader.PropertyToID("_ScreenColorTexture");
        private static readonly int _ScreenDepthTexture = Shader.PropertyToID("_ScreenDepthTexture");
        private const string _BIAS_GAMMA_SPACE_COLOR_ON = "_BIAS_GAMMA_SPACE_COLOR_ON";

        private void OnDisable()
        {
            if (m_blendMat != null)
            {
                UnityEngine.Object.DestroyImmediate(m_blendMat);
                m_blendMat = null;
            }

            m_camera.RemoveAllCommandBuffers();
            m_camera = null;

            if (m_sceneColorRT != null)
            {
                RenderTexture.ReleaseTemporary(m_sceneColorRT);
                m_sceneColorRT = null;
            }

            if (m_sceneDepthRT != null)
            {
                RenderTexture.ReleaseTemporary(m_sceneDepthRT);
                m_sceneDepthRT = null;
            }

            if (m_cameraShowMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(m_cameraShowMesh);
                m_cameraShowMesh = null;
            }

            if (m_cameraShowMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(m_cameraShowMaterial);
                m_cameraShowMaterial = null;
            }
        }

        private void OnEnable()
        {
            #if UNITY_EDITOR
            uiConverter = FindObjectOfType<UICameraGammaConverter>();
            #endif
            if (uiConverter == null)
            {
                return;
            }
            
            m_blendMat = new Material(Shader.Find("Hidden/rrtt_2323/BlitProcessing"));
            if (m_blendMat == null)
            {
                return;
            }
            
            m_camera = this.transform.GetComponent<Camera>();
            if (m_camera == null)
            {
                return;
            }
            // 确保场景摄像机在ui摄照相机之后绘制
            m_camera.depth = uiConverter.CameraDepth + 1;

            CreateRT();
            CreatedCommandBuffer();
            SwitchPreference();
        }

        private void OnValidate()
        {
            SwitchPreference();
        }
        
        
        private void CreateRT()
        {
            m_sceneColorRT = RenderTexture.GetTemporary(
                m_camera.pixelWidth, m_camera.pixelHeight,
                0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_sceneColorRT.name = "SceneColorRT";

            m_sceneDepthRT = RenderTexture.GetTemporary(
                m_camera.pixelWidth, m_camera.pixelHeight,
                24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_sceneDepthRT.name = "SceneDepthRT";
        }

        private void CreatedCommandBuffer()
        {
            CommandBuffer cBuffer = new CommandBuffer();
            cBuffer.name = "Blend ui and scene camera - cBuffer";
            
            // 发送给Shader
            cBuffer.SetGlobalTexture(_ScreenColorTexture, m_sceneColorRT);
            cBuffer.SetGlobalTexture(_ScreenDepthTexture, m_sceneDepthRT);
            
            // 清屏
            cBuffer.ClearRenderTarget(true, true, Color.black);
            
            // 绘制一个Quad来显示屏幕
            m_cameraShowMesh = CreateCamShowMesh(m_camera, this.showMeshDistance);
            m_cameraShowMaterial = new Material(Shader.Find("Hidden/rrtt_2323/CommandBufferTex"));
            cBuffer.DrawMesh(m_cameraShowMesh, Matrix4x4.identity, m_cameraShowMaterial);
            
            m_camera.AddCommandBuffer(CameraEvent.AfterImageEffects, cBuffer);
        }

        // 通过屏幕4个点和距离绘制一个 quad
        private Mesh CreateCamShowMesh(Camera camera, float distance)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = camera.ScreenToWorldPoint(new Vector3(0, 0, distance));
            vertices[1] = camera.ScreenToWorldPoint(new Vector3(0, camera.pixelHeight, distance));
            vertices[2] = camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, camera.pixelHeight, distance));
            vertices[3] = camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, 0, distance));

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, 1);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(1, 0);

            int[] triangleID = new int[6];
            triangleID[0] = 0;
            triangleID[1] = 1;
            triangleID[2] = 2;
            triangleID[3] = 2;
            triangleID[4] = 3;
            triangleID[5] = 0;

            Mesh drawMesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                triangles = triangleID
            };

            return drawMesh;
        }
        
        private void SwitchPreference()
        {
            if (this.biasGammaSpaceColor)
            {
                Shader.EnableKeyword(_BIAS_GAMMA_SPACE_COLOR_ON);
            }
            else
            {
                Shader.DisableKeyword(_BIAS_GAMMA_SPACE_COLOR_ON);
            }
        }

        
        private void OnPreRender()
        {
            if (m_camera == null)
            {
                return;
            }
            
            m_sceneColorRT.DiscardContents(true, true);
            m_sceneDepthRT.DiscardContents(true, true);

            m_camera.SetTargetBuffers(m_sceneColorRT.colorBuffer, m_sceneDepthRT.depthBuffer);
        }

        private void OnPostRender()
        {
            if (uiConverter == null || m_camera == null || m_blendMat == null)
            {
                return;
            }
            
            m_camera.targetTexture = null;
            
            Graphics.Blit(uiConverter.linearRT, m_sceneColorRT, m_blendMat, 1);
        }

        
        /*
        // 不用 CommandBuffer，直接用后处理来实现混合
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(uiConverter.linearRT, src, m_blendMat, 1);
            Graphics.Blit(src, dest);
        }
        */
    }
}