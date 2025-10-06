using UnityEngine;

public class FogOfWarRenderer : MonoBehaviour
{
    public Camera fogCamera;
    public Material fogApplyMaterial;

    private RenderTexture m_fogMask; 

    void Start ()
    {
        /*if (fogCamera == null)
        {
            GameObject camObj = new GameObject("FogCamera");
            fogCamera = camObj.AddComponent<Camera>();
            fogCamera.enabled = false;
        }*/

        // Crée la RT
        m_fogMask = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        fogCamera.targetTexture = m_fogMask;

        // Injecte le mask dans le shader d’application
        fogApplyMaterial.SetTexture("_FogMask", m_fogMask);
    }

    void LateUpdate ()
    {
        // On rend les meshes de fog dans le mask
        fogCamera.RenderWithShader(Shader.Find("Custom/FogOfWar_Mask"), "");
    }
}
