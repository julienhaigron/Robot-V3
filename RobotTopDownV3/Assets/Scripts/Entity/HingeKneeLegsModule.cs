using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

public class HingeKneeLegsModule : LegsAnimatorControlModuleBase
{
	private struct Knee
	{
		public Transform bone;
		public Transform foot;
		public Quaternion rest;
		public Vector3 ankleAxis;
	}

	private readonly List<Knee> m_knees = new();

	public override void OnInit ( LegsAnimator.LegsAnimatorCustomModuleHelper _helper )
	{
		m_knees.Clear();
		Avatar avatar = LA.Mecanim != null ? LA.Mecanim.avatar : null;
		if (avatar == null || !avatar.isHuman)
			return;

		var restRotations = new Dictionary<string, Quaternion>();
		foreach (SkeletonBone bone in avatar.humanDescription.skeleton)
			restRotations[bone.name] = bone.rotation;

		foreach (LegsAnimator.Leg leg in LA.Legs)
		{
			if (leg.BoneMid == null || leg.BoneEnd == null || !restRotations.TryGetValue(leg.BoneMid.name, out Quaternion rest))
				continue;

			m_knees.Add(new Knee { bone = leg.BoneMid, foot = leg.BoneEnd, rest = rest, ankleAxis = leg.BoneMid.InverseTransformPoint(leg.BoneEnd.position).normalized });
		}
	}

	public override void OnAfterAnimatorCaptureUpdate ( LegsAnimator.LegsAnimatorCustomModuleHelper _helper )
	{
		float blend = EffectBlend;
		if (blend <= 0f)
			return;

		foreach (Knee knee in m_knees)
		{
			Quaternion deviation = Quaternion.Inverse(knee.rest) * knee.bone.localRotation;
			Vector3 twistAxis = Vector3.Project(new Vector3(deviation.x, deviation.y, deviation.z), knee.ankleAxis);
			var twist = new Vector4(twistAxis.x, twistAxis.y, twistAxis.z, deviation.w);
			float length = twist.magnitude;
			if (length < 1e-6f)
				continue;

			twist /= length;
			Quaternion footRotation = knee.foot.rotation;
			knee.bone.localRotation *= Quaternion.Inverse(Quaternion.SlerpUnclamped(Quaternion.identity, new Quaternion(twist.x, twist.y, twist.z, twist.w), blend));
			knee.foot.rotation = footRotation;
		}
	}

#if UNITY_EDITOR
	public override void Editor_InspectorGUI ( LegsAnimator _legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper _helper )
	{
		UnityEditor.EditorGUILayout.HelpBox("Genou en charnière : retire la torsion du bas de jambe autour de l'axe genou-cheville (celle des clips humains et du Foot IK d'Unity), sans bouger la cheville ni le pied. Sur une jambe dont le tibia est désaxé, cette torsion faisait balayer le tibia et trembler la jambe.", UnityEditor.MessageType.None);
	}
#endif
}
