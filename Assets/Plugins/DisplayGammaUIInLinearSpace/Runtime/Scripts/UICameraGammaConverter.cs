/*
 * AuthorNote:
 * Created By: WangYu  Date: 2022-03-25
*/

using UnityEngine;
using UnityEngine.Rendering;

namespace rrtt_2323.DisplayGammaUIInLinearSpace
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class UICameraGammaConverter : MonoBehaviour
    {
        public RenderTexture linearRT;
        
        private Material m_blendMat;
        private Camera m_camera;
        
        public float CameraDepth => m_camera != null ? m_camera.depth : 0;

        private void OnDisable()
        {
            if (m_blendMat != null)
            {
                UnityEngine.Object.DestroyImmediate(m_blendMat);
                m_blendMat = null;
            }
            
            m_camera.RemoveAllCommandBuffers();
            m_camera = null;

            if (linearRT != null)
            {
                RenderTexture.ReleaseTemporary(linearRT);
                linearRT = null;
            }
        }

        private void OnEnable()
        {
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
            m_camera.clearFlags = CameraClearFlags.SolidColor;
            m_camera.backgroundColor = new Color(0, 0, 0, 0);
            
            linearRT = RenderTexture.GetTemporary(
                m_camera.pixelWidth, m_camera.pixelHeight, 
                24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            linearRT.DiscardContents(true, true);

            CreatedCommandBuffer();
        }

        private void CreatedCommandBuffer()
        {
            CommandBuffer cBuffer = new CommandBuffer();
            cBuffer.name = "ui gamma to linear - cBuffer";
            
            cBuffer.Blit(
                BuiltinRenderTextureType.CameraTarget, linearRT, 
                m_blendMat, 0);

            m_camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cBuffer);
        }
    }
}