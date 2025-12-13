using Fusion;
using System;
using UnityEngine;
using TMPro;

public class Player : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkCharacterController _cc;
    [SerializeField] private Transform playerModel;
    [SerializeField] private Animator animator;

    [Header("Nickname Display")]
    [SerializeField] private TextMeshPro nicknameText; // Player prefabına child olarak TextMeshPro 3D Text ekle ve buraya ata
    [SerializeField] private Vector3 nicknameOffset = new Vector3(0f, 2.5f, 0f);

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

    [Header("Scale System")]
    [SerializeField] private float shrinkDuration = 60f; // Tam küçülme süresi (saniye)
    [SerializeField] private float minScale = 0.3f; // Minimum scale değeri
    [SerializeField] private float maxScale = 2f; // Maximum scale değeri
    [SerializeField] private float startScale = 1f; // Başlangıç scale değeri
    [SerializeField] private float powerUpGrowthAmount = 0.2f; // PowerUp topladığında büyüme miktarı
    [SerializeField] private float scaleSmoothness = 5f; // Scale geçiş yumuşaklığı
    [SerializeField] private float sprintShrinkMultiplier = 2f; // Koşarken küçülme çarpanı

    [Header("Scale Effects on Stats")]
    [SerializeField] private float speedChangePerScale = 2f; // Scale değişimine göre hız değişimi
    [SerializeField] private float jumpChangePerScale = 3f; // Scale değişimine göre zıplama değişimi

    [Header("Dropped Pickup System")]
    [SerializeField] private NetworkObject droppedPickupPrefab; // Spawn edilecek prefab (NetworkObject olarak)
    [SerializeField] private float dropInterval = 0.5f; // Ne kadar sürede bir drop olacak
    [SerializeField] private float droppedPickupLifetime = 10f; // Pickup'ın yaşam süresi
    [SerializeField] private float droppedPickupActivationDelay = 0.3f; // Spawn sonrası aktif olma süresi
    [SerializeField] private float droppedPickupGrowthAmount = 0.05f; // Toplandığında büyüme miktarı
    [SerializeField] private float groundCheckDistance = 2f; // Zemin kontrolü mesafesi
    [SerializeField] private LayerMask groundLayer = ~0; // Zemin layer'ı

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
    [Networked] public float CurrentScale { get; set; }

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

    // Scale System - Local
    private float _displayScale; // Görsel smooth scale
    private float _scaleVelocity; // SmoothDamp için velocity
    private float _baseMoveSpeed; // Başlangıç yürüme hızı
    private float _baseSprintSpeed; // Başlangıç koşu hızı
    private float _baseJumpForce; // Başlangıç zıplama gücü
    private TickTimer _dropTimer; // Pickup drop zamanlayıcısı
    private float _pendingScaleChange; // Birikmiş scale değişikliği (threshold için)
    private const float SCALE_UPDATE_THRESHOLD = 0.02f; // Network update threshold
    private const float SCALE_SMOOTH_TIME = 0.15f; // Scale geçiş süresi

    // Nickname - Local
    private Camera _mainCamera; // Billboard efekti için ana kamera
    private string _lastNick; // Nick değişikliği kontrolü için

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

    // Interpolation için
    private Quaternion _interpolatedModelRotation;
    private Vector3 _interpolatedPosition; // Pozisyon smoothing
    private Vector3 _positionVelocity; // SmoothDamp için
    private float _smoothMovingForward; // IsMovingForward için smooth değer
    private bool _positionInitialized;
    private const float ROTATION_INTERPOLATION_SPEED = 20f;
    private const float POSITION_SMOOTH_TIME = 0.05f; // 50ms smoothing
    private const float MOVEMENT_STATE_SMOOTHING = 12f;

    // Kamera smoothing - ayrı sistem (jitter önleme)
    private Vector3 _cameraTargetPos; // Kamera hedef pozisyonu
    private Vector3 _cameraTargetVelocity; // Kamera hedef smooth velocity
    private Vector3 _cameraPosVelocity; // Kamera pozisyon smooth velocity
    private float _cameraDistanceVelocity; // Kamera mesafe smooth velocity
    private bool _cameraInitialized;
    private const float CAMERA_TARGET_SMOOTH_TIME = 0.08f; // Hedef smooth süresi
    private const float CAMERA_POS_SMOOTH_TIME = 0.06f; // Kamera pozisyon smooth süresi

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
        _interpolatedModelRotation = _model.rotation;
        _interpolatedPosition = transform.position;
        _positionInitialized = true;

        // Scale System başlangıç değerleri
        _baseMoveSpeed = moveSpeed;
        _baseSprintSpeed = sprintSpeed;
        _baseJumpForce = jumpForce;

        // Sadece StateAuthority scale'i başlatır
        if (Object.HasStateAuthority)
        {
            CurrentScale = startScale;
        }
        _displayScale = CurrentScale > 0 ? CurrentScale : startScale;
        transform.localScale = Vector3.one * _displayScale;

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

            // Kamera smoothing başlangıç değerleri
            _cameraTargetPos = transform.position + Vector3.up * cameraHeight;
            _cameraInitialized = true;

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

        // Nickname sistemi başlat
        _mainCamera = Camera.main;

        // Local player ise PlayerPrefs'ten nick'i oku ve set et
        if (Object.HasInputAuthority)
        {
            string savedNick = PlayerPrefs.GetString("Nick", "");
            if (!string.IsNullOrEmpty(savedNick))
            {
                RPC_SetNick(savedNick);
            }
        }

        UpdateNicknameText();
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

            // Scale System - Zamana bağlı küçülme (GetInput içinde - input olan oyuncu için)
            if (shrinkDuration > 0 && moving)
            {
                float shrinkRate = (startScale - minScale) / shrinkDuration;

                // Koşarken daha hızlı küçül
                if (input.isSprinting)
                {
                    shrinkRate *= sprintShrinkMultiplier;
                }

                // StateAuthority scale'i günceller
                if (Object.HasStateAuthority)
                {
                    float newScale = CurrentScale - (shrinkRate * Runner.DeltaTime);
                    CurrentScale = Mathf.Clamp(newScale, minScale, maxScale);
                    UpdateStatsBasedOnScale();
                }

                // Pickup spawn - Input alan oyuncu için (kendi karakteri)
                if (droppedPickupPrefab != null)
                {
                    if (_dropTimer.ExpiredOrNotRunning(Runner))
                    {
                        // Host ise direkt spawn et, client ise RPC ile iste
                        if (Object.HasStateAuthority)
                        {
                            SpawnDroppedPickup();
                        }
                        else
                        {
                            // Client: RPC ile host'a spawn isteği gönder
                            RPC_RequestSpawnPickup(transform.position, CurrentScale);
                        }
                        _dropTimer = TickTimer.CreateFromSeconds(Runner, dropInterval);
                    }
                }
            }
        }
    }

    public override void Render()
    {
        // Scale System - Smooth scale geçişi (tüm oyuncular için)
        if (CurrentScale > 0)
        {
            // SmoothDamp kullan - Lerp'ten çok daha smooth, jitter önler
            _displayScale = Mathf.SmoothDamp(_displayScale, CurrentScale, ref _scaleVelocity, SCALE_SMOOTH_TIME);
            transform.localScale = Vector3.one * _displayScale;
        }

        // Model rotasyonunu INTERPOLATE ederek uygula (jitter önleme)
        // Scale'e göre daha yavaş rotasyon geçişi
        float safeStartScale = startScale > 0.01f ? startScale : 1f;
        float scaleRatio = _displayScale / safeStartScale;
        float dynamicRotSpeed = ROTATION_INTERPOLATION_SPEED / Mathf.Clamp(scaleRatio, 0.5f, 2f);

        _interpolatedModelRotation = Quaternion.Slerp(
            _interpolatedModelRotation,
            ModelRotation,
            Time.deltaTime * dynamicRotSpeed
        );

        if (_model != null && _model != transform)
        {
            _model.rotation = _interpolatedModelRotation;
        }
        else if (playerModel != null)
        {
            playerModel.rotation = _interpolatedModelRotation;
        }

        // Animasyon
        bool isGrounded = _cc != null && _cc.Grounded;

        if (animator != null)
        {
            // Yürüme/Koşma animasyonu
            float targetSpeed = IsMoving ? (IsSprinting ? 1f : 0.5f) : 0f;
            _currentAnimSpeed = Mathf.MoveTowards(_currentAnimSpeed, targetSpeed, Time.deltaTime * animationSmoothness);
            animator.SetFloat("Speed", _currentAnimSpeed);

            // Animasyon hızını karakter hızına göre ayarla
            float speedRatio = 1f;
            if (_baseMoveSpeed > 0)
            {
                float currentSpeed = IsSprinting ? sprintSpeed : moveSpeed;
                float baseSpeed = IsSprinting ? _baseSprintSpeed : _baseMoveSpeed;
                speedRatio = currentSpeed / baseSpeed;
            }
            animator.speed = IsMoving ? speedRatio : 1f;

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

        // Movement state smoothing (jitter önleme)
        float targetMovingForward = IsMovingForward ? 1f : 0f;
        _smoothMovingForward = Mathf.Lerp(_smoothMovingForward, targetMovingForward, Time.deltaTime * MOVEMENT_STATE_SMOOTHING);

        // Nickname güncelle ve billboard efekti
        UpdateNicknameDisplay();
    }

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid) return;

        // Pozisyon smoothing - TÜM playerlar için (jitter önleme)
        // Scale'e göre dinamik smooth time
        if (_positionInitialized)
        {
            float safeStartScale = startScale > 0.01f ? startScale : 1f;
            float scaleRatio = _displayScale / safeStartScale;
            float dynamicSmoothTime = POSITION_SMOOTH_TIME * Mathf.Clamp(scaleRatio, 0.5f, 2f);

            _interpolatedPosition = Vector3.SmoothDamp(
                _interpolatedPosition,
                transform.position,
                ref _positionVelocity,
                dynamicSmoothTime
            );
        }

        // Wiggle efekti Animator'dan SONRA uygulanmalı
        UpdateWiggleEffect();

        // Kamera güncellemesi LateUpdate'te (tüm transform güncellemelerinden sonra)
        if (Object.HasInputAuthority)
        {
            UpdateCameraLate();
        }

        // Nickname billboard efekti - her frame kameraya baksın
        UpdateNicknameBillboard();
    }

    private void Update()
    {
        // Billboard efekti - TÜM playerlar için her frame çalışır
        BillboardNickname();

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

    /// <summary>
    /// LateUpdate'te çağrılan kamera güncellemesi - çift katmanlı smoothing ile jitter önleme
    /// </summary>
    private void UpdateCameraLate()
    {
        if (cam == null) return;

        // Scale faktörü hesapla
        float safeStartScale = startScale > 0.01f ? startScale : 1f;
        float scaleRatio = _displayScale / safeStartScale;
        float scaleFactor = Mathf.Clamp(scaleRatio, 0.5f, 3f);

        // Dinamik değerler
        float dynamicTargetDistance = _targetCameraDistance * scaleFactor;
        float dynamicMinDistance = minCameraDistance * scaleFactor;
        float dynamicMaxDistance = maxCameraDistance * scaleFactor;
        dynamicTargetDistance = Mathf.Clamp(dynamicTargetDistance, dynamicMinDistance, dynamicMaxDistance);
        float dynamicCameraHeight = cameraHeight * scaleFactor;

        // ===== KATMAN 1: Hedef pozisyonu smooth et (network jitter absorbe) =====
        Vector3 rawTargetPos = transform.position + Vector3.up * dynamicCameraHeight;

        // İlk çağrıda başlat
        if (!_cameraInitialized)
        {
            _cameraTargetPos = rawTargetPos;
            _cameraInitialized = true;
        }

        // Hedef pozisyonu çift katmanlı smooth - önce interpolated, sonra camera target
        // Bu network jitter'ı tamamen absorbe eder
        float targetSmoothTime = CAMERA_TARGET_SMOOTH_TIME * scaleFactor;
        _cameraTargetPos = Vector3.SmoothDamp(_cameraTargetPos, rawTargetPos, ref _cameraTargetVelocity, targetSmoothTime);

        // ===== ZOOM SMOOTH =====
        float zoomSmoothTime = 0.12f * scaleFactor;
        _currentCameraDistance = Mathf.SmoothDamp(_currentCameraDistance, dynamicTargetDistance, ref _cameraDistanceVelocity, zoomSmoothTime);

        // ===== YAW SMOOTH (ileri giderken) =====
        if (_smoothMovingForward > 0.5f)
        {
            float modelYaw = _interpolatedModelRotation.eulerAngles.y;
            float lerpFactor = Time.deltaTime * cameraSmoothness * _smoothMovingForward * 0.5f; // Daha yavaş
            _yaw = Mathf.LerpAngle(_yaw, modelYaw, lerpFactor);
        }

        // Kamera yönü
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 direction = rotation * Vector3.forward;

        // ===== COLLISION =====
        float finalDistance = _currentCameraDistance;
        float dynamicCollisionRadius = cameraCollisionRadius * scaleFactor;

        if (Physics.SphereCast(_cameraTargetPos, dynamicCollisionRadius, -direction, out RaycastHit hit,
            _currentCameraDistance, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            float dynamicOffset = cameraCollisionOffset * scaleFactor;
            float collisionDistance = Mathf.Max(hit.distance - dynamicOffset, dynamicMinDistance * 0.5f);
            // Collision mesafesini de smooth et (ani geçişleri önle)
            finalDistance = Mathf.Lerp(finalDistance, collisionDistance, Time.deltaTime * 15f);
        }

        // Hedef kamera pozisyonu
        Vector3 desiredCamPos = _cameraTargetPos - direction * finalDistance;

        // ===== KATMAN 2: Kamera pozisyonunu smooth et =====
        float camSmoothTime = CAMERA_POS_SMOOTH_TIME * scaleFactor;
        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredCamPos, ref _cameraPosVelocity, camSmoothTime);

        // ===== LOOKAT da smooth olmalı =====
        // LookAt yerine smooth rotation kullan
        Vector3 lookDirection = _cameraTargetPos - cam.transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRotation, Time.deltaTime * 20f);
        }
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

    #region NICKNAME SYSTEM

    /// <summary>
    /// Nickname text'ini günceller
    /// </summary>
    private void UpdateNicknameText()
    {
        if (nicknameText == null) return;

        string currentNick = Nick.ToString();
        if (string.IsNullOrEmpty(currentNick))
        {
            currentNick = $"Player {Object.InputAuthority.PlayerId}";
        }

        if (_lastNick != currentNick)
        {
            nicknameText.text = currentNick;
            _lastNick = currentNick;
        }
    }

    /// <summary>
    /// Nickname pozisyonunu günceller
    /// </summary>
    private void UpdateNicknameDisplay()
    {
        // Text yoksa çık
        if (nicknameText == null) return;

        // Nick değişmişse güncelle
        UpdateNicknameText();

        // Doğrudan TextMeshPro transform'unu kullan
        Transform textTransform = nicknameText.transform;

        // Nickname pozisyonunu güncelle (scale'e göre offset ayarla)
        // INTERPOLATED pozisyon kullan (jitter önleme)
        float scaleMultiplier = CurrentScale > 0 ? CurrentScale : 1f;
        Vector3 scaledOffset = nicknameOffset * scaleMultiplier;
        Vector3 basePos = _positionInitialized ? _interpolatedPosition : transform.position;
        textTransform.position = basePos + scaledOffset;
    }

    /// <summary>
    /// Billboard efekti - LateUpdate'te çağrılır
    /// </summary>
    private void UpdateNicknameBillboard()
    {
        BillboardNickname();
    }

    /// <summary>
    /// Nickname'i kameraya döndürür - Update'te çağrılır
    /// </summary>
    private void BillboardNickname()
    {
        if (nicknameText == null) return;

        // Aktif kamerayı bul
        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        // Text'in kameraya dönük olması için - kameradan uzağa baksın
        Transform textTransform = nicknameText.transform;
        Vector3 directionFromCamera = textTransform.position - activeCam.transform.position;
        textTransform.rotation = Quaternion.LookRotation(directionFromCamera);
    }

    /// <summary>
    /// Aktif kamerayı bulur (Camera.main yoksa diğer aktif kameraları arar)
    /// </summary>
    private Camera GetActiveCamera()
    {
        // Önce cached camera'yı kontrol et
        if (_mainCamera != null && _mainCamera.isActiveAndEnabled)
            return _mainCamera;

        // Camera.main dene
        Camera cam = Camera.main;
        if (cam != null)
        {
            _mainCamera = cam;
            return cam;
        }

        // MainCamera tag'i yoksa aktif kameraları ara
        foreach (Camera c in Camera.allCameras)
        {
            if (c.isActiveAndEnabled)
            {
                _mainCamera = c;
                return c;
            }
        }

        return null;
    }

    #endregion

    #region SCALE SYSTEM

    /// <summary>
    /// Scale değerine göre hız ve zıplama gücünü günceller
    /// </summary>
    private void UpdateStatsBasedOnScale()
    {
        if (_cc == null) return;

        // Scale farkını hesapla (startScale'e göre)
        float scaleDiff = CurrentScale - startScale;

        // Hız ve zıplama değerlerini güncelle
        moveSpeed = _baseMoveSpeed + (scaleDiff * speedChangePerScale);
        sprintSpeed = _baseSprintSpeed + (scaleDiff * speedChangePerScale);
        jumpForce = _baseJumpForce + (scaleDiff * jumpChangePerScale);

        // Minimum değerlerin altına düşmesin
        moveSpeed = Mathf.Max(moveSpeed, 1f);
        sprintSpeed = Mathf.Max(sprintSpeed, 2f);
        jumpForce = Mathf.Max(jumpForce, 3f);

        // NetworkCharacterController'a uygula
        _cc.jumpImpulse = jumpForce;
    }

    /// <summary>
    /// Karakteri büyütür (PowerUp topladığında çağrılır)
    /// </summary>
    public void Grow(float amount)
    {
        if (Object.HasStateAuthority)
        {
            CurrentScale = Mathf.Clamp(CurrentScale + amount, minScale, maxScale);
            UpdateStatsBasedOnScale();
        }
        else
        {
            // Client ise RPC ile StateAuthority'ye bildir
            RPC_RequestGrow(amount);
        }
    }

    /// <summary>
    /// PowerUp ve DroppedPickup ile trigger collision
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Sadece InputAuthority (local player) ve hareket ederken toplayabilir
        if (!Object.HasInputAuthority) return;
        if (!IsMoving) return;

        // DroppedPickup kontrolü
        if (other.TryGetComponent<DroppedPickup>(out var droppedPickup))
        {
            // Aktif değilse toplama
            if (!droppedPickup.CanBeCollected()) return;

            // NetworkObject'i al
            if (!other.TryGetComponent<NetworkObject>(out var netObj)) return;

            // Büyüme uygula (hız ve zıplama otomatik scale ile artar)
            float growth = droppedPickup.GrowthAmount;
            Grow(growth);

            // Network üzerinden despawn - Host ise direkt, client ise RPC
            if (Runner != null)
            {
                if (Object.HasStateAuthority)
                {
                    Runner.Despawn(netObj);
                }
                else
                {
                    // Client: RPC ile host'a despawn isteği gönder
                    RPC_RequestDespawnPickup(netObj.Id);
                }
            }
            return;
        }

        // Normal PowerUp kontrolü
        if (other.CompareTag("PowerUp"))
        {
            // PowerUp'ı yok et ve büyü
            Grow(powerUpGrowthAmount);

            // PowerUp objesini yok et (network üzerinden)
            if (Runner != null && other.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (Object.HasStateAuthority)
                {
                    Runner.Despawn(netObj);
                }
                else
                {
                    RPC_RequestDespawnPickup(netObj.Id);
                }
            }
            else
            {
                // Network objesi değilse normal destroy
                Destroy(other.gameObject);
            }
        }
    }

    /// <summary>
    /// Scale'i sıfırlar (başlangıç değerine döndürür)
    /// </summary>
    public void ResetScale()
    {
        if (Object.HasStateAuthority)
        {
            CurrentScale = startScale;
            UpdateStatsBasedOnScale();
        }
    }

    /// <summary>
    /// Zemine göre pickup spawn eder
    /// </summary>
    private void SpawnDroppedPickup()
    {
        if (!Object.HasStateAuthority || Runner == null) return;

        // Zemin kontrolü - raycast ile normal bul
        // Player scale'ine göre raycast origin ve mesafe ayarla
        float scaledHeight = 0.5f * CurrentScale;
        Vector3 rayOrigin = transform.position + Vector3.up * scaledHeight;

        // Küçük player için daha uzun mesafe kullan
        float dynamicCheckDistance = groundCheckDistance + (startScale - CurrentScale) * 3f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            // Spawn pozisyonu: tam zemin yüzeyinde
            Vector3 spawnPos = hit.point;

            // Spawn rotasyonu: zemin normaline göre
            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // Network üzerinden spawn
            NetworkObject spawnedObj = Runner.Spawn(
                droppedPickupPrefab,
                spawnPos,
                spawnRot,
                Object.InputAuthority
            );

            // DroppedPickup ayarlarını yap
            if (spawnedObj != null && spawnedObj.TryGetComponent<DroppedPickup>(out var pickup))
            {
                pickup.SetLifetime(droppedPickupLifetime);
                pickup.SetGrowthAmount(droppedPickupGrowthAmount);
                pickup.SetScaleMultiplier(CurrentScale); // Prefab'ın base scale'i korunur
            }
        }
    }

    #endregion

    #region RPC METHODS

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestGrow(float amount)
    {
        CurrentScale = Mathf.Clamp(CurrentScale + amount, minScale, maxScale);
        UpdateStatsBasedOnScale();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDespawnPickup(NetworkId pickupId)
    {
        // Host pickup'ı despawn eder
        if (Runner == null) return;

        if (Runner.TryFindObject(pickupId, out NetworkObject netObj))
        {
            Debug.Log($"[Player] RPC_RequestDespawnPickup: Despawning pickup {pickupId}");
            Runner.Despawn(netObj);
        }
        else
        {
            Debug.LogWarning($"[Player] RPC_RequestDespawnPickup: Pickup {pickupId} not found!");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawnPickup(Vector3 playerPosition, float playerScale)
    {
        Debug.Log($"[Player] RPC_RequestSpawnPickup received! Position: {playerPosition}, Scale: {playerScale}");

        // StateAuthority (host) pickup'ı spawn eder
        if (Runner == null || droppedPickupPrefab == null)
        {
            Debug.LogWarning("[Player] Runner null or prefab null!");
            return;
        }

        // Zemin kontrolü - raycast ile normal bul
        float scaledHeight = 0.5f * playerScale;
        Vector3 rayOrigin = playerPosition + Vector3.up * scaledHeight;
        float dynamicCheckDistance = groundCheckDistance + (startScale - playerScale) * 3f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 spawnPos = hit.point;
            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);

            NetworkObject spawnedObj = Runner.Spawn(
                droppedPickupPrefab,
                spawnPos,
                spawnRot,
                Object.InputAuthority
            );

            Debug.Log($"[Player] Pickup spawned: {(spawnedObj != null ? "SUCCESS" : "FAILED")}");

            if (spawnedObj != null && spawnedObj.TryGetComponent<DroppedPickup>(out var pickup))
            {
                pickup.SetLifetime(droppedPickupLifetime);
                pickup.SetGrowthAmount(droppedPickupGrowthAmount);
                pickup.SetScaleMultiplier(playerScale);
            }
        }
        else
        {
            Debug.LogWarning("[Player] Ground raycast failed!");
        }
    }

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
        // Interpolation değerlerini hemen güncelle
        _interpolatedPosition = RespawnPos;
        _positionVelocity = Vector3.zero;
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

        // Interpolation değerlerini hemen güncelle (teleport sonrası jitter önleme)
        _interpolatedPosition = targetPosition;
        _positionVelocity = Vector3.zero;

        NetIsTouchingWall = false;
        NetIsJumping = false;
    }
    #endregion
}
