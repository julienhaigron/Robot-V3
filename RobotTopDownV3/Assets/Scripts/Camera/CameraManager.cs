using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    public static Action onFollowUnitsChanged;

    [SerializeField] private GameObject m_cameraParent;
    public GameObject CameraParent => m_cameraParent;
    [SerializeField] private Camera m_camera;
    public Camera Camera => m_camera;

    [SerializeField] private Transform m_defaultCameraPosition;
    public float DefaultZoomDistance => m_defaultCameraPosition == null ? m_cameraParent.transform.position.y : m_defaultCameraPosition.position.y;

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
        FogOfWarRenderer.Instance.MarkDirty();
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

    public void TeleportCameraTo(Transform _to )
	{
        TeleportCameraTo(_to.position, _to.rotation);
    }

    public void TeleportCameraTo(Vector3 _position, Quaternion _rotation )
	{
        m_cameraParent.transform.position = _position;
        m_cameraParent.transform.rotation = _rotation;
    }

}
