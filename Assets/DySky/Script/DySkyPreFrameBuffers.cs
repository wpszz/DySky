using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DySkyPreFrameBuffers : MonoBehaviour {

    public enum BlitEventTrigger
    {
        Off = -1,

        AfterForwardOpaque  = CameraEvent.AfterForwardOpaque,
        BeforeImageEffects  = CameraEvent.BeforeImageEffects,

        BeforeForwardOpaque = CameraEvent.BeforeForwardOpaque,
        BeforeForwardAlpha  = CameraEvent.BeforeForwardAlpha,
    }

    public enum ColorPrecision
    {
        Off = -1,

        HalfRGB111110Float  = 0x200 | RenderTextureFormat.RGB111110Float,

        FullRGB111110Float  = 0x100 | RenderTextureFormat.RGB111110Float,
    }

    public enum DepthPrecision
    {
        Off = -1,

        HalfR8              = 0x200 | RenderTextureFormat.R8,
        HalfRHalf           = 0x200 | RenderTextureFormat.RHalf,
        HalfR16             = 0x200 | RenderTextureFormat.R16,

        FullR8              = 0x100 | RenderTextureFormat.R8,
        FullRHalf           = 0x100 | RenderTextureFormat.RHalf,
        FullR16             = 0x100 | RenderTextureFormat.R16,
    }

    [SerializeField]
    BlitEventTrigger BlitEvent = BlitEventTrigger.Off;
    [SerializeField]
    ColorPrecision BlitColor = ColorPrecision.HalfRGB111110Float;
    [SerializeField]
    DepthPrecision BlitDepth = DepthPrecision.HalfRHalf;

    [SerializeField]
    Camera NextCamera = null;
    [SerializeField]
    BlitEventTrigger BlitEventNext = BlitEventTrigger.BeforeForwardAlpha;

    Camera _mCamera;
    Camera mCamera
    {
        get
        {
            if (!_mCamera)
                _mCamera = GetComponent<Camera>();
            return _mCamera;
        }
    }

    RenderTexture mSrcColorBufferRT;
    RenderTexture mSrcDepthBufferRT;

    RenderTexture mDestColorBufferRT;
    RenderTexture mDestDepthBufferRT;

    CommandBuffer mCommandBuffer;

    BlitEventTrigger mBlitEvent;
    ColorPrecision mBlitColor;
    DepthPrecision mBlitDepth;

    Camera mNextCamera;
    CommandBuffer mCommandBufferNext;
    BlitEventTrigger mBlitEventNext;

    internal static readonly int ID_MainTex                     = Shader.PropertyToID("_MainTex");
    internal static readonly int ID_PreFrameCameraDepthTexture  = Shader.PropertyToID("_CameraDepthTexture");
    internal static readonly int ID_PreFrameCameraColorTexture  = Shader.PropertyToID("_CameraColorTexture");

    internal static HashSet<Camera> sRef = new HashSet<Camera>();

    private void OnEnable()
    {
        if (BlitEvent == BlitEventTrigger.Off)
        {
            this.enabled = false;
            return;
        }
        mBlitEvent = BlitEvent;
        mBlitColor = BlitColor;
        mBlitDepth = BlitDepth;
        BindBuffer();
        sRef.Add(mCamera);
    }

    private void OnDisable()
    {
        ReleaseBuffer();
        sRef.Remove(mCamera);
    }

    private void OnPostRender()
    {
        if (NextCamera != mNextCamera || BlitEventNext != mBlitEventNext)
        {
            ReleaseBufferNext();
            mNextCamera = NextCamera;
            mBlitEventNext = BlitEventNext;
        }
        if (mBlitEventNext != BlitEventTrigger.Off && mNextCamera && mNextCamera.enabled && mNextCamera.gameObject.activeInHierarchy)
        {
            if (mCommandBufferNext == null)
            {
                mCommandBufferNext = new CommandBuffer();
                mCommandBufferNext.name = "Blit To Next Camera";
                mCommandBufferNext.SetGlobalTexture(ID_MainTex, mSrcColorBufferRT);
                mCommandBufferNext.DrawMesh(DySkyUtils.FullscreenQuad, Matrix4x4.identity, DySkyUtils.CopyMaterial, 0, 0);
                mNextCamera.AddCommandBuffer((CameraEvent)mBlitEventNext, mCommandBufferNext);
                //mNextCamera.clearFlags = CameraClearFlags.Nothing;
            }
        }
        else
        {
            ReleaseBufferNext();

            Graphics.Blit(mSrcColorBufferRT, (RenderTexture)null);
        }
    }

    private void BindBuffer()
    {
        mSrcColorBufferRT = RenderTexture.GetTemporary(mCamera.pixelWidth, mCamera.pixelHeight, 0);
        mSrcDepthBufferRT = RenderTexture.GetTemporary(mCamera.pixelWidth, mCamera.pixelHeight, 24, RenderTextureFormat.Depth);
        mCommandBuffer = new CommandBuffer();
        mCommandBuffer.name = "Blit Target Buffers";
        if (mBlitColor != ColorPrecision.Off)
        {
            int down = ((int)mBlitColor >> 8);
            RenderTextureFormat format = (RenderTextureFormat)((int)mBlitColor & 0xff);
            mDestColorBufferRT = RenderTexture.GetTemporary(mCamera.pixelWidth / down, mCamera.pixelHeight / down, 0, format);
            Shader.SetGlobalTexture(ID_PreFrameCameraColorTexture, mDestColorBufferRT);
            mCommandBuffer.Blit(mSrcColorBufferRT.depthBuffer, mDestColorBufferRT.colorBuffer);
        }
        if (mBlitDepth != DepthPrecision.Off)
        {
            int down = ((int)mBlitDepth >> 8);
            RenderTextureFormat format = (RenderTextureFormat)((int)mBlitDepth & 0xff);
            mDestDepthBufferRT = RenderTexture.GetTemporary(mCamera.pixelWidth / down, mCamera.pixelHeight / down, 0, format);
            Shader.SetGlobalTexture(ID_PreFrameCameraDepthTexture, mDestDepthBufferRT);
            mCommandBuffer.Blit(mSrcDepthBufferRT.depthBuffer, mDestDepthBufferRT.colorBuffer);

            mCamera.depthTextureMode &= ~(DepthTextureMode.Depth | DepthTextureMode.DepthNormals);
        }
        mCamera.AddCommandBuffer((CameraEvent)mBlitEvent, mCommandBuffer);
        mCamera.SetTargetBuffers(mSrcColorBufferRT.colorBuffer, mSrcDepthBufferRT.depthBuffer);
    }

    private void ReleaseBuffer()
    {
        ReleaseBufferNext();

        mCamera.targetTexture = null;
        mCamera.RemoveCommandBuffer((CameraEvent)mBlitEvent, mCommandBuffer);

        if (mCommandBuffer != null)
        {
            mCommandBuffer.Release();
            mCommandBuffer = null;
        }
        if (mSrcColorBufferRT != null)
        {
            RenderTexture.ReleaseTemporary(mSrcColorBufferRT);
            mSrcColorBufferRT = null;
        }
        if (mSrcDepthBufferRT)
        {
            RenderTexture.ReleaseTemporary(mSrcDepthBufferRT);
            mSrcDepthBufferRT = null;
        }
        if (mDestColorBufferRT != null)
        {
            RenderTexture.ReleaseTemporary(mDestColorBufferRT);
            mDestColorBufferRT = null;
        }
        if (mDestDepthBufferRT)
        {
            RenderTexture.ReleaseTemporary(mDestDepthBufferRT);
            mDestDepthBufferRT = null;
        }
    }

    private void ReleaseBufferNext()
    {
        if (mCommandBufferNext != null)
        {
            if (mNextCamera)
                mNextCamera.RemoveCommandBuffer((CameraEvent)mBlitEventNext, mCommandBufferNext);
            mCommandBufferNext.Release();
            mCommandBufferNext = null;
        }
    }

    public static bool IsEnable(Camera camera)
    {
        return sRef.Contains(camera);
    }

    public static bool IsColorEnable(Camera camera)
    {
        if (!IsEnable(camera)) return false;
        DySkyPreFrameBuffers pfb = camera.GetComponent<DySkyPreFrameBuffers>();
        return pfb.mBlitColor != ColorPrecision.Off;
    }

    public static bool IsDepthEnable(Camera camera)
    {
        if (!IsEnable(camera)) return false;
        DySkyPreFrameBuffers pfb = camera.GetComponent<DySkyPreFrameBuffers>();
        return pfb.mBlitDepth != DepthPrecision.Off;
    }

    public static void BindCamera(Camera camera, 
        BlitEventTrigger evt = BlitEventTrigger.AfterForwardOpaque, 
        ColorPrecision color = ColorPrecision.HalfRGB111110Float, 
        DepthPrecision depth = DepthPrecision.HalfRHalf)
    {
        if (!camera) return;
        if (evt == BlitEventTrigger.Off)
        {
            ReleaseCamera(camera);
            return;
        }
        DySkyPreFrameBuffers pfb;
        if (IsEnable(camera))
        {
            pfb = camera.GetComponent<DySkyPreFrameBuffers>();
            if (pfb.mBlitColor == color && pfb.mBlitDepth == depth && pfb.mBlitEvent == evt) return;
            pfb.enabled = false;
        }
        else
        {
            pfb = camera.GetComponent<DySkyPreFrameBuffers>();
            if (!pfb) pfb = camera.gameObject.AddComponent<DySkyPreFrameBuffers>();
        }
        pfb.BlitEvent = evt;
        pfb.BlitColor = color;
        pfb.BlitDepth = depth;
        pfb.enabled = true;
    }

    public static void ReleaseCamera(Camera camera)
    {
        if (!camera) return;
        if (IsEnable(camera))
        {
            DySkyPreFrameBuffers pfb = camera.GetComponent<DySkyPreFrameBuffers>();
            pfb.enabled = false;
        }
    }
}
