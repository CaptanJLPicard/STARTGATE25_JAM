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

    [Header("Camera Zoom")]
    [SerializeField] private float minCameraDistance = 2f;
    [SerializeField] private float maxCameraDistance = 10f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float zoomSmoothness = 8f;

    [Header("Camera Collision")]
    [SerializeField] private float cameraCollisionRadius = 0.3f;
    [SerializeField] private LayerMask cameraCollisionLayers = ~0; // Default: all layers
    [SerializeField] private float cameraCollisionOffset = 0.1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Animation")]
    [SerializeField] private float fallingThreshold = -2f; // Bu değerin altında düşüyor sayılır
    [SerializeField] private float animationSmoothness = 8f; // Speed geçiş yumuşaklığı
    [SerializeField] private float airAnimationSmoothness = 6f; // Havada animasyon geçiş yumuşaklığı
    [SerializeField] private float maxJumpVelocity = 10f; // Normalize için max yukarı hız
    [SerializeField] private float maxFallVelocity = 15f; // Normalize için max düşme hızı

    [Header("Momentum Effect - Rig References")]
    [SerializeField] private Transform headRig;
    [SerializeField] private Transform leftArmRig;
    [SerializeField] private Transform rightArmRig;

    [Header("Momentum Effect - Settings")]
    [SerializeField] private float momentumIntensity = 15f;       // Hareket anında geriye çekilme miktarı
    [SerializeField] private float springStiffness = 80f;         // Yay sertliği (düşük = daha yavaş dönüş)
    [SerializeField] private float springDamping = 4f;            // Sönümleme (düşük = daha uzun sallanma)
    [SerializeField] private float headMomentumMultiplier = 0.6f; // Kafa için çarpan
    [SerializeField] private float armMomentumMultiplier = 1.2f;  // Kollar için çarpan
    [SerializeField] private float velocitySmoothing = 8f;        // Hız yumuşatma

    [Header("Momentum Effect - Arm Axis")]
    [SerializeField] private bool armAxisX = true;   // Pitch (ileri/geri eğilme)
    [SerializeField] private bool armAxisY = false;  // Yaw (sağa/sola dönme)
    [SerializeField] private bool armAxisZ = true;   // Roll (yana yatma)

    // Network synced
    [Networked] private float Yaw { get; set; }
    [Networked] private float Pitch { get; set; }
    [Networked] private Quaternion ModelRotation { get; set; }
    [Networked] private NetworkBool IsMoving { get; set; }
    [Networked] private NetworkBool IsMovingForward { get; set; }
    [Networked] private NetworkBool IsSprinting { get; set; }
    [Networked] private Vector3 InitialSpawnPosition { get; set; }
    [Networked] public Vector3 NetVelocity { get; set; }
    [Networked] public NetworkBool NetIsTouchingWall { get; set; }
    [Networked] public NetworkBool NetIsJumping { get; set; }
    [Networked] public NetworkBool NetIsFalling { get; set; }
    [Networked] public NetworkString<_16> Nick { get; set; }
    [Networked] public bool IsReady { get; set; }
    [Networked] public Vector3 RespawnPos { get; private set; }

    // Local
    private float _yaw, _pitch;
    private Vector3 _camVelocity;
    private Transform _model;
    private float _targetCameraDistance;
    private float _currentCameraDistance;
    private bool _wasGrounded = true;
    private bool _jumpTriggered = false;
    private float _currentAnimSpeed = 0f;
    private float _currentAirVertical = 0f;
    private bool _landingTriggered = false; // Landing animasyonu için

    // Momentum Effect - Local variables
    private Quaternion _headInitialRotation;
    private Quaternion _leftArmInitialRotation;
    private Quaternion _rightArmInitialRotation;

    // Spring physics - her rig için offset ve velocity
    private Vector3 _headCurrentOffset;
    private Vector3 _headVelocity;
    private Vector3 _leftArmCurrentOffset;
    private Vector3 _leftArmVelocity;
    private Vector3 _rightArmCurrentOffset;
    private Vector3 _rightArmVelocity;

    // Hareket takibi
    private Vector3 _smoothedVelocity;
    private Vector3 _lastPosition;

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

        // Wiggle efekti için başlangıç rotasyonlarını kaydet
        InitializeWiggleSystem();

        if (Object.HasInputAuthority)
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 20f;
            _targetCameraDistance = cameraDistance;
            _currentCameraDistance = cameraDistance;

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

            // Local yaw'ı da güncelle (kamera ile senkron)
            if (Object.HasInputAuthority)
            {
                _yaw = Yaw;
            }

            // Hareket
            Vector3 dir = input.direction;
            bool moving = dir.sqrMagnitude > 0.01f;
            IsMoving = moving;
            IsMovingForward = dir.z > 0.1f; // Sadece W basılıyken
            IsSprinting = input.isSprinting && moving;

            float speed = IsSprinting ? sprintSpeed : moveSpeed;

            // Kamera yönlerini hesapla (kameranın baktığı açıya göre)
            Vector3 forward = Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, Yaw, 0) * Vector3.right;

            if (moving)
            {
                // Hareket yönünü hesapla (kameraya göre)
                // W = ileri (forward), S = geri (-forward)
                // A = sol (-right), D = sağ (right)
                Vector3 moveDir = (forward * dir.z + right * dir.x).normalized;

                _cc.maxSpeed = speed;
                _cc.Move(moveDir * speed);

                // Hareket yönüne döndür
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                ModelRotation = Quaternion.Slerp(ModelRotation, targetRot, Runner.DeltaTime * rotationSpeed);
            }
            else
            {
                _cc.Move(Vector3.zero);
            }

            // Ziplama
            if (input.isJumping && _cc.Grounded)
            {
                _cc.Jump();
                NetIsJumping = true;
                _jumpTriggered = true; // Animasyon için flag
                Debug.Log($"[JUMP] Ziplama yapildi! _jumpTriggered = {_jumpTriggered}");
            }

            // Zıplama ve düşme durumlarını güncelle
            float verticalVelocity = _cc.Velocity.y;

            // Yere değdiğinde zıplama durumunu sıfırla
            if (_cc.Grounded)
            {
                NetIsJumping = false;
                NetIsFalling = false;
            }
            else
            {
                // Havadayken: düşüyor mu kontrol et
                NetIsFalling = verticalVelocity < fallingThreshold;
            }
        }
    }

    public override void Render()
    {
        // Model rotasyonunu uygula (tum oyuncular icin)
        if (_model != null && _model != transform)
        {
            _model.rotation = ModelRotation;
        }
        else if (playerModel != null)
        {
            playerModel.rotation = ModelRotation;
        }

        // Animasyon
        bool isGrounded = _cc != null && _cc.Grounded;

        if (animator != null)
        {
            // Yürüme/Koşma animasyonu
            float targetSpeed = IsMoving ? (IsSprinting ? 1f : 0.5f) : 0f;
            _currentAnimSpeed = Mathf.MoveTowards(_currentAnimSpeed, targetSpeed, Time.deltaTime * animationSmoothness);
            animator.SetFloat("Speed", _currentAnimSpeed);

            if (HasParameter(animator, "IsGrounded"))
                animator.SetBool("IsGrounded", isGrounded);

            // Hareket durumu - Landing'den çıkış için
            if (HasParameter(animator, "IsMoving"))
                animator.SetBool("IsMoving", IsMoving);

            // Havada animasyon - Blend Tree için AirVertical değeri
            // 0 = JumpUp (yukarı), 1 = Falling (düşme)
            float verticalVelocity = _cc != null ? _cc.Velocity.y : 0f;
            float targetAirVertical = 0f;

            if (!isGrounded)
            {
                if (verticalVelocity > 0)
                {
                    // Yukarı gidiyor - JumpUp (0)
                    targetAirVertical = 0f;
                }
                else
                {
                    // Aşağı düşüyor - Falling (1'e doğru)
                    targetAirVertical = Mathf.Clamp01(Mathf.Abs(verticalVelocity) / maxFallVelocity);
                }
            }

            _currentAirVertical = Mathf.MoveTowards(_currentAirVertical, targetAirVertical, Time.deltaTime * airAnimationSmoothness);

            if (HasParameter(animator, "AirVertical"))
                animator.SetFloat("AirVertical", _currentAirVertical);

            // Landing - havadayken yere değdiği an
            if (!_wasGrounded && isGrounded)
            {
                _landingTriggered = true;
            }

            if (_landingTriggered && HasParameter(animator, "Land"))
            {
                animator.SetTrigger("Land");
                _landingTriggered = false;
            }
        }

        // Flag'leri her zaman sıfırla
        _wasGrounded = isGrounded;

        // Kamera (sadece local player)
        if (Object.HasInputAuthority)
        {
            UpdateCamera();
        }
    }

    private void LateUpdate()
    {
        // Wiggle efekti Animator'dan SONRA uygulanmalı
        // LateUpdate, Animator güncellemesinden sonra çalışır
        if (Object != null && Object.IsValid)
        {
            UpdateWiggleEffect();
        }
    }

    private void Update()
    {
        if (!Object || !Object.HasInputAuthority) return;

        _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, minPitch, maxPitch);

        // Mouse scroll ile zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetCameraDistance -= scroll * zoomSpeed;
            _targetCameraDistance = Mathf.Clamp(_targetCameraDistance, minCameraDistance, maxCameraDistance);
        }
    }

    private void UpdateCamera()
    {
        if (cam == null) return;

        // Smooth zoom
        _currentCameraDistance = Mathf.Lerp(_currentCameraDistance, _targetCameraDistance, Time.deltaTime * zoomSmoothness);

        Vector3 targetPos = transform.position + Vector3.up * cameraHeight;

        // Sadece ileri (W) giderken kamera karakterin arkasından baksın
        if (IsMovingForward)
        {
            // Karakterin baktığı yönü al
            float modelYaw = ModelRotation.eulerAngles.y;
            // Yaw'ı karakterin arkasına yumuşak geçiş yap
            _yaw = Mathf.LerpAngle(_yaw, modelYaw, Time.deltaTime * cameraSmoothness);
        }

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 direction = rotation * Vector3.forward;

        // Kamera collision kontrolü
        float finalDistance = _currentCameraDistance;

        // SphereCast ile engel kontrolü
        if (Physics.SphereCast(targetPos, cameraCollisionRadius, -direction, out RaycastHit hit,
            _currentCameraDistance, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Engel varsa kamerayı engelin önünde tut
            finalDistance = Mathf.Max(hit.distance - cameraCollisionOffset, minCameraDistance * 0.5f);
        }

        Vector3 desiredPos = targetPos - direction * finalDistance;

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

    #region MOMENTUM EFFECT SYSTEM

    private void InitializeWiggleSystem()
    {
        _lastPosition = transform.position;
        _smoothedVelocity = Vector3.zero;

        // Spring değerlerini sıfırla
        _headCurrentOffset = Vector3.zero;
        _headVelocity = Vector3.zero;
        _leftArmCurrentOffset = Vector3.zero;
        _leftArmVelocity = Vector3.zero;
        _rightArmCurrentOffset = Vector3.zero;
        _rightArmVelocity = Vector3.zero;
    }

    private void UpdateWiggleEffect()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0.0001f) return;

        // Karakterin dünya uzayındaki hızını hesapla
        Vector3 currentPosition = transform.position;
        Vector3 worldVelocity = (currentPosition - _lastPosition) / deltaTime;
        _lastPosition = currentPosition;

        // Hızı yumuşat (ani değişimleri önle)
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, worldVelocity, deltaTime * velocitySmoothing);

        // Hızı karakterin lokal uzayına çevir (model rotasyonuna göre)
        Vector3 localVelocity = Vector3.zero;
        if (_model != null)
        {
            localVelocity = Quaternion.Inverse(_model.rotation) * _smoothedVelocity;
        }
        else
        {
            localVelocity = Quaternion.Inverse(transform.rotation) * _smoothedVelocity;
        }

        // Momentum efektini uygula
        UpdateMomentumEffect(deltaTime, localVelocity);
    }

    private void UpdateMomentumEffect(float deltaTime, Vector3 localVelocity)
    {
        // Hareket yönünün tersine offset hesapla (geriden takip efekti)
        // İleri giderken -> geriye eğilim
        // Sağa giderken -> sola eğilim
        // Zıplarken -> aşağı eğilim (yukarı momentum)
        // Düşerken -> yukarı eğilim (aşağı momentum)

        // Target offset: hız vektörünün tersi yönünde rotasyon
        // X ekseni (pitch): ileri/geri + yukarı/aşağı hareket -> öne/arkaya eğilme
        // Y ekseni (yaw): sağ/sol hareket -> yanlara eğilme

        // Kafa için hedef offset
        Vector3 headTargetOffset = Vector3.zero;
        if (localVelocity.sqrMagnitude > 0.1f)
        {
            // Dikey hareket için pitch etkisi (zıplama/düşme)
            float verticalPitch = -localVelocity.y * momentumIntensity * headMomentumMultiplier * 0.08f;

            headTargetOffset = new Vector3(
                -localVelocity.z * momentumIntensity * headMomentumMultiplier * 0.1f + verticalPitch,  // İleri + dikey -> pitch
                localVelocity.x * momentumIntensity * headMomentumMultiplier * 0.05f,  // Sağa git -> sola bak (yaw)
                -localVelocity.x * momentumIntensity * headMomentumMultiplier * 0.03f  // Hafif roll
            );
        }

        // Kollar için hedef offset (daha abartılı)
        Vector3 armTargetOffset = Vector3.zero;
        if (localVelocity.sqrMagnitude > 0.1f)
        {
            // Dikey hareket için kollar (zıplama = kollar aşağı, düşme = kollar yukarı)
            float verticalArm = -localVelocity.y * momentumIntensity * armMomentumMultiplier * 0.12f;

            float armX = armAxisX ? (-localVelocity.z * momentumIntensity * armMomentumMultiplier * 0.15f + verticalArm) : 0f;
            float armY = armAxisY ? (-localVelocity.x * momentumIntensity * armMomentumMultiplier * 0.08f) : 0f;
            float armZ = armAxisZ ? (-localVelocity.x * momentumIntensity * armMomentumMultiplier * 0.05f) : 0f;

            armTargetOffset = new Vector3(armX, armY, armZ);
        }

        // Spring physics ile smooth geçiş ve sallanma
        UpdateSpringPhysics(ref _headCurrentOffset, ref _headVelocity, headTargetOffset, deltaTime);
        UpdateSpringPhysics(ref _leftArmCurrentOffset, ref _leftArmVelocity, armTargetOffset, deltaTime);
        UpdateSpringPhysics(ref _rightArmCurrentOffset, ref _rightArmVelocity, armTargetOffset, deltaTime);

        // Rotasyonları uygula
        ApplyMomentumRotation(headRig, _headCurrentOffset);
        ApplyMomentumRotation(leftArmRig, _leftArmCurrentOffset);
        ApplyMomentumRotation(rightArmRig, _rightArmCurrentOffset);
    }

    private void UpdateSpringPhysics(ref Vector3 currentOffset, ref Vector3 velocity, Vector3 targetOffset, float deltaTime)
    {
        // Spring physics: F = -k(x - target) - b*v
        // Hedef pozisyona doğru çekiliş + sönümleme
        // Bu, durma anında overshoot yapıp ileri geri sallanma sağlar

        Vector3 displacement = currentOffset - targetOffset;
        Vector3 springForce = -springStiffness * displacement;
        Vector3 dampingForce = -springDamping * velocity;
        Vector3 totalForce = springForce + dampingForce;

        velocity += totalForce * deltaTime;
        currentOffset += velocity * deltaTime;
    }

    private void ApplyMomentumRotation(Transform rig, Vector3 offset)
    {
        if (rig == null) return;

        // Mevcut animasyon rotasyonunun üzerine offset ekle
        Quaternion currentRotation = rig.localRotation;
        Quaternion offsetRotation = Quaternion.Euler(offset);
        rig.localRotation = currentRotation * offsetRotation;
    }

    /// <summary>
    /// Momentum sistemini runtime'da sıfırlar
    /// </summary>
    public void ResetWiggleSystem()
    {
        _smoothedVelocity = Vector3.zero;
        _lastPosition = transform.position;

        _headCurrentOffset = Vector3.zero;
        _headVelocity = Vector3.zero;
        _leftArmCurrentOffset = Vector3.zero;
        _leftArmVelocity = Vector3.zero;
        _rightArmCurrentOffset = Vector3.zero;
        _rightArmVelocity = Vector3.zero;
    }

    #endregion

    #region RPC METHODS

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetRespawnPos(Vector3 pos)
    {
        RespawnPos = pos;
    }

    // Host çağırır: oyuncuyu son respawn noktasına ışınlar
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TeleportToRespawn()
    {
        transform.position = RespawnPos;
    }

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
            Debug.Log($"<color=green>�lk spawn noktas�na d�n�yorsun!</color>");
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
                    Debug.Log($"<color=green>Spawn noktas�na ���nland�n�z!</color>");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Teleport hatas�: {e.Message}");
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
