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

    [Header("Slope Settings")]
    [SerializeField] private float maxWalkableSlope = 45f; // Yürünebilir maksimum eğim açısı (derece)
    [SerializeField] private float slopeCheckDistance = 1.5f; // Zemin kontrolü ray mesafesi
    [SerializeField] private float slopeCheckRadius = 0.3f; // SphereCast yarıçapı

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

    [Header("Punch System")]
    [SerializeField] private float punchRange = 3f;              // Punch menzili
    [SerializeField] private float punchRadius = 0.5f;           // SphereCast yarıçapı (hassasiyet)
    [SerializeField] private float punchCooldown = 0.5f;         // Punch cooldown süresi
    [SerializeField] private float punchKnockbackDuration = 0.3f;// Knockback süresi
    [SerializeField] private LayerMask punchTargetLayers = ~0;   // Hedef layer'ları
    [SerializeField] private Vector3 punchOriginOffset = new Vector3(0f, 0.8f, 0f); // Punch başlangıç noktası offset

    [Header("Punch Knockback Settings")]
    [SerializeField] private float knockbackUpwardForce = 8f;        // Yukarı fırlatma gücü (Inspector'dan ayarla)
    [SerializeField] private float knockbackPushForce = 12f;         // Geri itme gücü (Inspector'dan ayarla)
    [SerializeField] private float knockbackDecay = 6f;              // Yavaşlama hızı (düşük = uzun kayma)

    [Header("Punch Gizmos")]
    [SerializeField] private bool showPunchGizmos = true;        // Gizmos göster/gizle
    [SerializeField] private Color gizmoRangeColor = new Color(1f, 0.5f, 0f, 0.3f);  // Menzil rengi
    [SerializeField] private Color gizmoRayColor = Color.red;    // Ray rengi
    [SerializeField] private Color gizmoHitColor = Color.green;  // Hit rengi

    [Header("Player Collision System")]
    [SerializeField] private float playerCollisionRadius = 0.8f;  // Collision algılama yarıçapı
    [SerializeField] private LayerMask playerLayer;               // Player layer'ı (Inspector'dan ayarla)
    [SerializeField] private float antiStackingForce = 25f;       // Üst üste binmeyi engelleme gücü
    [SerializeField] private float minPlayerDistance = 0.6f;      // Minimum oyuncu mesafesi

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem walkingFX;            // Yürürken toz efekti

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
    [Networked] public NetworkBool MovementLocked { get; set; }

    // Punch System - Networked
    [Networked] private NetworkBool IsBeingKnockedBack { get; set; }
    [Networked] private Vector3 KnockbackDirection { get; set; }
    [Networked] private float KnockbackStartTime { get; set; }
    [Networked] private float KnockbackForceMultiplier { get; set; } // Scale bazlı güç çarpanı
    [Networked] private TickTimer PunchCooldownTimer { get; set; }

    // Anti-Launch System - Collision kaynaklı yukarı fırlamayı engeller
    [Networked] private NetworkBool DidJump { get; set; } // Gerçekten zıpladı mı (space basıldı)

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

    // Slope System - Local
    private bool _isOnWalkableSlope = false; // Yürünebilir eğimde mi
    private float _currentSlopeAngle = 0f; // Mevcut eğim açısı
    private Vector3 _slopeNormal = Vector3.up; // Zemin normali

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

    // Slope Camera Smoothing - eğimde ekstra Y ekseni yumuşatma
    private float _cameraTargetY; // Kamera hedef Y pozisyonu (ayrı smooth)
    private float _cameraTargetYVelocity; // Y smooth velocity
    private const float CAMERA_Y_SMOOTH_TIME_NORMAL = 0.08f; // Normal zeminde Y smooth
    private const float CAMERA_Y_SMOOTH_TIME_SLOPE = 0.25f; // Eğimde Y smooth (daha yavaş)

    // Kamera Collision - kalıcı değişkenler
    private float _cameraCollisionDistance; // Collision sonrası mesafe
    private float _cameraCollisionVelocity; // Collision smooth velocity

    // Punch System - Local
    private bool _punchAnimTriggered;             // Punch animasyonu tetiklendi mi

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
            _cameraCollisionDistance = cameraDistance; // Collision mesafesi başlangıç

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

            // Ziplama - Başka oyuncunun üstündeyken ASLA zıplayamaz
            // Eğimde de zıplayabilmeli
            bool isOnGround = _cc.Grounded || CheckSlopeGroundInline();
            bool canJump = isOnGround && !IsStandingOnPlayer();
            if (input.isJumping && canJump)
            {
                _cc.Jump();
                NetIsJumping = true;
                DidJump = true; // GERÇEK zıplama yapıldı
                _jumpTriggered = true; // Animasyon için flag
            }

            // Zıplama ve düşme durumlarını güncelle
            float verticalVelocity = _cc.Velocity.y;

            // Eğim kontrolü - yürünebilir eğimde de grounded sayılır
            bool effectivelyGrounded = _cc.Grounded || CheckSlopeGroundInline();

            // Yere değdiğinde zıplama durumunu sıfırla
            if (effectivelyGrounded)
            {
                NetIsJumping = false;
                NetIsFalling = false;
                DidJump = false; // Yere indi, zıplama flag'ini sıfırla
            }
            else
            {
                // Havadayken: düşüyor mu kontrol et
                NetIsFalling = verticalVelocity < fallingThreshold;
            }

            // Punch System - E tuşuna basıldığında
            if (input.isPunching && PunchCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                TryPunch();
                PunchCooldownTimer = TickTimer.CreateFromSeconds(Runner, punchCooldown);
                _punchAnimTriggered = true;
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

                // Pickup spawn - SADECE forward simulation'da (resimulation'da çift spawn önleme)
                // Runner.IsForward: true = gerçek simulation, false = resimulation (geçmişi tekrar hesaplama)
                if (droppedPickupPrefab != null && Runner.IsForward)
                {
                    if (_dropTimer.ExpiredOrNotRunning(Runner))
                    {
                        // SADECE StateAuthority (Host) spawn yapar
                        // Client hiç spawn yapmaz, sadece host kendi inputuyla spawn eder
                        if (Object.HasStateAuthority)
                        {
                            SpawnDroppedPickup();
                            _dropTimer = TickTimer.CreateFromSeconds(Runner, dropInterval);
                        }
                    }
                }
            }
        }

        // Knockback uygulama - tüm ticklerde çalışır (GetInput dışında)
        if (IsBeingKnockedBack && Object.HasStateAuthority)
        {
            float elapsed = (Runner.SimulationTime - KnockbackStartTime);
            if (elapsed < punchKnockbackDuration)
            {
                // Scale çarpanı (büyük oyuncu küçüğü daha sert vurur)
                float forceMultiplier = Mathf.Max(KnockbackForceMultiplier, 1f);

                // Decay faktörü - zamanla azalan kuvvet
                float decayFactor = Mathf.Exp(-knockbackDecay * elapsed);

                // Yatay knockback yönü (Y=0)
                Vector3 horizontalDir = new Vector3(KnockbackDirection.x, 0f, KnockbackDirection.z).normalized;

                // Velocity hesapla
                Vector3 currentVel = _cc.Velocity;

                // Yatay geri itme (vuruş yönünde) - scale çarpanı ile
                float pushForce = knockbackPushForce * forceMultiplier * decayFactor;
                currentVel.x = horizontalDir.x * pushForce;
                currentVel.z = horizontalDir.z * pushForce;

                // Yukarı fırlatma (sadece başlangıçta, sonra yerçekimi devralır) - scale çarpanı ile
                if (elapsed < 0.05f)
                {
                    currentVel.y = knockbackUpwardForce * forceMultiplier;
                }

                _cc.Velocity = currentVel;
            }
            else
            {
                IsBeingKnockedBack = false;
            }
        }

        // Anti-Stacking System - Oyuncuların birbirlerinin üzerine çıkmasını KESİNLİKLE engeller
        if (Object.HasStateAuthority)
        {
            EnforcePlayerSeparation();

            // === ANTI-LAUNCH SYSTEM ===
            // Space basılmadan yukarı fırlama ASLA olmaz
            // Collision kaynaklı yukarı velocity'yi engelle
            if (!DidJump && !IsBeingKnockedBack && _cc != null)
            {
                float currentYVelocity = _cc.Velocity.y;

                // Eğer zıplama yapılmadı AMA yukarı doğru hareket var
                // ve başka bir oyuncuya yakınız
                if (currentYVelocity > 0.5f && IsNearOtherPlayer())
                {
                    // Yukarı velocity'yi SIFIRLA - collision kaynaklı fırlama
                    Vector3 vel = _cc.Velocity;
                    vel.y = Mathf.Min(vel.y, 0f); // Yukarı hareket yok, sadece aşağı veya sıfır
                    _cc.Velocity = vel;
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

        // Slope kontrolünü her frame yap
        CheckSlopeGround();

        // Animasyon - Eğim dahil grounded kontrolü
        bool isGrounded = IsGroundedWithSlope();

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
            // Eğimde olduğumuzda havada değiliz, AirVertical 0 olmalı
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

            // Punch animasyonu
            if (_punchAnimTriggered && HasParameter(animator, "Punch"))
            {
                animator.SetTrigger("Punch");
                _punchAnimTriggered = false;
            }
        }

        // Flag'leri her zaman sıfırla
        _wasGrounded = isGrounded;

        // Walking FX kontrolü - sadece koşarken ve yerdeyse efekt oynat
        if (walkingFX != null)
        {
            // Rotasyonu model rotasyonuyla senkronize et (network uyumlu)
            // _interpolatedModelRotation zaten ModelRotation (Networked) değerinden türetiliyor
            walkingFX.transform.rotation = _interpolatedModelRotation;

            if (IsSprinting && isGrounded)
            {
                if (!walkingFX.isPlaying)
                    walkingFX.Play();
            }
            else
            {
                if (walkingFX.isPlaying)
                    walkingFX.Stop();
            }
        }

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
            _cameraTargetY = rawTargetPos.y;
            _cameraInitialized = true;
        }

        // ===== EĞİMDE Y EKSENİ AYRI SMOOTH =====
        // Eğim açısına göre Y smooth time'ı dinamik ayarla
        // Eğim ne kadar dikse, Y o kadar yavaş takip etsin (titreme önleme)
        float slopeFactor = Mathf.Clamp01(_currentSlopeAngle / maxWalkableSlope); // 0-1 arası
        float ySmoothTime = Mathf.Lerp(CAMERA_Y_SMOOTH_TIME_NORMAL, CAMERA_Y_SMOOTH_TIME_SLOPE, slopeFactor) * scaleFactor;

        // Y eksenini ayrı smooth et
        _cameraTargetY = Mathf.SmoothDamp(_cameraTargetY, rawTargetPos.y, ref _cameraTargetYVelocity, ySmoothTime);

        // XZ eksenlerini normal smooth et
        float targetSmoothTime = CAMERA_TARGET_SMOOTH_TIME * scaleFactor;
        Vector3 xzTarget = new Vector3(rawTargetPos.x, _cameraTargetPos.y, rawTargetPos.z);
        _cameraTargetPos = Vector3.SmoothDamp(_cameraTargetPos, xzTarget, ref _cameraTargetVelocity, targetSmoothTime);

        // Y'yi ayrı smooth edilmiş değerle değiştir
        _cameraTargetPos.y = _cameraTargetY;

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

        // ===== COLLISION - Geliştirilmiş Sistem =====
        float dynamicCollisionRadius = cameraCollisionRadius * scaleFactor;
        float dynamicOffset = cameraCollisionOffset * scaleFactor;
        float targetCollisionDistance = _currentCameraDistance; // Varsayılan: collision yok

        // SphereCast ile duvar kontrolü
        if (Physics.SphereCast(_cameraTargetPos, dynamicCollisionRadius, -direction, out RaycastHit hit,
            _currentCameraDistance + dynamicCollisionRadius, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Collision var - mesafeyi hesapla
            float collisionDist = Mathf.Max(hit.distance - dynamicOffset, dynamicMinDistance * 0.3f);
            targetCollisionDistance = collisionDist;
        }

        // Raycast ile de kontrol (SphereCast kaçırabilir)
        if (Physics.Raycast(_cameraTargetPos, -direction, out RaycastHit rayHit,
            _currentCameraDistance, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            float rayCollisionDist = Mathf.Max(rayHit.distance - dynamicOffset, dynamicMinDistance * 0.3f);
            // En yakın collision'ı kullan
            targetCollisionDistance = Mathf.Min(targetCollisionDistance, rayCollisionDist);
        }

        // Collision mesafesini smooth et
        // Duvara yaklaşırken HIZLI, uzaklaşırken yavaş
        float collisionSpeed = targetCollisionDistance < _cameraCollisionDistance ? 25f : 8f;
        _cameraCollisionDistance = Mathf.Lerp(_cameraCollisionDistance, targetCollisionDistance, Time.deltaTime * collisionSpeed);

        // Final mesafe: collision mesafesi ile hedef mesafenin minimumu
        float finalDistance = Mathf.Min(_currentCameraDistance, _cameraCollisionDistance);

        // Hedef kamera pozisyonu
        Vector3 desiredCamPos = _cameraTargetPos - direction * finalDistance;

        // ===== KATMAN 2: Kamera pozisyonunu smooth et =====
        // Eğimde daha yavaş pozisyon takibi
        float camSmoothTime = CAMERA_POS_SMOOTH_TIME * scaleFactor;
        if (slopeFactor > 0.1f)
        {
            // Eğimde ekstra smooth (1.5x - 3x arası)
            camSmoothTime *= Mathf.Lerp(1.5f, 3f, slopeFactor);
        }
        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredCamPos, ref _cameraPosVelocity, camSmoothTime);

        // ===== LOOKAT da smooth olmalı =====
        // LookAt yerine smooth rotation kullan
        // Eğimde daha yavaş rotasyon geçişi
        Vector3 lookDirection = _cameraTargetPos - cam.transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            // Eğimde rotasyon hızını düşür (20 -> 10 arası)
            float rotationSpeed = Mathf.Lerp(20f, 10f, slopeFactor);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    #region SLOPE SYSTEM

    /// <summary>
    /// Zemin eğimini kontrol eder ve yürünebilir bir eğimde olup olmadığını belirler.
    /// 45 dereceye kadar olan eğimlerde karakter "grounded" sayılır.
    /// </summary>
    private void CheckSlopeGround()
    {
        _isOnWalkableSlope = false;
        _currentSlopeAngle = 0f;
        _slopeNormal = Vector3.up;

        // Scale'e göre dinamik mesafe
        float scale = Mathf.Max(CurrentScale, 0.1f);
        float dynamicCheckDistance = slopeCheckDistance * scale;
        float dynamicRadius = slopeCheckRadius * scale;

        // Karakterin ayaklarından biraz yukarıdan ray at
        Vector3 rayOrigin = transform.position + Vector3.up * (0.2f * scale);

        // SphereCast ile zemin kontrolü (daha güvenilir)
        if (Physics.SphereCast(rayOrigin, dynamicRadius, Vector3.down, out RaycastHit hit,
            dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            _slopeNormal = hit.normal;

            // Eğim açısını hesapla (derece cinsinden)
            _currentSlopeAngle = Vector3.Angle(Vector3.up, _slopeNormal);

            // Eğim yürünebilir aralıkta mı?
            if (_currentSlopeAngle <= maxWalkableSlope)
            {
                _isOnWalkableSlope = true;
            }
        }

        // Ek raycast kontrolü (SphereCast kaçırırsa diye)
        if (!_isOnWalkableSlope)
        {
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayHit,
                dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                float rayAngle = Vector3.Angle(Vector3.up, rayHit.normal);

                if (rayAngle <= maxWalkableSlope)
                {
                    _isOnWalkableSlope = true;
                    _currentSlopeAngle = rayAngle;
                    _slopeNormal = rayHit.normal;
                }
            }
        }
    }

    /// <summary>
    /// Karakterin yerde olup olmadığını kontrol eder (eğim dahil).
    /// NetworkCharacterController.Grounded VEYA yürünebilir eğimdeyse true döner.
    /// </summary>
    private bool IsGroundedWithSlope()
    {
        // Önce normal grounded kontrolü
        if (_cc != null && _cc.Grounded)
        {
            return true;
        }

        // Eğimde mi kontrol et
        return _isOnWalkableSlope;
    }

    /// <summary>
    /// FixedUpdateNetwork için inline slope kontrolü.
    /// Her frame'de güncel sonuç döner.
    /// </summary>
    private bool CheckSlopeGroundInline()
    {
        float scale = Mathf.Max(CurrentScale, 0.1f);
        float dynamicCheckDistance = slopeCheckDistance * scale;
        float dynamicRadius = slopeCheckRadius * scale;

        Vector3 rayOrigin = transform.position + Vector3.up * (0.2f * scale);

        // SphereCast ile zemin kontrolü
        if (Physics.SphereCast(rayOrigin, dynamicRadius, Vector3.down, out RaycastHit hit,
            dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle <= maxWalkableSlope)
            {
                return true;
            }
        }

        // Ek raycast kontrolü
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayHit,
            dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float rayAngle = Vector3.Angle(Vector3.up, rayHit.normal);
            if (rayAngle <= maxWalkableSlope)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

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

    #region PLAYER COLLISION SYSTEM

    /// <summary>
    /// Oyuncuların birbirlerinin üzerine çıkmasını KESİNLİKLE engeller.
    /// Bu sistem itme YAPMAZ - sadece üst üste binmeyi engeller.
    /// Üstteki oyuncu AŞAĞI ve YANA zorlanır, ASLA yukarı kaldırılmaz.
    /// </summary>
    private void EnforcePlayerSeparation()
    {
        if (_cc == null) return;

        float myScale = Mathf.Max(CurrentScale, 0.1f);
        float dynamicRadius = playerCollisionRadius * Mathf.Max(myScale, 0.5f);
        float dynamicMinDistance = minPlayerDistance * Mathf.Max(myScale, 0.5f);

        // Çevredeki oyuncuları bul
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, dynamicRadius * 2f, playerLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider col in nearbyColliders)
        {
            // Kendi collider'ımızı atla
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Player otherPlayer = col.GetComponentInParent<Player>();
            if (otherPlayer == null || otherPlayer == this) continue;

            Vector3 myPos = transform.position;
            Vector3 otherPos = otherPlayer.transform.position;

            // Yatay mesafe (XZ düzlemi)
            Vector3 horizontalDiff = new Vector3(otherPos.x - myPos.x, 0f, otherPos.z - myPos.z);
            float horizontalDistance = horizontalDiff.magnitude;

            // Dikey fark
            float verticalDiff = otherPos.y - myPos.y;

            // === ÜST ÜSTE BİNME KONTROLÜ ===
            // Eğer BİZ diğer oyuncunun ÜSTÜNDEYSEK ve çok yakınsak
            if (verticalDiff < -0.2f && horizontalDistance < dynamicMinDistance * 1.5f)
            {
                // Biz üstteyiz - KENDİMİZİ aşağı ve yana zorla
                Vector3 escapeDir;

                if (horizontalDistance > 0.01f)
                {
                    // Yatay mesafe varsa, o yönde kaç
                    escapeDir = horizontalDiff.normalized;
                }
                else
                {
                    // Tam üstündeyiz - rastgele yön seç
                    float angle = (Object.InputAuthority.PlayerId * 137.5f) % 360f; // Deterministik "rastgele"
                    escapeDir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
                }

                // Aşağı doğru kuvvet ekle (yerçekimi gibi ama daha güçlü)
                escapeDir.y = -1f;
                escapeDir = escapeDir.normalized;

                // Kuvvet - ne kadar yakınsak o kadar güçlü
                float overlapAmount = dynamicMinDistance - horizontalDistance;
                float forceMagnitude = Mathf.Max(overlapAmount, 0.1f) * antiStackingForce;

                Vector3 separationMove = escapeDir * forceMagnitude * Runner.DeltaTime;
                _cc.Move(separationMove);
            }

            // === YATAY OVERLAP KONTROLÜ (İtme değil, sadece ayrılma) ===
            // Eğer aynı yükseklikte ve çok yakınsak
            if (Mathf.Abs(verticalDiff) < 1f && horizontalDistance < dynamicMinDistance)
            {
                // Her iki oyuncu da birbirinden uzaklaşır (eşit)
                Vector3 separationDir;

                if (horizontalDistance > 0.01f)
                {
                    // Diğer oyuncudan UZAĞA
                    separationDir = -horizontalDiff.normalized;
                }
                else
                {
                    // Üst üste - deterministik yön
                    float angle = (Object.InputAuthority.PlayerId * 137.5f) % 360f;
                    separationDir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
                }

                // ASLA yukarı kaldırma
                separationDir.y = 0f;

                float overlapAmount = dynamicMinDistance - horizontalDistance;
                float forceMagnitude = overlapAmount * antiStackingForce * 0.5f; // Yatay için daha az kuvvet

                Vector3 separationMove = separationDir * forceMagnitude * Runner.DeltaTime;
                _cc.Move(separationMove);
            }
        }
    }

    /// <summary>
    /// Başka bir oyuncuya yakın mı kontrol eder (collision kaynaklı fırlama kontrolü için)
    /// </summary>
    private bool IsNearOtherPlayer()
    {
        float myScale = Mathf.Max(CurrentScale, 0.1f);
        float checkRadius = playerCollisionRadius * myScale * 1.5f; // Biraz daha geniş alan

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, checkRadius, playerLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider col in nearbyColliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Player otherPlayer = col.GetComponentInParent<Player>();
            if (otherPlayer != null && otherPlayer != this)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Başka bir oyuncunun üstünde mi kontrol eder
    /// </summary>
    public bool IsStandingOnPlayer()
    {
        float myScale = Mathf.Max(CurrentScale, 0.1f);
        float checkRadius = playerCollisionRadius * myScale;

        // Ayakların altını kontrol et
        Vector3 checkOrigin = transform.position + Vector3.up * 0.1f;

        Collider[] belowColliders = Physics.OverlapSphere(checkOrigin + Vector3.down * 0.3f, checkRadius, playerLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider col in belowColliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Player otherPlayer = col.GetComponentInParent<Player>();
            if (otherPlayer != null && otherPlayer != this)
            {
                // Diğer oyuncunun üstündeyiz
                float heightDiff = transform.position.y - otherPlayer.transform.position.y;
                if (heightDiff > 0.2f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region PUNCH SYSTEM

    /// <summary>
    /// Önündeki oyuncuya punch atmayı dener - SphereCast ile hassas algılama
    /// </summary>
    private void TryPunch()
    {
        if (_model == null) return;

        // Model'in baktığı yön
        Vector3 punchDirection = _model.forward;
        Vector3 punchOrigin = transform.position + _model.TransformDirection(punchOriginOffset);

        // SphereCast ile direkt bakış yönünde algılama (çok daha hassas)
        if (Physics.SphereCast(punchOrigin, punchRadius, punchDirection, out RaycastHit hit, punchRange, punchTargetLayers, QueryTriggerInteraction.Ignore))
        {
            // Kendi collider'ımız mı?
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) return;

            // Player bileşeni var mı?
            Player targetPlayer = hit.collider.GetComponentInParent<Player>();
            if (targetPlayer == null || targetPlayer == this) return;

            // === PUNCH KNOCKBACK ===

            // Vuruş yönü (yatay)
            Vector3 hitDirection = (targetPlayer.transform.position - transform.position);
            hitDirection.y = 0;
            hitDirection = hitDirection.normalized;

            // Kendi scale'ine göre vuruş gücü
            // Büyüksen (scale > startScale) = daha güçlü vurursun
            // Küçüksen (scale < startScale) = daha zayıf vurursun
            float safeStartScale = startScale > 0.1f ? startScale : 1f;
            float scaleMultiplier = CurrentScale / safeStartScale;
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.3f, 3f); // Min 0.3x, Max 3x

            // Knockback yönü
            Vector3 knockbackDir = hitDirection;

            // Host ise direkt uygula, client ise RPC ile
            if (Object.HasStateAuthority)
            {
                targetPlayer.ApplyCartoonKnockback(knockbackDir, scaleMultiplier, scaleMultiplier);
            }
            else
            {
                RPC_RequestCartoonPunch(targetPlayer.Object.Id, knockbackDir, scaleMultiplier, scaleMultiplier);
            }

            Debug.Log($"[PUNCH] {Nick} -> {targetPlayer.Nick} Scale: {CurrentScale:F2} -> Güç: {scaleMultiplier:F1}x");
        }
    }

    /// <summary>
    /// Knockback uygular - force ve intensity scale'e göre ayarlanır
    /// </summary>
    public void ApplyCartoonKnockback(Vector3 direction, float force, float intensity)
    {
        if (!Object.HasStateAuthority) return;

        IsBeingKnockedBack = true;
        KnockbackDirection = direction;
        KnockbackStartTime = Runner.SimulationTime;
        KnockbackForceMultiplier = intensity; // Scale çarpanını kaydet

        // Tüm client'lara bildir
        RPC_OnCartoonKnockbackReceived(direction, force, intensity);
    }

    /// <summary>
    /// Eski knockback metodu - geriye uyumluluk için
    /// </summary>
    public void ApplyKnockback(Vector3 direction)
    {
        ApplyCartoonKnockback(direction, 1f, 1f);
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
        if (!Object.HasStateAuthority || Runner == null || droppedPickupPrefab == null) return;

        float scale = Mathf.Max(CurrentScale, 0.01f);

        // Origin: scale büyüdükçe daha yukarıdan at
        float originHeight = Mathf.Max(0.5f * scale, 0.5f);
        Vector3 rayOrigin = transform.position + Vector3.up * originHeight;

        // Ray uzunluğu: origin yüksekliği + ekstra (büyüdükçe ray da uzasın)
        float extra = 2.0f;
        float dynamicCheckDistance = originHeight + groundCheckDistance + extra;

        // Güvenlik clamp (asla 0/negatif olmasın)
        dynamicCheckDistance = Mathf.Clamp(dynamicCheckDistance, 1f, 200f);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 spawnPos = hit.point;
            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);

            NetworkObject spawnedObj = Runner.Spawn(droppedPickupPrefab, spawnPos, spawnRot, Object.InputAuthority);

            if (spawnedObj != null && spawnedObj.TryGetComponent<DroppedPickup>(out var pickup))
            {
                pickup.SetLifetime(droppedPickupLifetime);
                pickup.SetGrowthAmount(droppedPickupGrowthAmount);
                pickup.SetScaleMultiplier(scale);
            }
        }
    }


    #endregion

    #region GIZMOS

    /// <summary>
    /// Editor'da punch menzilini ve raycast'i gösterir
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showPunchGizmos) return;

        Transform model = playerModel != null ? playerModel : transform;
        Vector3 origin = transform.position + model.TransformDirection(punchOriginOffset);
        Vector3 direction = model.forward;

        // Punch menzil küresi (saydam turuncu)
        Gizmos.color = gizmoRangeColor;
        Gizmos.DrawWireSphere(origin, punchRange);

        // SphereCast başlangıç noktası (küçük küre)
        Gizmos.color = gizmoRayColor;
        Gizmos.DrawWireSphere(origin, punchRadius);

        // Punch yönü çizgisi
        Gizmos.DrawLine(origin, origin + direction * punchRange);

        // SphereCast bitiş noktası
        Gizmos.DrawWireSphere(origin + direction * punchRange, punchRadius);

        // Eğer oyun modundaysa ve hit varsa göster
        if (Application.isPlaying && Physics.SphereCast(origin, punchRadius, direction, out RaycastHit hit, punchRange, punchTargetLayers, QueryTriggerInteraction.Ignore))
        {
            // Hit noktası
            Gizmos.color = gizmoHitColor;
            Gizmos.DrawWireSphere(hit.point, 0.2f);
            Gizmos.DrawLine(origin, hit.point);
        }
    }

    /// <summary>
    /// Her zaman Gizmos göster (seçili olmasa bile)
    /// </summary>
    private void OnDrawGizmos()
    {
        // Player Collision radius'u her zaman göster
        float scale = Application.isPlaying && CurrentScale > 0 ? CurrentScale : 1f;
        float dynamicRadius = playerCollisionRadius * Mathf.Max(scale, 0.5f);

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Kırmızı, saydam (collision = tehlike)
        Gizmos.DrawWireSphere(transform.position, dynamicRadius);

        // Minimum mesafe göster
        float dynamicMinDistance = minPlayerDistance * Mathf.Max(scale, 0.5f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f); // Turuncu
        Gizmos.DrawWireSphere(transform.position, dynamicMinDistance);
    }

    #endregion

    #region RPC METHODS

    // ===== PUNCH SYSTEM RPC =====

    /// <summary>
    /// Client'tan Host'a punch isteği
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestPunch(NetworkId targetId, Vector3 knockbackDirection)
    {
        if (Runner == null) return;

        if (Runner.TryFindObject(targetId, out NetworkObject targetNetObj))
        {
            Player targetPlayer = targetNetObj.GetComponent<Player>();
            if (targetPlayer != null)
            {
                targetPlayer.ApplyKnockback(knockbackDirection);
                Debug.Log($"[PUNCH RPC] {Nick} -> {targetPlayer.Nick} knockback uygulandı");
            }
        }
    }

    /// <summary>
    /// Knockback bildirimi (eski sistem - geriye uyumluluk)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnKnockbackReceived(Vector3 direction)
    {
        Debug.Log($"[KNOCKBACK] {Nick} knockback aldı! Yön: {direction}");
    }

    /// <summary>
    /// Client'tan Host'a cartoon punch isteği
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCartoonPunch(NetworkId targetId, Vector3 knockbackDirection, float force, float intensity)
    {
        if (Runner == null) return;

        if (Runner.TryFindObject(targetId, out NetworkObject targetNetObj))
        {
            Player targetPlayer = targetNetObj.GetComponent<Player>();
            if (targetPlayer != null)
            {
                targetPlayer.ApplyCartoonKnockback(knockbackDirection, force, intensity);
                Debug.Log($"[CARTOON PUNCH RPC] {Nick} -> {targetPlayer.Nick} CARTOON knockback! Intensity: {intensity:F1}x");
            }
        }
    }

    /// <summary>
    /// Knockback bildirimi - tüm client'lara bildirir
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnCartoonKnockbackReceived(Vector3 direction, float force, float intensity)
    {
        Debug.Log($"[KNOCKBACK] {Nick} knockback aldı! Yön: {direction}, Güç: {force:F1}");
    }

    // ===== SCALE SYSTEM RPC =====

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

    public void TeleportTo(Vector3 pos, Quaternion rot)
    {
        // NetworkCharacterController kullanıyorsan en temiz yöntem:
        if (_cc != null)
        {
            _cc.Teleport(pos);
            transform.rotation = rot;
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }
    }

    // Player class'ının içine, RPC bölgesine ekle:

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayWin()
    {
        // Win animasyonu başladıysa hareket kilitle
        MovementLocked = true;

        // Kaymayı tamamen kes
        NetVelocity = Vector3.zero;
        if (_cc != null)
        {
            _cc.Velocity = Vector3.zero;
            _cc.maxSpeed = 0f;
            _cc.Move(Vector3.zero);
        }

        IsMoving = false;
        IsMovingForward = false;
        IsSprinting = false;

        if (animator != null)
        {
            if (HasParameter(animator, "Win"))
                animator.SetTrigger("Win");
            else
                Debug.LogWarning("[Player] Animator'da 'Win' trigger parametresi yok!");
        }
    }
    #endregion
}