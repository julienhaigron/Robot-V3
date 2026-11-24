using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraManager : Singleton<CameraManager>
{
    public static Action onFollowUnitsChanged;

    [SerializeField] private GameObject m_cameraParent;
    public GameObject CameraParent => m_cameraParent;
    [SerializeField] private Camera m_camera;
    public Camera Camera => m_camera;

    [SerializeField] private Transform m_defaultCameraPosition;
    public float DefaultZoomDistance => m_defaultCameraPosition == null ? m_cameraParent.transform.position.y : m_defaultCameraPosition.position.y;

    [SerializeField] private Transform m_startScreenCameraTfm;
    [SerializeField] private float m_startScreenFocalLength = 300f;
    [SerializeField] private float m_startScreenFarClipPlane = 1200f;
    [SerializeField] private Transform m_hubCameraTfm;

    private float m_defaultFieldOfView;
    private float m_defaultFarClipPlane;
    private Tween m_cameraMoveTween;

    [Header("Follow Units")]
    [SerializeField] private float m_followSmoothTime = .35f;

    private bool m_canFollowUnits;
    public bool CanFollowUnits => m_canFollowUnits;
    private bool m_isFollowingUnits;
    public bool IsFollowingUnits => m_isFollowingUnits;

    private Vector3? m_followedPoint;
    private Vector3 m_followVelocity;

    private Vector2 XLimits => GridManager.Instance == null ? Vector2.zero : new Vector2(0, GridManager.Instance.GridData.width * Tile.innerRadius * 2f);
    private Vector2 ZLimits => GridManager.Instance == null ? Vector2.zero : new Vector2(0, GridManager.Instance.GridData.height * 1.5f);

    public override void Awake ()
    {
        base.Awake();
        m_defaultFieldOfView = m_camera.fieldOfView;
        m_defaultFarClipPlane = m_camera.farClipPlane;
        TurnManager.onEndInputPhase += OnEndInputPhase;
        TurnManager.onStartInputPhase += OnStartInputPhase;
        TurnManager.onEndLevel += OnEndLevel;
    }

    private void OnDestroy ()
    {
        TurnManager.onEndInputPhase -= OnEndInputPhase;
        TurnManager.onStartInputPhase -= OnStartInputPhase;
        TurnManager.onEndLevel -= OnEndLevel;
    }

    private void LateUpdate ()
    {
        if (!m_isFollowingUnits || !IsPlayPhase())
            return;

        if (TryGetMovingUnitsPoint(out Vector3 movingUnitsPoint))
            m_followedPoint = movingUnitsPoint;

        if (!m_followedPoint.HasValue)
            return;

        Vector3 followedPoint = m_followedPoint.Value;
        Vector3 parentPosition = m_cameraParent.transform.position;
        Vector3 focusOffset = GetFocusOffset(followedPoint.y);
        Vector3 targetPosition = ClampToBounds(new Vector3(followedPoint.x - focusOffset.x, parentPosition.y, followedPoint.z - focusOffset.z));
        Vector3 newPosition = Vector3.SmoothDamp(parentPosition, targetPosition, ref m_followVelocity, m_followSmoothTime);

        if ((newPosition - parentPosition).sqrMagnitude < 1e-8f)
            return;

        m_cameraParent.transform.position = newPosition;
    }

    private bool IsPlayPhase ()
    {
        return TurnManager.Instance != null
            && (TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Calculating || TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Playing);
    }

    private bool TryGetMovingUnitsPoint ( out Vector3 _point )
    {
        _point = Vector3.zero;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.PlayersEntityAnchor == null || gameManager.PlayerID >= gameManager.PlayersEntityAnchor.Length)
            return false;

        int movingUnitCount = 0;
        foreach (Entity entity in gameManager.PlayersEntityAnchor[gameManager.PlayerID].Entities)
        {
            if (entity == null || entity.Equipment.IsDead || !entity.Displacement.DidMoveThisTick)
                continue;

            Tile tile = entity.Displacement.Coordinates.GetTile();
            Vector3 entityPosition = entity.transform.position;
            _point += new Vector3(entityPosition.x, tile == null ? entityPosition.y : tile.transform.position.y, entityPosition.z);
            movingUnitCount++;
        }

        if (movingUnitCount == 0)
            return false;

        _point /= movingUnitCount;
        return true;
    }

    public void FollowUnits ()
    {
        if (m_canFollowUnits)
            SetFollowState(true, true);
    }

    public void StopFollowingUnits ()
    {
        if (m_isFollowingUnits)
            SetFollowState(m_canFollowUnits, false);
    }

    private void OnEndInputPhase ()
    {
        m_followedPoint = null;
        SetFollowState(true, true);
    }

    private void OnStartInputPhase ()
    {
        SetFollowState(false, false);
    }

    private void OnEndLevel ()
    {
        SetFollowState(false, false);
    }

    private void SetFollowState ( bool _canFollow, bool _isFollowing )
    {
        m_followVelocity = Vector3.zero;

        if (m_canFollowUnits == _canFollow && m_isFollowingUnits == _isFollowing)
            return;

        m_canFollowUnits = _canFollow;
        m_isFollowingUnits = _isFollowing;
        onFollowUnitsChanged?.Invoke();
    }

    public Vector3 ClampToBounds ( Vector3 _position )
    {
        _position.x = Mathf.Clamp(_position.x, XLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.x, XLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.x);
        _position.z = Mathf.Clamp(_position.z, ZLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.y, ZLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.y);
        return _position;
    }

    public Vector3 GetFocusOffset ( float _groundHeight )
    {
        Ray centerRay = m_camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, _groundHeight, 0f));

        if (!groundPlane.Raycast(centerRay, out float distance))
            return Vector3.zero;

        Vector3 focusPoint = centerRay.GetPoint(distance);
        Vector3 parentPosition = m_cameraParent.transform.position;

        return new Vector3(focusPoint.x - parentPosition.x, 0f, focusPoint.z - parentPosition.z);
    }

    public void ResetPosition ()
	{
        TeleportCameraTo(m_defaultCameraPosition);
	}

    public void ShowStartScreen ()
    {
        PlaceCameraAt(m_startScreenCameraTfm);

        m_camera.usePhysicalProperties = true;
        m_camera.sensorSize = new Vector2(36f, 24f);
        m_camera.gateFit = Camera.GateFitMode.Horizontal;
        m_camera.focalLength = m_startScreenFocalLength;
        m_camera.farClipPlane = m_startScreenFarClipPlane;
    }

    public void ShowHub ()
    {
        PlaceCameraAt(m_hubCameraTfm);
    }

    public bool TryGetHubCameraPose ( out Vector3 _position, out Quaternion _rotation )
    {
        _position = m_hubCameraTfm != null ? m_hubCameraTfm.position : Vector3.zero;
        _rotation = m_hubCameraTfm != null ? m_hubCameraTfm.rotation : Quaternion.identity;
        return m_hubCameraTfm != null;
    }

    public float DefaultFieldOfView => m_defaultFieldOfView;

    public bool TryGetGridOverviewPosition ( Vector3 _forward, float _margin, out Vector3 _position )
    {
        _position = default;
        if (GridManager.Instance == null || _forward.sqrMagnitude < 1e-8f || !GridManager.Instance.TryGetTileBounds(out Bounds bounds))
            return false;

        float halfVertical = m_defaultFieldOfView * .5f * Mathf.Deg2Rad;
        float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * m_camera.aspect);
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + Tile.outerRadius;
        float distance = radius / Mathf.Tan(Mathf.Min(halfVertical, halfHorizontal)) * _margin;
        _position = bounds.center - _forward.normalized * distance;
        return true;
    }

    public float GetFieldOfViewToFit ( float _radius, float _distance )
    {
        float halfAngle = Mathf.Asin(Mathf.Clamp(_radius / Mathf.Max(_distance, 1e-4f), 0f, .99f));
        float verticalHalfAngle = m_camera.aspect >= 1f ? halfAngle : Mathf.Atan(Mathf.Tan(halfAngle) / m_camera.aspect);
        return Mathf.Clamp(2f * verticalHalfAngle * Mathf.Rad2Deg, 1f, 170f);
    }

    public void PlaceCameraAt ( Transform _cameraPoint )
    {
        PlaceCameraAt(_cameraPoint.position, _cameraPoint.rotation);
    }

    public void PlaceCameraAt ( Vector3 _cameraPosition, Quaternion _cameraRotation )
    {
        GetParentPoseFor(_cameraPosition, _cameraRotation, out Vector3 parentPosition, out Quaternion parentRotation);
        TeleportCameraTo(parentPosition, parentRotation);
    }

    public void PlaceCameraAt ( Vector3 _cameraPosition, Quaternion _cameraRotation, float _fieldOfView )
    {
        PlaceCameraAt(_cameraPosition, _cameraRotation);
        m_camera.fieldOfView = _fieldOfView;
    }

    public void MoveCameraAlong ( IReadOnlyList<Vector3> _path, Quaternion _cameraRotation, float _fieldOfView, float _duration )
    {
        if (_path == null || _path.Count == 0)
            return;

        if (_duration <= 0f)
        {
            PlaceCameraAt(_path[_path.Count - 1], _cameraRotation, _fieldOfView);
            return;
        }

        float startFieldOfView = m_camera.usePhysicalProperties ? m_defaultFieldOfView : m_camera.fieldOfView;
        KillCameraMove();
        ResetLens();
        m_camera.fieldOfView = startFieldOfView;

        int pointCount = Mathf.Max(_path.Count, 2);
        Vector3[] points = new Vector3[pointCount];
        float[] distances = new float[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            points[i] = _path[Mathf.Min(i, _path.Count - 1)];
            distances[i] = i == 0 ? 0f : distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
        }

        Quaternion startRotation = m_camera.transform.rotation;
        Vector3 startForward = startRotation * Vector3.forward;
        Vector3 endForward = _cameraRotation * Vector3.forward;
        float travelFieldOfView = Mathf.Max(m_defaultFieldOfView, Mathf.Max(startFieldOfView, _fieldOfView));
        m_cameraMoveTween = DOVirtual.Float(0f, 1f, _duration, progress =>
        {
            Quaternion rotation = GetLevelRotation(Vector3.Slerp(startForward, endForward, progress), Quaternion.Slerp(startRotation, _cameraRotation, progress));
            GetParentPoseFor(SamplePolyline(points, distances, progress), rotation, out Vector3 parentPosition, out Quaternion parentRotation);
            m_cameraParent.transform.SetPositionAndRotation(parentPosition, parentRotation);
            m_camera.fieldOfView = progress < .5f
                ? InterpolateFieldOfView(startFieldOfView, travelFieldOfView, Mathf.SmoothStep(0f, 1f, progress * 2f))
                : InterpolateFieldOfView(travelFieldOfView, _fieldOfView, Mathf.SmoothStep(0f, 1f, progress * 2f - 1f));
        }).SetEase(Ease.InOutCubic);
    }

    private static Quaternion GetLevelRotation ( Vector3 _forward, Quaternion _fallback )
    {
        return Vector3.Cross(_forward.normalized, Vector3.up).sqrMagnitude < 1e-6f ? _fallback : Quaternion.LookRotation(_forward, Vector3.up);
    }

    private static float InterpolateFieldOfView ( float _from, float _to, float _progress )
    {
        float fromZoom = Mathf.Log(Mathf.Tan(_from * .5f * Mathf.Deg2Rad));
        float toZoom = Mathf.Log(Mathf.Tan(_to * .5f * Mathf.Deg2Rad));
        return 2f * Mathf.Atan(Mathf.Exp(Mathf.Lerp(fromZoom, toZoom, _progress))) * Mathf.Rad2Deg;
    }

    private static Vector3 SamplePolyline ( Vector3[] _points, float[] _distances, float _progress )
    {
        float target = _progress * _distances[^1];
        for (int i = 1; i < _points.Length; i++)
        {
            if (target > _distances[i] && i < _points.Length - 1)
                continue;

            float segmentLength = _distances[i] - _distances[i - 1];
            return segmentLength <= 0f ? _points[i] : Vector3.Lerp(_points[i - 1], _points[i], (target - _distances[i - 1]) / segmentLength);
        }

        return _points[^1];
    }

    private void GetParentPoseFor ( Vector3 _cameraPosition, Quaternion _cameraRotation, out Vector3 _parentPosition, out Quaternion _parentRotation )
    {
        Transform parent = m_cameraParent.transform;
        Transform camera = m_camera.transform;

        Quaternion parentInverseRotation = Quaternion.Inverse(parent.rotation);
        Quaternion cameraRotationInParent = parentInverseRotation * camera.rotation;
        Vector3 cameraOffsetInParent = parentInverseRotation * (camera.position - parent.position);

        _parentRotation = _cameraRotation * Quaternion.Inverse(cameraRotationInParent);
        _parentPosition = _cameraPosition - _parentRotation * cameraOffsetInParent;
    }

    private void KillCameraMove ()
    {
        if (m_cameraMoveTween.IsActive())
            m_cameraMoveTween.Kill();

        m_cameraMoveTween = null;
    }

    public void TeleportCameraTo(Transform _to )
	{
        TeleportCameraTo(_to.position, _to.rotation);
    }

    public void TeleportCameraTo(Vector3 _position, Quaternion _rotation )
	{
        KillCameraMove();
        ResetLens();
        m_cameraParent.transform.position = _position;
        m_cameraParent.transform.rotation = _rotation;
    }

    private void ResetLens ()
    {
        m_camera.usePhysicalProperties = false;
        m_camera.fieldOfView = m_defaultFieldOfView;
        m_camera.farClipPlane = m_defaultFarClipPlane;
    }

}
