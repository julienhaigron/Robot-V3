using UnityEngine;

public class FogOfWarRenderer : Singleton<FogOfWarRenderer>
{
    public Camera fogCamera;
    public Material fogApplyMaterial;

    private RenderTexture m_fogMask;
    private RenderTexture m_visibleMask;
    private Shader m_fogShader; 
    private bool m_dirty = true;

    void Start ()
    {
        //fogCamera.targetTexture = m_fogMask;
        // Crée la RT
        int scale = 2;
        m_fogMask = new RenderTexture(Screen.width / scale, Screen.height / scale, 16);
        m_visibleMask = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.R8);
        m_visibleMask.filterMode = FilterMode.Point;
        m_visibleMask.wrapMode = TextureWrapMode.Clamp;

        // Injecte le mask dans le shader d’application
        fogApplyMaterial.SetTexture("_FogMask", m_fogMask);
        fogApplyMaterial.SetTexture("_VisibleMask", m_visibleMask);

        m_fogShader = Shader.Find("Custom/FogOfWar_Mask");
    }

    public void MarkDirty ()
    {
        m_dirty = true;
    }

    void LateUpdate ()
    {
        if (!m_dirty)
            return;

        m_dirty = false;

        if (m_fogMask == null || m_visibleMask == null)
            return;

        fogCamera.targetTexture = m_fogMask;
        fogCamera.RenderWithShader(m_fogShader, "");

        fogCamera.targetTexture = m_visibleMask;
        fogCamera.RenderWithShader(m_fogShader, "");

        fogCamera.targetTexture = m_fogMask;
    }
}
