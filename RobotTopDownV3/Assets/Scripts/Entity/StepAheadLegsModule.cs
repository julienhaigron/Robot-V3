using FIMSpace.FProceduralAnimation;
using UnityEngine;

public class StepAheadLegsModule : LegsAnimatorControlModuleBase
{
    public const string AheadTimeVariable = "Avance du pas (s)";
    public const string TeleportSpeedVariable = "Seuil de téléport (m/s)";
    private const float DefaultAheadTime = 0.2f;
    private const float DefaultTeleportSpeed = 6f;
    private const float VelocitySmoothTime = 0.1f;

    private LegsAnimator.Variable m_aheadTime;
    private LegsAnimator.Variable m_teleportSpeed;
    private Vector3 m_lastPosition;
    private Vector3 m_velocity;
    private Vector3 m_velocitySmoothing;
    private Vector3 m_offset;

    public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper _helper)
    {
        m_aheadTime = _helper.RequestVariable(AheadTimeVariable, DefaultAheadTime);
        m_teleportSpeed = _helper.RequestVariable(TeleportSpeedVariable, DefaultTeleportSpeed);
        ResetMotion();
    }

    public override void OnReInitialize(LegsAnimator.LegsAnimatorCustomModuleHelper _helper)
    {
        ResetMotion();
    }

    private void ResetMotion()
    {
        m_lastPosition = LA.BaseTransform.position;
        m_velocity = Vector3.zero;
        m_velocitySmoothing = Vector3.zero;
        m_offset = Vector3.zero;
    }

    public override void OnAfterAnimatorCaptureUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper _helper)
    {
        Vector3 position = LA.BaseTransform.position;
        Vector3 delta = position - m_lastPosition;
        m_lastPosition = position;

        float deltaTime = LA.DeltaTime;
        if (deltaTime <= 0f) return;

        Vector3 localVelocity = LA.ToRootLocalSpaceVec(delta / deltaTime);
        localVelocity.y = 0f;
        if (localVelocity.magnitude > m_teleportSpeed.GetFloat())
        {
            m_velocity = Vector3.zero;
            m_velocitySmoothing = Vector3.zero;
            m_offset = Vector3.zero;
            return;
        }

        m_velocity = Vector3.SmoothDamp(m_velocity, LA.RootToWorldSpaceVec(localVelocity), ref m_velocitySmoothing, VelocitySmoothTime, Mathf.Infinity, deltaTime);
        m_offset = m_velocity * (m_aheadTime.GetFloat() * EffectBlend);
    }

    public override void Leg_LatePreRaycastingUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper _helper, LegsAnimator.Leg _leg)
    {
        if (m_offset.sqrMagnitude < 1e-8f) return;
        _leg.OverrideFinalAndSourceIKPos(_leg.GetFinalIKPos() + m_offset);
        _leg.OverrideControlPositionsWithCurrentIKState();
    }

#if UNITY_EDITOR
    public override void Editor_InspectorGUI(LegsAnimator _legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper _helper)
    {
        UnityEditor.EditorGUILayout.HelpBox("Pose chaque pas en avant du corps, le long de sa vitesse : le pied repasse sous les hanches et le genou se lève. 0,2 s pose le pied à environ 0,35 m devant à la vitesse de marche. Un déplacement plus rapide que le seuil (téléport, réapparition d'un fantôme) remet l'avance à zéro.", UnityEditor.MessageType.None);

        LegsAnimator.Variable aheadTime = _helper.RequestVariable(AheadTimeVariable, DefaultAheadTime);
        aheadTime.SetMinMaxSlider(0f, 0.5f);
        aheadTime.Editor_DisplayVariableGUI();

        LegsAnimator.Variable teleportSpeed = _helper.RequestVariable(TeleportSpeedVariable, DefaultTeleportSpeed);
        teleportSpeed.SetMinMaxSlider(2f, 20f);
        teleportSpeed.Editor_DisplayVariableGUI();
    }
#endif
}
