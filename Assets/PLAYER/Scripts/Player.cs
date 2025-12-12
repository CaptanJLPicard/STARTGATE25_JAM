using Fusion;
using System;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkCharacterController _cc;
    [SerializeField] private Transform playerModel;
    [SerializeField] private Animator animator;

    [Header("Camera")]
    [SerializeField] private Camera cam;
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float cameraHeight = 2f;
    [SerializeField] private float cameraSmoothness = 10f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;

    // Network synced
    [Networked] private float Yaw { get; set; }
    [Networked] private float Pitch { get; set; }
    [Networked] private Quaternion ModelRotation { get; set; }
    [Networked] private NetworkBool IsMoving { get; set; }
    [Networked] private NetworkBool IsSprinting { get; set; }
    [Networked] private Vector3 InitialSpawnPosition { get; set; }
    [Networked] public Vector3 NetVelocity { get; set; }
    [Networked] public NetworkBool NetIsTouchingWall { get; set; }
    [Networked] public NetworkBool NetIsJumping { get; set; }
    [Networked] public NetworkString<_16> Nick { get; set; }
    [Networked] public bool IsReady { get; set; }

    // Local
    private float _yaw, _pitch;
    private Vector3 _camVelocity;
    private Transform _model;

    public override void Spawned()
    {
        if (_cc == null)
            _cc = GetComponent<NetworkCharacterController>();

        if (_cc != null)
        {
            _cc.jumpImpulse = jumpForce;
            _cc.gravity = -20f;
        }

        _model = playerModel != null ? playerModel : transform;
        ModelRotation = _model.rotation;

        if (Object.HasInputAuthority)
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 20f;

            if (cam == null)
                cam = Camera.main;

            if (cam != null)
            {
                cam.enabled = true;
                cam.gameObject.SetActive(true);
            }

            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
        else
        {
            if (cam != null)
            {
                cam.enabled = false;
                cam.gameObject.SetActive(false);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_cc == null) return;

        if (GetInput(out NetworkInputData input))
        {
            // Kamera rotasyonu
            Yaw += input.mouseDelta.x * mouseSensitivity;
            Pitch = Mathf.Clamp(Pitch - input.mouseDelta.y * mouseSensitivity, minPitch, maxPitch);

            // Hareket
            Vector3 dir = input.direction;
            bool moving = dir.sqrMagnitude > 0.01f;
            IsMoving = moving;
            IsSprinting = input.isSprinting && moving;

            float speed = IsSprinting ? sprintSpeed : moveSpeed;

            if (moving)
            {
                Vector3 forward = Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, Yaw, 0) * Vector3.right;
                Vector3 moveDir = (forward * dir.z + right * dir.x).normalized;

                _cc.maxSpeed = speed;
                _cc.Move(moveDir * speed);

                // Rotasyonu network'e kaydet
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    ModelRotation = Quaternion.Slerp(ModelRotation, targetRot, Runner.DeltaTime * rotationSpeed);
                }
            }
            else
            {
                _cc.Move(Vector3.zero);
            }

            // Ziplama
            if (input.isJumping && _cc.Grounded)
            {
                _cc.Jump();
            }
        }
    }

    public override void Render()
    {
        // Model rotasyonunu uygula (tum oyuncular icin)
        if (_model != null)
        {
            _model.rotation = ModelRotation;
        }

        // Animasyon
        if (animator != null)
        {
            float speed = IsMoving ? (IsSprinting ? 1f : 0.5f) : 0f;
            animator.SetFloat("Speed", speed);

            if (HasParameter(animator, "IsGrounded"))
                animator.SetBool("IsGrounded", _cc != null && _cc.Grounded);
        }

        // Kamera (sadece local player)
        if (Object.HasInputAuthority)
        {
            UpdateCamera();
        }
    }

    private void Update()
    {
        if (!Object || !Object.HasInputAuthority) return;

        _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, minPitch, maxPitch);
    }

    private void UpdateCamera()
    {
        if (cam == null) return;

        Vector3 targetPos = transform.position + Vector3.up * cameraHeight;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 desiredPos = targetPos - rotation * Vector3.forward * cameraDistance;

        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredPos, ref _camVelocity, 1f / cameraSmoothness);
        cam.transform.LookAt(targetPos);
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;
        foreach (var param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    #region RPC METHODS

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool value) => IsReady = value;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNick(NetworkString<_16> nick)
    {
        string s = nick.ToString().Trim();
        if (string.IsNullOrEmpty(s))
        {
            Nick = $"P{Object.InputAuthority.PlayerId}";
        }
        else
        {
            if (s.Length > 16) s = s.Substring(0, 16);
            Nick = s;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TeleportToInitialSpawn()
    {
        RPC_TeleportToPosition(InitialSpawnPosition);

        if (Object.HasInputAuthority)
        {
            Debug.Log($"<color=green>Ýlk spawn noktasýna dönüyorsun!</color>");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TeleportToPosition(Vector3 targetPosition)
    {
        NetVelocity = Vector3.zero;

        if (_cc != null)
        {
            try
            {
                _cc.Velocity = Vector3.zero;
                _cc.Teleport(targetPosition);

                if (Object.HasInputAuthority)
                {
                    Debug.Log($"<color=green>Spawn noktasýna ýþýnlandýnýz!</color>");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Teleport hatasý: {e.Message}");
                transform.position = targetPosition;
            }
        }
        else
        {
            transform.position = targetPosition;
        }

        NetIsTouchingWall = false;
        NetIsJumping = false;
    }
    #endregion
}
