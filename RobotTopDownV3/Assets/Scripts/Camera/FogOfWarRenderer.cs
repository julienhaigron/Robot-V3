using UnityEngine;

public class FogOfWarRenderer : Singleton<FogOfWarRenderer>
{
    public Camera fogCamera;
    public Material fogApplyMaterial;

    private RenderTexture m_fogMask;
    private Shader m_fogShader; 
    private bool m_dirty = true;

    void Start ()
    {
        // Crée la RT
        int scale = 2;
        m_fogMask = new RenderTexture(Screen.width / scale, Screen.height / scale, 16);
        fogCamera.targetTexture = m_fogMask;

        // Injecte le mask dans le shader d’application
        fogApplyMaterial.SetTexture("_FogMask", m_fogMask);

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

        // On rend les meshes de fog dans le mask
        if (fogCamera.targetTexture == null)
            return;

        fogCamera.RenderWithShader(m_fogShader, "");
    }
}
