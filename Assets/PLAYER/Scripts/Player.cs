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
    [SerializeField] private TextMeshPro nicknameText; // Player prefabÄ±na child olarak TextMeshPro 3D Text ekle ve buraya ata
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
    [SerializeField] private float fallingThreshold = -2f; // Bu deÄŸerin altÄ±nda dÃ¼ÅŸÃ¼yor sayÄ±lÄ±r
    [SerializeField] private float animationSmoothness = 8f; // Speed geÃ§iÅŸ yumuÅŸaklÄ±ÄŸÄ±
    [SerializeField] private float airAnimationSmoothness = 6f; // Havada animasyon geÃ§iÅŸ yumuÅŸaklÄ±ÄŸÄ±
    [SerializeField] private float maxJumpVelocity = 10f; // Normalize iÃ§in max yukarÄ± hÄ±z
    [SerializeField] private float maxFallVelocity = 15f; // Normalize iÃ§in max dÃ¼ÅŸme hÄ±zÄ±

    [Header("Slope Settings")]
    [SerializeField] private float maxWalkableSlope = 45f; // YÃ¼rÃ¼nebilir maksimum eÄŸim aÃ§Ä±sÄ± (derece)
    [SerializeField] private float slopeCheckDistance = 1.5f; // Zemin kontrolÃ¼ ray mesafesi
    [SerializeField] private float slopeCheckRadius = 0.3f; // SphereCast yarÄ±Ã§apÄ±

    [Header("Scale System")]
    [SerializeField] private float shrinkDuration = 60f; // Tam kÃ¼Ã§Ã¼lme sÃ¼resi (saniye)
    [SerializeField] private float minScale = 0.3f; // Minimum scale deÄŸeri
    [SerializeField] private float maxScale = 2f; // Maximum scale deÄŸeri
    [SerializeField] private float startScale = 1f; // BaÅŸlangÄ±Ã§ scale deÄŸeri
    [SerializeField] private float powerUpGrowthAmount = 0.2f; // PowerUp topladÄ±ÄŸÄ±nda bÃ¼yÃ¼me miktarÄ±
    [SerializeField] private float scaleSmoothness = 5f; // Scale geÃ§iÅŸ yumuÅŸaklÄ±ÄŸÄ±
    [SerializeField] private float sprintShrinkMultiplier = 2f; // KoÅŸarken kÃ¼Ã§Ã¼lme Ã§arpanÄ±

    [Header("Scale Effects on Stats")]
    [SerializeField] private float speedChangePerScale = 2f; // Scale deÄŸiÅŸimine gÃ¶re hÄ±z deÄŸiÅŸimi
    [SerializeField] private float jumpChangePerScale = 3f; // Scale deÄŸiÅŸimine gÃ¶re zÄ±plama deÄŸiÅŸimi

    [Header("Dropped Pickup System")]
    [SerializeField] private NetworkObject droppedPickupPrefab; // Spawn edilecek prefab (NetworkObject olarak)
    [SerializeField] private float dropInterval = 0.5f; // Ne kadar sÃ¼rede bir drop olacak
    [SerializeField] private float droppedPickupLifetime = 10f; // Pickup'Ä±n yaÅŸam sÃ¼resi
    [SerializeField] private float droppedPickupActivationDelay = 0.3f; // Spawn sonrasÄ± aktif olma sÃ¼resi
    [SerializeField] private float droppedPickupGrowthAmount = 0.05f; // ToplandÄ±ÄŸÄ±nda bÃ¼yÃ¼me miktarÄ±
    [SerializeField] private float groundCheckDistance = 2f; // Zemin kontrolÃ¼ mesafesi
    [SerializeField] private LayerMask groundLayer = ~0; // Zemin layer'Ä±

    [Header("Momentum Effect - Rig References")]
    [SerializeField] private Transform headRig;
    [SerializeField] private Transform leftArmRig;
    [SerializeField] private Transform rightArmRig;

    [Header("Momentum Effect - Settings")]
    [SerializeField] private float momentumIntensity = 15f;       // Hareket anÄ±nda geriye Ã§ekilme miktarÄ±
    [SerializeField] private float springStiffness = 80f;         // Yay sertliÄŸi (dÃ¼ÅŸÃ¼k = daha yavaÅŸ dÃ¶nÃ¼ÅŸ)
    [SerializeField] private float springDamping = 4f;            // SÃ¶nÃ¼mleme (dÃ¼ÅŸÃ¼k = daha uzun sallanma)
    [SerializeField] private float headMomentumMultiplier = 0.6f; // Kafa iÃ§in Ã§arpan
    [SerializeField] private float armMomentumMultiplier = 1.2f;  // Kollar iÃ§in Ã§arpan
    [SerializeField] private float velocitySmoothing = 8f;        // HÄ±z yumuÅŸatma

    [Header("Momentum Effect - Arm Axis")]
    [SerializeField] private bool armAxisX = true;   // Pitch (ileri/geri eÄŸilme)
    [SerializeField] private bool armAxisY = false;  // Yaw (saÄŸa/sola dÃ¶nme)
    [SerializeField] private bool armAxisZ = true;   // Roll (yana yatma)

    [Header("Punch System")]
    [SerializeField] private float punchRange = 3f;              // Punch menzili
    [SerializeField] private float punchRadius = 0.5f;           // SphereCast yarÄ±Ã§apÄ± (hassasiyet)
    [SerializeField] private float punchCooldown = 0.5f;         // Punch cooldown sÃ¼resi
    [SerializeField] private float punchKnockbackDuration = 0.3f;// Knockback sÃ¼resi
    [SerializeField] private LayerMask punchTargetLayers = ~0;   // Hedef layer'larÄ±
    [SerializeField] private Vector3 punchOriginOffset = new Vector3(0f, 0.8f, 0f); // Punch baÅŸlangÄ±Ã§ noktasÄ± offset

    [Header("Punch Knockback Settings")]
    [SerializeField] private float knockbackUpwardForce = 8f;        // YukarÄ± fÄ±rlatma gÃ¼cÃ¼ (Inspector'dan ayarla)
    [SerializeField] private float knockbackPushForce = 12f;         // Geri itme gÃ¼cÃ¼ (Inspector'dan ayarla)
    [SerializeField] private float knockbackDecay = 6f;              // YavaÅŸlama hÄ±zÄ± (dÃ¼ÅŸÃ¼k = uzun kayma)

    [Header("Punch Gizmos")]
    [SerializeField] private bool showPunchGizmos = true;        // Gizmos gÃ¶ster/gizle
    [SerializeField] private Color gizmoRangeColor = new Color(1f, 0.5f, 0f, 0.3f);  // Menzil rengi
    [SerializeField] private Color gizmoRayColor = Color.red;    // Ray rengi
    [SerializeField] private Color gizmoHitColor = Color.green;  // Hit rengi

    [Header("Player Collision System")]
    [SerializeField] private float playerCollisionRadius = 0.8f;  // Collision algÄ±lama yarÄ±Ã§apÄ±
    [SerializeField] private LayerMask playerLayer;               // Player layer'Ä± (Inspector'dan ayarla)
    [SerializeField] private float antiStackingForce = 25f;       // Ãœst Ã¼ste binmeyi engelleme gÃ¼cÃ¼
    [SerializeField] private float minPlayerDistance = 0.6f;      // Minimum oyuncu mesafesi

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem walkingFX;            // YÃ¼rÃ¼rken toz efekti
    [SerializeField] private float particleGroundCheckDistance = 5f; // Zemin normal kontrolÃ¼ mesafesi

    [Header("Particle Effect Prefabs (Network Synced)")]
    [SerializeField] private GameObject jumpParticlePrefab;       // ZÄ±plarken spawn edilecek particle prefab
    
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
    [Networked] private float KnockbackForceMultiplier { get; set; } // Scale bazlÄ± gÃ¼Ã§ Ã§arpanÄ±
    [Networked] private TickTimer PunchCooldownTimer { get; set; }

    // Anti-Launch System - Collision kaynaklÄ± yukarÄ± fÄ±rlamayÄ± engeller
    [Networked] private NetworkBool DidJump { get; set; } // GerÃ§ekten zÄ±pladÄ± mÄ± (space basÄ±ldÄ±)

    // Boost System - Networked
    [Networked] public NetworkBool HasJumpBoost { get; set; }
    [Networked] public NetworkBool HasSpeedBoost { get; set; }
    [Networked] public NetworkBool HasScaleBoost { get; set; }
    [Networked] public TickTimer JumpBoostTimer { get; set; }
    [Networked] public TickTimer SpeedBoostTimer { get; set; }
    [Networked] public TickTimer ScaleBoostTimer { get; set; }
    [Networked] public float JumpBoostMultiplier { get; set; }
    [Networked] public float SpeedBoostMultiplier { get; set; }
    [Networked] public float ScaleBoostAmount { get; set; }

    // Fan/Wind Push System - Networked
    [Networked] private Vector3 ExternalPush { get; set; }
    [Networked] public NetworkBool IsInUpdraft { get; set; }

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
    private bool _landingTriggered = false; // Landing animasyonu iÃ§in

    // Slope System - Local
    private bool _isOnWalkableSlope = false; // YÃ¼rÃ¼nebilir eÄŸimde mi
    private float _currentSlopeAngle = 0f; // Mevcut eÄŸim aÃ§Ä±sÄ±
    private Vector3 _slopeNormal = Vector3.up; // Zemin normali

    // Scale System - Local
    private float _displayScale; // GÃ¶rsel smooth scale
    private float _scaleVelocity; // SmoothDamp iÃ§in velocity
    private float _baseMoveSpeed; // BaÅŸlangÄ±Ã§ yÃ¼rÃ¼me hÄ±zÄ±
    private float _baseSprintSpeed; // BaÅŸlangÄ±Ã§ koÅŸu hÄ±zÄ±
    private float _baseJumpForce; // BaÅŸlangÄ±Ã§ zÄ±plama gÃ¼cÃ¼
    private TickTimer _dropTimer; // Pickup drop zamanlayÄ±cÄ±sÄ±
    private float _pendingScaleChange; // BirikmiÅŸ scale deÄŸiÅŸikliÄŸi (threshold iÃ§in)
    private const float SCALE_UPDATE_THRESHOLD = 0.02f; // Network update threshold
    private const float SCALE_SMOOTH_TIME = 0.15f; // Scale geÃ§iÅŸ sÃ¼resi

    // Nickname - Local
    private Camera _mainCamera; // Billboard efekti iÃ§in ana kamera
    private string _lastNick; // Nick deÄŸiÅŸikliÄŸi kontrolÃ¼ iÃ§in

    // Momentum Effect - Local variables
    private Quaternion _headInitialRotation;
    private Quaternion _leftArmInitialRotation;
    private Quaternion _rightArmInitialRotation;

    // Spring physics - her rig iÃ§in offset ve velocity
    private Vector3 _headCurrentOffset;
    private Vector3 _headVelocity;
    private Vector3 _leftArmCurrentOffset;
    private Vector3 _leftArmVelocity;
    private Vector3 _rightArmCurrentOffset;
    private Vector3 _rightArmVelocity;

    // Hareket takibi
    private Vector3 _smoothedVelocity;
    private Vector3 _lastPosition;

    // Interpolation iÃ§in
    private Quaternion _interpolatedModelRotation;
    private Vector3 _interpolatedPosition; // Pozisyon smoothing
    private Vector3 _positionVelocity; // SmoothDamp iÃ§in
    private float _smoothMovingForward; // IsMovingForward iÃ§in smooth deÄŸer
    private bool _positionInitialized;
    private const float ROTATION_INTERPOLATION_SPEED = 20f;
    private const float POSITION_SMOOTH_TIME = 0.05f; // 50ms smoothing
    private const float MOVEMENT_STATE_SMOOTHING = 12f;

    // Kamera smoothing - ayrÄ± sistem (jitter Ã¶nleme)
    private Vector3 _cameraTargetPos; // Kamera hedef pozisyonu
    private Vector3 _cameraTargetVelocity; // Kamera hedef smooth velocity
    private Vector3 _cameraPosVelocity; // Kamera pozisyon smooth velocity
    private float _cameraDistanceVelocity; // Kamera mesafe smooth velocity
    private bool _cameraInitialized;
    private const float CAMERA_TARGET_SMOOTH_TIME = 0.08f; // Hedef smooth sÃ¼resi
    private const float CAMERA_POS_SMOOTH_TIME = 0.06f; // Kamera pozisyon smooth sÃ¼resi

    // Slope Camera Smoothing - eÄŸimde ekstra Y ekseni yumuÅŸatma
    private float _cameraTargetY; // Kamera hedef Y pozisyonu (ayrÄ± smooth)
    private float _cameraTargetYVelocity; // Y smooth velocity
    private const float CAMERA_Y_SMOOTH_TIME_NORMAL = 0.08f; // Normal zeminde Y smooth
    private const float CAMERA_Y_SMOOTH_TIME_SLOPE = 0.25f; // EÄŸimde Y smooth (daha yavaÅŸ)

    // Kamera Collision - kalÄ±cÄ± deÄŸiÅŸkenler
    private float _cameraCollisionDistance; // Collision sonrasÄ± mesafe
    private float _cameraCollisionVelocity; // Collision smooth velocity

    // Wiggle Lock System - Spawn/Teleport sonrasÄ± sallanmayÄ± engeller
    private float _wiggleLockUntilTime; // Bu zamana kadar wiggle kilitli
    private const float WIGGLE_LOCK_DURATION = 0.5f; // Kilit sÃ¼resi (saniye)

    // Punch System - Local
    private bool _punchAnimTriggered;             // Punch animasyonu tetiklendi mi

    // Particle System - Ã‡oklu spawn Ã¶nleme (TRIGGER tarafÄ±)
    private bool _didJumpParticle;

    // Particle System - Ã‡oklu spawn Ã¶nleme (SPAWN/RPC tarafÄ±)
    private float _lastJumpSpawnTime = -999f;
    private const float PARTICLE_SPAWN_COOLDOWN = 0.3f;

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

        // Scale System baÅŸlangÄ±Ã§ deÄŸerleri
        _baseMoveSpeed = moveSpeed;
        _baseSprintSpeed = sprintSpeed;
        _baseJumpForce = jumpForce;

        // Sadece StateAuthority scale'i baÅŸlatÄ±r
        if (Object.HasStateAuthority)
        {
            CurrentScale = startScale;
        }
        _displayScale = CurrentScale > 0 ? CurrentScale : startScale;
        transform.localScale = Vector3.one * _displayScale;

        // Wiggle efekti iÃ§in baÅŸlangÄ±Ã§ rotasyonlarÄ±nÄ± kaydet
        InitializeWiggleSystem();

        // Spawn sonrasÄ± wiggle kilidini aktifleÅŸtir (sallanmayÄ± Ã¶nle)
        LockWiggleTemporarily();

        if (Object.HasInputAuthority)
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 20f;
            _targetCameraDistance = cameraDistance;
            _currentCameraDistance = cameraDistance;
            _cameraCollisionDistance = cameraDistance; // Collision mesafesi baÅŸlangÄ±Ã§

            if (cam == null)
                cam = Camera.main;

            if (cam != null)
            {
                cam.enabled = true;
                cam.gameObject.SetActive(true);
            }

            // Kamera smoothing baÅŸlangÄ±Ã§ deÄŸerleri
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

        // Nickname sistemi baÅŸlat
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

            // Local yaw'Ä± da gÃ¼ncelle (kamera ile senkron)
            if (Object.HasInputAuthority)
            {
                _yaw = Yaw;
            }

            // Hareket
            Vector3 dir = input.direction;
            bool moving = dir.sqrMagnitude > 0.01f;
            IsMoving = moving;
            IsMovingForward = dir.z > 0.1f; // Sadece W basÄ±lÄ±yken
            IsSprinting = input.isSprinting && moving;

            // Speed Boost Ã§arpanÄ± uygula
            float baseSpeed = IsSprinting ? sprintSpeed : moveSpeed;
            float speedMultiplier = HasSpeedBoost ? SpeedBoostMultiplier : 1f;
            float speed = baseSpeed * speedMultiplier;

            // Kamera yÃ¶nlerini hesapla (kameranÄ±n baktÄ±ÄŸÄ± aÃ§Ä±ya gÃ¶re)
            Vector3 forward = Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, Yaw, 0) * Vector3.right;

            if (moving)
            {
                // Hareket yÃ¶nÃ¼nÃ¼ hesapla (kameraya gÃ¶re)
                // W = ileri (forward), S = geri (-forward)
                // A = sol (-right), D = saÄŸ (right)
                Vector3 moveDir = (forward * dir.z + right * dir.x).normalized;

                _cc.maxSpeed = speed;
                _cc.Move(moveDir * speed);

                // Hareket yÃ¶nÃ¼ne dÃ¶ndÃ¼r
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                ModelRotation = Quaternion.Slerp(ModelRotation, targetRot, Runner.DeltaTime * rotationSpeed);
            }
            else
            {
                _cc.Move(Vector3.zero);
            }

            // Ziplama - BaÅŸka oyuncunun Ã¼stÃ¼ndeyken ASLA zÄ±playamaz
            // EÄŸimde de zÄ±playabilmeli
            bool isOnGround = _cc.Grounded || CheckSlopeGroundInline();
            bool canJump = isOnGround && !IsStandingOnPlayer();
            if (input.isJumping && canJump)
            {
                // Jump Boost Ã§arpanÄ± uygula
                float jumpMultiplier = HasJumpBoost ? JumpBoostMultiplier : 1f;
                _cc.jumpImpulse = jumpForce * jumpMultiplier;
                _cc.Jump();
                NetIsJumping = true;
                DidJump = true;
                _jumpTriggered = true;

                // Jump particle - SADECE InputAuthority'de, forward'da ve 1 KERE
                if (Object.HasInputAuthority && Runner.IsForward && !_didJumpParticle)
                {
                    _didJumpParticle = true;
                    TriggerJumpParticle();
                }
            }

            // ZÄ±plama ve dÃ¼ÅŸme durumlarÄ±nÄ± gÃ¼ncelle
            float verticalVelocity = _cc.Velocity.y;
            bool effectivelyGrounded = _cc.Grounded || CheckSlopeGroundInline();

            // Yere deÄŸdiÄŸinde sÄ±fÄ±rla
            if (effectivelyGrounded)
            {
                NetIsJumping = false;
                NetIsFalling = false;
                DidJump = false;
                if (Object.HasInputAuthority) _didJumpParticle = false;
            }
            else
            {
                // Havadayken: dÃ¼ÅŸÃ¼yor mu kontrol et
                NetIsFalling = verticalVelocity < fallingThreshold;
            }

            // Punch System - E tuÅŸuna basÄ±ldÄ±ÄŸÄ±nda
            if (input.isPunching && PunchCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                TryPunch();
                PunchCooldownTimer = TickTimer.CreateFromSeconds(Runner, punchCooldown);
                _punchAnimTriggered = true;
            }

            // Scale System - Zamana baÄŸlÄ± kÃ¼Ã§Ã¼lme (GetInput iÃ§inde - input olan oyuncu iÃ§in)
            if (shrinkDuration > 0 && moving)
            {
                float shrinkRate = (startScale - minScale) / shrinkDuration;

                // KoÅŸarken daha hÄ±zlÄ± kÃ¼Ã§Ã¼l
                if (input.isSprinting)
                {
                    shrinkRate *= sprintShrinkMultiplier;
                }

                // StateAuthority scale'i gÃ¼nceller
                if (Object.HasStateAuthority)
                {
                    float newScale = CurrentScale - (shrinkRate * Runner.DeltaTime);
                    CurrentScale = Mathf.Clamp(newScale, minScale, maxScale);
                    UpdateStatsBasedOnScale();
                }

                // Pickup spawn - SADECE forward simulation'da (resimulation'da Ã§ift spawn Ã¶nleme)
                // Runner.IsForward: true = gerÃ§ek simulation, false = resimulation (geÃ§miÅŸi tekrar hesaplama)
                if (droppedPickupPrefab != null && Runner.IsForward)
                {
                    if (_dropTimer.ExpiredOrNotRunning(Runner))
                    {
                        // SADECE StateAuthority (Host) spawn yapar
                        // Client hiÃ§ spawn yapmaz, sadece host kendi inputuyla spawn eder
                        if (Object.HasStateAuthority)
                        {
                            SpawnDroppedPickup();
                            _dropTimer = TickTimer.CreateFromSeconds(Runner, dropInterval);
                        }
                    }
                }
            }
        }

        // Knockback uygulama - tÃ¼m ticklerde Ã§alÄ±ÅŸÄ±r (GetInput dÄ±ÅŸÄ±nda)
        if (IsBeingKnockedBack && Object.HasStateAuthority)
        {
            float elapsed = (Runner.SimulationTime - KnockbackStartTime);
            if (elapsed < punchKnockbackDuration)
            {
                // Scale Ã§arpanÄ± (bÃ¼yÃ¼k oyuncu kÃ¼Ã§Ã¼ÄŸÃ¼ daha sert vurur)
                float forceMultiplier = Mathf.Max(KnockbackForceMultiplier, 1f);

                // Decay faktÃ¶rÃ¼ - zamanla azalan kuvvet
                float decayFactor = Mathf.Exp(-knockbackDecay * elapsed);

                // Yatay knockback yÃ¶nÃ¼ (Y=0)
                Vector3 horizontalDir = new Vector3(KnockbackDirection.x, 0f, KnockbackDirection.z).normalized;

                // Velocity hesapla
                Vector3 currentVel = _cc.Velocity;

                // Yatay geri itme (vuruÅŸ yÃ¶nÃ¼nde) - scale Ã§arpanÄ± ile
                float pushForce = knockbackPushForce * forceMultiplier * decayFactor;
                currentVel.x = horizontalDir.x * pushForce;
                currentVel.z = horizontalDir.z * pushForce;

                // YukarÄ± fÄ±rlatma (sadece baÅŸlangÄ±Ã§ta, sonra yerÃ§ekimi devralÄ±r) - scale Ã§arpanÄ± ile
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

        // Anti-Stacking System - OyuncularÄ±n birbirlerinin Ã¼zerine Ã§Ä±kmasÄ±nÄ± KESÄ°NLÄ°KLE engeller
        if (Object.HasStateAuthority)
        {
            EnforcePlayerSeparation();

            // === ANTI-LAUNCH SYSTEM ===
            // Space basÄ±lmadan yukarÄ± fÄ±rlama ASLA olmaz
            // Collision kaynaklÄ± yukarÄ± velocity'yi engelle
            if (!DidJump && !IsBeingKnockedBack && _cc != null)
            {
                float currentYVelocity = _cc.Velocity.y;

                // EÄŸer zÄ±plama yapÄ±lmadÄ± AMA yukarÄ± doÄŸru hareket var
                // ve baÅŸka bir oyuncuya yakÄ±nÄ±z
                if (currentYVelocity > 0.5f && IsNearOtherPlayer())
                {
                    // YukarÄ± velocity'yi SIFIRLA - collision kaynaklÄ± fÄ±rlama
                    Vector3 vel = _cc.Velocity;
                    vel.y = Mathf.Min(vel.y, 0f); // YukarÄ± hareket yok, sadece aÅŸaÄŸÄ± veya sÄ±fÄ±r
                    _cc.Velocity = vel;
                }
            }
        }

        // Boost System - Timer kontrolÃ¼ (sadece StateAuthority)
        if (Object.HasStateAuthority)
        {
            // Jump Boost timer kontrolÃ¼
            if (HasJumpBoost && JumpBoostTimer.Expired(Runner))
            {
                HasJumpBoost = false;
                JumpBoostMultiplier = 1f;
                Debug.Log($"[Boost] {Nick} Jump Boost bitti!");
            }

            // Speed Boost timer kontrolÃ¼
            if (HasSpeedBoost && SpeedBoostTimer.Expired(Runner))
            {
                HasSpeedBoost = false;
                SpeedBoostMultiplier = 1f;
                Debug.Log($"[Boost] {Nick} Speed Boost bitti!");
            }

            // Scale Boost timer kontrolÃ¼
            if (HasScaleBoost && ScaleBoostTimer.Expired(Runner))
            {
                HasScaleBoost = false;
                // Scale'i geri al
                CurrentScale = Mathf.Clamp(CurrentScale - ScaleBoostAmount, minScale, maxScale);
                ScaleBoostAmount = 0f;
                Debug.Log($"[Boost] {Nick} Scale Boost bitti!");
            }
        }
    }

    public override void Render()
    {
        // Scale System - Smooth scale geÃ§iÅŸi (tÃ¼m oyuncular iÃ§in)
        if (CurrentScale > 0)
        {
            // SmoothDamp kullan - Lerp'ten Ã§ok daha smooth, jitter Ã¶nler
            _displayScale = Mathf.SmoothDamp(_displayScale, CurrentScale, ref _scaleVelocity, SCALE_SMOOTH_TIME);
            transform.localScale = Vector3.one * _displayScale;
        }

        // Model rotasyonunu INTERPOLATE ederek uygula (jitter Ã¶nleme)
        // Scale'e gÃ¶re daha yavaÅŸ rotasyon geÃ§iÅŸi
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

        // Slope kontrolÃ¼nÃ¼ her frame yap
        CheckSlopeGround();

        // Animasyon - EÄŸim dahil grounded kontrolÃ¼
        bool isGrounded = IsGroundedWithSlope();

        if (animator != null)
        {
            // YÃ¼rÃ¼me/KoÅŸma animasyonu
            float targetSpeed = IsMoving ? (IsSprinting ? 1f : 0.5f) : 0f;
            _currentAnimSpeed = Mathf.MoveTowards(_currentAnimSpeed, targetSpeed, Time.deltaTime * animationSmoothness);
            animator.SetFloat("Speed", _currentAnimSpeed);

            // Animasyon hÄ±zÄ±nÄ± karakter hÄ±zÄ±na gÃ¶re ayarla
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

            // Hareket durumu - Landing'den Ã§Ä±kÄ±ÅŸ iÃ§in
            if (HasParameter(animator, "IsMoving"))
                animator.SetBool("IsMoving", IsMoving);

            // Havada animasyon - Blend Tree iÃ§in AirVertical deÄŸeri
            // 0 = JumpUp (yukarÄ±), 1 = Falling (dÃ¼ÅŸme)
            // EÄŸimde olduÄŸumuzda havada deÄŸiliz, AirVertical 0 olmalÄ±
            float verticalVelocity = _cc != null ? _cc.Velocity.y : 0f;
            float targetAirVertical = 0f;

            if (!isGrounded)
            {
                if (verticalVelocity > 0)
                {
                    // YukarÄ± gidiyor - JumpUp (0)
                    targetAirVertical = 0f;
                }
                else
                {
                    // AÅŸaÄŸÄ± dÃ¼ÅŸÃ¼yor - Falling (1'e doÄŸru)
                    targetAirVertical = Mathf.Clamp01(Mathf.Abs(verticalVelocity) / maxFallVelocity);
                }
            }

            _currentAirVertical = Mathf.MoveTowards(_currentAirVertical, targetAirVertical, Time.deltaTime * airAnimationSmoothness);

            if (HasParameter(animator, "AirVertical"))
                animator.SetFloat("AirVertical", _currentAirVertical);

            // Landing - havadayken yere deÄŸdiÄŸi an
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

        // Flag'leri her zaman sÄ±fÄ±rla
        _wasGrounded = isGrounded;

        // Walking FX kontrolÃ¼ - sadece koÅŸarken ve yerdeyse efekt oynat
        if (walkingFX != null)
        {
            // Rotasyonu model rotasyonuyla senkronize et (network uyumlu)
            // _interpolatedModelRotation zaten ModelRotation (Networked) deÄŸerinden tÃ¼retiliyor
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

        // Movement state smoothing (jitter Ã¶nleme)
        float targetMovingForward = IsMovingForward ? 1f : 0f;
        _smoothMovingForward = Mathf.Lerp(_smoothMovingForward, targetMovingForward, Time.deltaTime * MOVEMENT_STATE_SMOOTHING);

        // Nickname gÃ¼ncelle ve billboard efekti
        UpdateNicknameDisplay();
    }

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid) return;

        // Pozisyon smoothing - TÃœM playerlar iÃ§in (jitter Ã¶nleme)
        // Scale'e gÃ¶re dinamik smooth time
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

        // Wiggle efekti Animator'dan SONRA uygulanmalÄ±
        UpdateWiggleEffect();

        // Kamera gÃ¼ncellemesi LateUpdate'te (tÃ¼m transform gÃ¼ncellemelerinden sonra)
        if (Object.HasInputAuthority)
        {
            UpdateCameraLate();
        }

        // Nickname billboard efekti - her frame kameraya baksÄ±n
        UpdateNicknameBillboard();
    }

    private void Update()
    {
        // Billboard efekti - TÃœM playerlar iÃ§in her frame Ã§alÄ±ÅŸÄ±r
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
    /// LateUpdate'te Ã§aÄŸrÄ±lan kamera gÃ¼ncellemesi - Ã§ift katmanlÄ± smoothing ile jitter Ã¶nleme
    /// </summary>
    private void UpdateCameraLate()
    {
        if (cam == null) return;

        // Scale faktÃ¶rÃ¼ hesapla
        float safeStartScale = startScale > 0.01f ? startScale : 1f;
        float scaleRatio = _displayScale / safeStartScale;
        float scaleFactor = Mathf.Clamp(scaleRatio, 0.5f, 3f);

        // Dinamik deÄŸerler
        float dynamicTargetDistance = _targetCameraDistance * scaleFactor;
        float dynamicMinDistance = minCameraDistance * scaleFactor;
        float dynamicMaxDistance = maxCameraDistance * scaleFactor;
        dynamicTargetDistance = Mathf.Clamp(dynamicTargetDistance, dynamicMinDistance, dynamicMaxDistance);
        float dynamicCameraHeight = cameraHeight * scaleFactor;

        // ===== KATMAN 1: Hedef pozisyonu smooth et (network jitter absorbe) =====
        // Client'ta network sync jitter'Ä±nÄ± Ã¶nlemek iÃ§in interpolated pozisyon kullan
        Vector3 basePosition = _positionInitialized ? _interpolatedPosition : transform.position;
        Vector3 rawTargetPos = basePosition + Vector3.up * dynamicCameraHeight;

        // Ä°lk Ã§aÄŸrÄ±da baÅŸlat
        if (!_cameraInitialized)
        {
            _cameraTargetPos = rawTargetPos;
            _cameraTargetY = rawTargetPos.y;
            _cameraInitialized = true;
        }

        // ===== EÄÄ°MDE Y EKSENÄ° AYRI SMOOTH =====
        // EÄŸim aÃ§Ä±sÄ±na gÃ¶re Y smooth time'Ä± dinamik ayarla
        // EÄŸim ne kadar dikse, Y o kadar yavaÅŸ takip etsin (titreme Ã¶nleme)
        float slopeFactor = Mathf.Clamp01(_currentSlopeAngle / maxWalkableSlope); // 0-1 arasÄ±
        float ySmoothTime = Mathf.Lerp(CAMERA_Y_SMOOTH_TIME_NORMAL, CAMERA_Y_SMOOTH_TIME_SLOPE, slopeFactor) * scaleFactor;

        // Y eksenini ayrÄ± smooth et
        _cameraTargetY = Mathf.SmoothDamp(_cameraTargetY, rawTargetPos.y, ref _cameraTargetYVelocity, ySmoothTime);

        // XZ eksenlerini normal smooth et
        float targetSmoothTime = CAMERA_TARGET_SMOOTH_TIME * scaleFactor;
        Vector3 xzTarget = new Vector3(rawTargetPos.x, _cameraTargetPos.y, rawTargetPos.z);
        _cameraTargetPos = Vector3.SmoothDamp(_cameraTargetPos, xzTarget, ref _cameraTargetVelocity, targetSmoothTime);

        // Y'yi ayrÄ± smooth edilmiÅŸ deÄŸerle deÄŸiÅŸtir
        _cameraTargetPos.y = _cameraTargetY;

        // ===== ZOOM SMOOTH =====
        float zoomSmoothTime = 0.12f * scaleFactor;
        _currentCameraDistance = Mathf.SmoothDamp(_currentCameraDistance, dynamicTargetDistance, ref _cameraDistanceVelocity, zoomSmoothTime);

        // ===== YAW SMOOTH (ileri giderken) =====
        if (_smoothMovingForward > 0.5f)
        {
            float modelYaw = _interpolatedModelRotation.eulerAngles.y;
            float lerpFactor = Time.deltaTime * cameraSmoothness * _smoothMovingForward * 0.5f; // Daha yavaÅŸ
            _yaw = Mathf.LerpAngle(_yaw, modelYaw, lerpFactor);
        }

        // Kamera yÃ¶nÃ¼
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 direction = rotation * Vector3.forward;

        // ===== COLLISION - GeliÅŸtirilmiÅŸ Sistem =====
        float dynamicCollisionRadius = cameraCollisionRadius * scaleFactor;
        float dynamicOffset = cameraCollisionOffset * scaleFactor;
        float targetCollisionDistance = _currentCameraDistance; // VarsayÄ±lan: collision yok

        // SphereCast ile duvar kontrolÃ¼
        if (Physics.SphereCast(_cameraTargetPos, dynamicCollisionRadius, -direction, out RaycastHit hit,
            _currentCameraDistance + dynamicCollisionRadius, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Collision var - mesafeyi hesapla
            float collisionDist = Mathf.Max(hit.distance - dynamicOffset, dynamicMinDistance * 0.3f);
            targetCollisionDistance = collisionDist;
        }

        // Raycast ile de kontrol (SphereCast kaÃ§Ä±rabilir)
        if (Physics.Raycast(_cameraTargetPos, -direction, out RaycastHit rayHit,
            _currentCameraDistance, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            float rayCollisionDist = Mathf.Max(rayHit.distance - dynamicOffset, dynamicMinDistance * 0.3f);
            // En yakÄ±n collision'Ä± kullan
            targetCollisionDistance = Mathf.Min(targetCollisionDistance, rayCollisionDist);
        }

        // Collision mesafesini smooth et
        // Duvara yaklaÅŸÄ±rken HIZLI, uzaklaÅŸÄ±rken yavaÅŸ
        float collisionSpeed = targetCollisionDistance < _cameraCollisionDistance ? 25f : 8f;
        _cameraCollisionDistance = Mathf.Lerp(_cameraCollisionDistance, targetCollisionDistance, Time.deltaTime * collisionSpeed);

        // Final mesafe: collision mesafesi ile hedef mesafenin minimumu
        float finalDistance = Mathf.Min(_currentCameraDistance, _cameraCollisionDistance);

        // Hedef kamera pozisyonu
        Vector3 desiredCamPos = _cameraTargetPos - direction * finalDistance;

        // ===== KATMAN 2: Kamera pozisyonunu smooth et =====
        // EÄŸimde daha yavaÅŸ pozisyon takibi
        float camSmoothTime = CAMERA_POS_SMOOTH_TIME * scaleFactor;
        if (slopeFactor > 0.1f)
        {
            // EÄŸimde ekstra smooth (1.5x - 3x arasÄ±)
            camSmoothTime *= Mathf.Lerp(1.5f, 3f, slopeFactor);
        }
        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredCamPos, ref _cameraPosVelocity, camSmoothTime);

        // ===== LOOKAT da smooth olmalÄ± =====
        // LookAt yerine smooth rotation kullan
        // EÄŸimde daha yavaÅŸ rotasyon geÃ§iÅŸi
        Vector3 lookDirection = _cameraTargetPos - cam.transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            // EÄŸimde rotasyon hÄ±zÄ±nÄ± dÃ¼ÅŸÃ¼r (20 -> 10 arasÄ±)
            float rotationSpeed = Mathf.Lerp(20f, 10f, slopeFactor);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    #region SLOPE SYSTEM

    /// <summary>
    /// Zemin eÄŸimini kontrol eder ve yÃ¼rÃ¼nebilir bir eÄŸimde olup olmadÄ±ÄŸÄ±nÄ± belirler.
    /// 45 dereceye kadar olan eÄŸimlerde karakter "grounded" sayÄ±lÄ±r.
    /// </summary>
    private void CheckSlopeGround()
    {
        _isOnWalkableSlope = false;
        _currentSlopeAngle = 0f;
        _slopeNormal = Vector3.up;

        // Scale'e gÃ¶re dinamik mesafe
        float scale = Mathf.Max(CurrentScale, 0.1f);
        float dynamicCheckDistance = slopeCheckDistance * scale;
        float dynamicRadius = slopeCheckRadius * scale;

        // Karakterin ayaklarÄ±ndan biraz yukarÄ±dan ray at
        Vector3 rayOrigin = transform.position + Vector3.up * (0.2f * scale);

        // SphereCast ile zemin kontrolÃ¼ (daha gÃ¼venilir)
        if (Physics.SphereCast(rayOrigin, dynamicRadius, Vector3.down, out RaycastHit hit,
            dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            _slopeNormal = hit.normal;

            // EÄŸim aÃ§Ä±sÄ±nÄ± hesapla (derece cinsinden)
            _currentSlopeAngle = Vector3.Angle(Vector3.up, _slopeNormal);

            // EÄŸim yÃ¼rÃ¼nebilir aralÄ±kta mÄ±?
            if (_currentSlopeAngle <= maxWalkableSlope)
            {
                _isOnWalkableSlope = true;
            }
        }

        // Ek raycast kontrolÃ¼ (SphereCast kaÃ§Ä±rÄ±rsa diye)
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
    /// Karakterin yerde olup olmadÄ±ÄŸÄ±nÄ± kontrol eder (eÄŸim dahil).
    /// NetworkCharacterController.Grounded VEYA yÃ¼rÃ¼nebilir eÄŸimdeyse true dÃ¶ner.
    /// </summary>
    private bool IsGroundedWithSlope()
    {
        // Ã–nce normal grounded kontrolÃ¼
        if (_cc != null && _cc.Grounded)
        {
            return true;
        }

        // EÄŸimde mi kontrol et
        return _isOnWalkableSlope;
    }

    /// <summary>
    /// FixedUpdateNetwork iÃ§in inline slope kontrolÃ¼.
    /// Her frame'de gÃ¼ncel sonuÃ§ dÃ¶ner.
    /// </summary>
    private bool CheckSlopeGroundInline()
    {
        float scale = Mathf.Max(CurrentScale, 0.1f);
        float dynamicCheckDistance = slopeCheckDistance * scale;
        float dynamicRadius = slopeCheckRadius * scale;

        Vector3 rayOrigin = transform.position + Vector3.up * (0.2f * scale);

        // SphereCast ile zemin kontrolÃ¼
        if (Physics.SphereCast(rayOrigin, dynamicRadius, Vector3.down, out RaycastHit hit,
            dynamicCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle <= maxWalkableSlope)
            {
                return true;
            }
        }

        // Ek raycast kontrolÃ¼
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

        // Spring deÄŸerlerini sÄ±fÄ±rla
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

        // Wiggle kilitli mi kontrol et (spawn/teleport sonrasÄ±)
        if (Time.time < _wiggleLockUntilTime)
        {
            // Kilitli - pozisyonu gÃ¼ncelle ama efekt uygulama
            _lastPosition = transform.position;
            _smoothedVelocity = Vector3.zero;
            return;
        }

        // Karakterin dÃ¼nya uzayÄ±ndaki hÄ±zÄ±nÄ± hesapla
        Vector3 currentPosition = transform.position;
        Vector3 worldVelocity = (currentPosition - _lastPosition) / deltaTime;
        _lastPosition = currentPosition;

        // AÅŸÄ±rÄ± hÄ±z kontrolÃ¼ - teleport algÄ±lama (saniyede 50 birimden fazla = muhtemelen teleport)
        if (worldVelocity.sqrMagnitude > 2500f) // 50^2 = 2500
        {
            // Ani pozisyon deÄŸiÅŸikliÄŸi - muhtemelen teleport, wiggle'Ä± kilitle
            LockWiggleTemporarily();
            return;
        }

        // HÄ±zÄ± yumuÅŸat (ani deÄŸiÅŸimleri Ã¶nle)
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, worldVelocity, deltaTime * velocitySmoothing);

        // HÄ±zÄ± karakterin lokal uzayÄ±na Ã§evir (model rotasyonuna gÃ¶re)
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
        // Hareket yÃ¶nÃ¼nÃ¼n tersine offset hesapla (geriden takip efekti)
        // Ä°leri giderken -> geriye eÄŸilim
        // SaÄŸa giderken -> sola eÄŸilim
        // ZÄ±plarken -> aÅŸaÄŸÄ± eÄŸilim (yukarÄ± momentum)
        // DÃ¼ÅŸerken -> yukarÄ± eÄŸilim (aÅŸaÄŸÄ± momentum)

        // Target offset: hÄ±z vektÃ¶rÃ¼nÃ¼n tersi yÃ¶nÃ¼nde rotasyon
        // X ekseni (pitch): ileri/geri + yukarÄ±/aÅŸaÄŸÄ± hareket -> Ã¶ne/arkaya eÄŸilme
        // Y ekseni (yaw): saÄŸ/sol hareket -> yanlara eÄŸilme

        // Kafa iÃ§in hedef offset
        Vector3 headTargetOffset = Vector3.zero;
        if (localVelocity.sqrMagnitude > 0.1f)
        {
            // Dikey hareket iÃ§in pitch etkisi (zÄ±plama/dÃ¼ÅŸme)
            float verticalPitch = -localVelocity.y * momentumIntensity * headMomentumMultiplier * 0.08f;

            headTargetOffset = new Vector3(
                -localVelocity.z * momentumIntensity * headMomentumMultiplier * 0.1f + verticalPitch,  // Ä°leri + dikey -> pitch
                localVelocity.x * momentumIntensity * headMomentumMultiplier * 0.05f,  // SaÄŸa git -> sola bak (yaw)
                -localVelocity.x * momentumIntensity * headMomentumMultiplier * 0.03f  // Hafif roll
            );
        }

        // Kollar iÃ§in hedef offset (daha abartÄ±lÄ±)
        Vector3 armTargetOffset = Vector3.zero;
        if (localVelocity.sqrMagnitude > 0.1f)
        {
            // Dikey hareket iÃ§in kollar (zÄ±plama = kollar aÅŸaÄŸÄ±, dÃ¼ÅŸme = kollar yukarÄ±)
            float verticalArm = -localVelocity.y * momentumIntensity * armMomentumMultiplier * 0.12f;

            float armX = armAxisX ? (-localVelocity.z * momentumIntensity * armMomentumMultiplier * 0.15f + verticalArm) : 0f;
            float armY = armAxisY ? (-localVelocity.x * momentumIntensity * armMomentumMultiplier * 0.08f) : 0f;
            float armZ = armAxisZ ? (-localVelocity.x * momentumIntensity * armMomentumMultiplier * 0.05f) : 0f;

            armTargetOffset = new Vector3(armX, armY, armZ);
        }

        // Spring physics ile smooth geÃ§iÅŸ ve sallanma
        UpdateSpringPhysics(ref _headCurrentOffset, ref _headVelocity, headTargetOffset, deltaTime);
        UpdateSpringPhysics(ref _leftArmCurrentOffset, ref _leftArmVelocity, armTargetOffset, deltaTime);
        UpdateSpringPhysics(ref _rightArmCurrentOffset, ref _rightArmVelocity, armTargetOffset, deltaTime);

        // RotasyonlarÄ± uygula
        ApplyMomentumRotation(headRig, _headCurrentOffset);
        ApplyMomentumRotation(leftArmRig, _leftArmCurrentOffset);
        ApplyMomentumRotation(rightArmRig, _rightArmCurrentOffset);
    }

    private void UpdateSpringPhysics(ref Vector3 currentOffset, ref Vector3 velocity, Vector3 targetOffset, float deltaTime)
    {
        // Spring physics: F = -k(x - target) - b*v
        // Hedef pozisyona doÄŸru Ã§ekiliÅŸ + sÃ¶nÃ¼mleme
        // Bu, durma anÄ±nda overshoot yapÄ±p ileri geri sallanma saÄŸlar

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

        // Mevcut animasyon rotasyonunun Ã¼zerine offset ekle
        Quaternion currentRotation = rig.localRotation;
        Quaternion offsetRotation = Quaternion.Euler(offset);
        rig.localRotation = currentRotation * offsetRotation;
    }

    /// <summary>
    /// Momentum sistemini runtime'da sÄ±fÄ±rlar
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

    /// <summary>
    /// Wiggle sistemini geÃ§ici olarak kilitler (spawn/teleport sonrasÄ± sallanmayÄ± Ã¶nler)
    /// </summary>
    public void LockWiggleTemporarily()
    {
        _wiggleLockUntilTime = Time.time + WIGGLE_LOCK_DURATION;
        _lastPosition = transform.position;
        _smoothedVelocity = Vector3.zero;

        // Mevcut offset ve velocity'leri sÄ±fÄ±rla
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
    /// Nickname text'ini gÃ¼nceller
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
    /// Nickname pozisyonunu gÃ¼nceller
    /// </summary>
    private void UpdateNicknameDisplay()
    {
        // Text yoksa Ã§Ä±k
        if (nicknameText == null) return;

        // Nick deÄŸiÅŸmiÅŸse gÃ¼ncelle
        UpdateNicknameText();

        // DoÄŸrudan TextMeshPro transform'unu kullan
        Transform textTransform = nicknameText.transform;

        // Nickname pozisyonunu gÃ¼ncelle (scale'e gÃ¶re offset ayarla)
        // INTERPOLATED pozisyon kullan (jitter Ã¶nleme)
        float scaleMultiplier = CurrentScale > 0 ? CurrentScale : 1f;
        Vector3 scaledOffset = nicknameOffset * scaleMultiplier;
        Vector3 basePos = _positionInitialized ? _interpolatedPosition : transform.position;
        textTransform.position = basePos + scaledOffset;
    }

    /// <summary>
    /// Billboard efekti - LateUpdate'te Ã§aÄŸrÄ±lÄ±r
    /// </summary>
    private void UpdateNicknameBillboard()
    {
        BillboardNickname();
    }

    /// <summary>
    /// Nickname'i kameraya dÃ¶ndÃ¼rÃ¼r - Update'te Ã§aÄŸrÄ±lÄ±r
    /// </summary>
    private void BillboardNickname()
    {
        if (nicknameText == null) return;

        // Aktif kamerayÄ± bul
        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        // Text'in kameraya dÃ¶nÃ¼k olmasÄ± iÃ§in - kameradan uzaÄŸa baksÄ±n
        Transform textTransform = nicknameText.transform;
        Vector3 directionFromCamera = textTransform.position - activeCam.transform.position;
        textTransform.rotation = Quaternion.LookRotation(directionFromCamera);
    }

    /// <summary>
    /// Aktif kamerayÄ± bulur (Camera.main yoksa diÄŸer aktif kameralarÄ± arar)
    /// </summary>
    private Camera GetActiveCamera()
    {
        // Ã–nce cached camera'yÄ± kontrol et
        if (_mainCamera != null && _mainCamera.isActiveAndEnabled)
            return _mainCamera;

        // Camera.main dene
        Camera cam = Camera.main;
        if (cam != null)
        {
            _mainCamera = cam;
            return cam;
        }

        // MainCamera tag'i yoksa aktif kameralarÄ± ara
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
    /// OyuncularÄ±n birbirlerinin Ã¼zerine Ã§Ä±kmasÄ±nÄ± KESÄ°NLÄ°KLE engeller.
    /// Bu sistem itme YAPMAZ - sadece Ã¼st Ã¼ste binmeyi engeller.
    /// Ãœstteki oyuncu AÅAÄI ve YANA zorlanÄ±r, ASLA yukarÄ± kaldÄ±rÄ±lmaz.
    /// </summary>
    private void EnforcePlayerSeparation()
    {
        if (_cc == null) return;

        float myScale = Mathf.Max(CurrentScale, 0.1f);
        float dynamicRadius = playerCollisionRadius * Mathf.Max(myScale, 0.5f);
        float dynamicMinDistance = minPlayerDistance * Mathf.Max(myScale, 0.5f);

        // Ã‡evredeki oyuncularÄ± bul
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, dynamicRadius * 2f, playerLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider col in nearbyColliders)
        {
            // Kendi collider'Ä±mÄ±zÄ± atla
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Player otherPlayer = col.GetComponentInParent<Player>();
            if (otherPlayer == null || otherPlayer == this) continue;

            Vector3 myPos = transform.position;
            Vector3 otherPos = otherPlayer.transform.position;

            // Yatay mesafe (XZ dÃ¼zlemi)
            Vector3 horizontalDiff = new Vector3(otherPos.x - myPos.x, 0f, otherPos.z - myPos.z);
            float horizontalDistance = horizontalDiff.magnitude;

            // Dikey fark
            float verticalDiff = otherPos.y - myPos.y;

            // === ÃœST ÃœSTE BÄ°NME KONTROLÃœ ===
            // EÄŸer BÄ°Z diÄŸer oyuncunun ÃœSTÃœNDEYSEK ve Ã§ok yakÄ±nsak
            if (verticalDiff < -0.2f && horizontalDistance < dynamicMinDistance * 1.5f)
            {
                // Biz Ã¼stteyiz - KENDÄ°MÄ°ZÄ° aÅŸaÄŸÄ± ve yana zorla
                Vector3 escapeDir;

                if (horizontalDistance > 0.01f)
                {
                    // Yatay mesafe varsa, o yÃ¶nde kaÃ§
                    escapeDir = horizontalDiff.normalized;
                }
                else
                {
                    // Tam Ã¼stÃ¼ndeyiz - rastgele yÃ¶n seÃ§
                    float angle = (Object.InputAuthority.PlayerId * 137.5f) % 360f; // Deterministik "rastgele"
                    escapeDir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
                }

                // AÅŸaÄŸÄ± doÄŸru kuvvet ekle (yerÃ§ekimi gibi ama daha gÃ¼Ã§lÃ¼)
                escapeDir.y = -1f;
                escapeDir = escapeDir.normalized;

                // Kuvvet - ne kadar yakÄ±nsak o kadar gÃ¼Ã§lÃ¼
                float overlapAmount = dynamicMinDistance - horizontalDistance;
                float forceMagnitude = Mathf.Max(overlapAmount, 0.1f) * antiStackingForce;

                Vector3 separationMove = escapeDir * forceMagnitude * Runner.DeltaTime;
                _cc.Move(separationMove);
            }

            // === YATAY OVERLAP KONTROLÃœ (Ä°tme deÄŸil, sadece ayrÄ±lma) ===
            // EÄŸer aynÄ± yÃ¼kseklikte ve Ã§ok yakÄ±nsak
            if (Mathf.Abs(verticalDiff) < 1f && horizontalDistance < dynamicMinDistance)
            {
                // Her iki oyuncu da birbirinden uzaklaÅŸÄ±r (eÅŸit)
                Vector3 separationDir;

                if (horizontalDistance > 0.01f)
                {
                    // DiÄŸer oyuncudan UZAÄA
                    separationDir = -horizontalDiff.normalized;
                }
                else
                {
                    // Ãœst Ã¼ste - deterministik yÃ¶n
                    float angle = (Object.InputAuthority.PlayerId * 137.5f) % 360f;
                    separationDir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
                }

                // ASLA yukarÄ± kaldÄ±rma
                separationDir.y = 0f;

                float overlapAmount = dynamicMinDistance - horizontalDistance;
                float forceMagnitude = overlapAmount * antiStackingForce * 0.5f; // Yatay iÃ§in daha az kuvvet

                Vector3 separationMove = separationDir * forceMagnitude * Runner.DeltaTime;
                _cc.Move(separationMove);
            }
        }
    }

    /// <summary>
    /// BaÅŸka bir oyuncuya yakÄ±n mÄ± kontrol eder (collision kaynaklÄ± fÄ±rlama kontrolÃ¼ iÃ§in)
    /// </summary>
    private bool IsNearOtherPlayer()
    {
        float myScale = Mathf.Max(CurrentScale, 0.1f);
        float checkRadius = playerCollisionRadius * myScale * 1.5f; // Biraz daha geniÅŸ alan

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
    /// BaÅŸka bir oyuncunun Ã¼stÃ¼nde mi kontrol eder
    /// </summary>
    public bool IsStandingOnPlayer()
    {
        float myScale = Mathf.Max(CurrentScale, 0.1f);
        float checkRadius = playerCollisionRadius * myScale;

        // AyaklarÄ±n altÄ±nÄ± kontrol et
        Vector3 checkOrigin = transform.position + Vector3.up * 0.1f;

        Collider[] belowColliders = Physics.OverlapSphere(checkOrigin + Vector3.down * 0.3f, checkRadius, playerLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider col in belowColliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Player otherPlayer = col.GetComponentInParent<Player>();
            if (otherPlayer != null && otherPlayer != this)
            {
                // DiÄŸer oyuncunun Ã¼stÃ¼ndeyiz
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
    /// Ã–nÃ¼ndeki oyuncuya punch atmayÄ± dener - SphereCast ile hassas algÄ±lama
    /// </summary>
    private void TryPunch()
    {
        if (_model == null) return;

        // Model'in baktÄ±ÄŸÄ± yÃ¶n
        Vector3 punchDirection = _model.forward;
        Vector3 punchOrigin = transform.position + _model.TransformDirection(punchOriginOffset);

        // SphereCast ile direkt bakÄ±ÅŸ yÃ¶nÃ¼nde algÄ±lama (Ã§ok daha hassas)
        if (Physics.SphereCast(punchOrigin, punchRadius, punchDirection, out RaycastHit hit, punchRange, punchTargetLayers, QueryTriggerInteraction.Ignore))
        {
            // Kendi collider'Ä±mÄ±z mÄ±?
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) return;

            // Player bileÅŸeni var mÄ±?
            Player targetPlayer = hit.collider.GetComponentInParent<Player>();
            if (targetPlayer == null || targetPlayer == this) return;

            // === PUNCH KNOCKBACK ===

            // VuruÅŸ yÃ¶nÃ¼ (yatay)
            Vector3 hitDirection = (targetPlayer.transform.position - transform.position);
            hitDirection.y = 0;
            hitDirection = hitDirection.normalized;

            // Kendi scale'ine gÃ¶re vuruÅŸ gÃ¼cÃ¼
            // BÃ¼yÃ¼ksen (scale > startScale) = daha gÃ¼Ã§lÃ¼ vurursun
            // KÃ¼Ã§Ã¼ksen (scale < startScale) = daha zayÄ±f vurursun
            float safeStartScale = startScale > 0.1f ? startScale : 1f;
            float scaleMultiplier = CurrentScale / safeStartScale;
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.3f, 3f); // Min 0.3x, Max 3x

            // Knockback yÃ¶nÃ¼
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

            Debug.Log($"[PUNCH] {Nick} -> {targetPlayer.Nick} Scale: {CurrentScale:F2} -> GÃ¼Ã§: {scaleMultiplier:F1}x");
        }
    }

    /// <summary>
    /// Knockback uygular - force ve intensity scale'e gÃ¶re ayarlanÄ±r
    /// </summary>
    public void ApplyCartoonKnockback(Vector3 direction, float force, float intensity)
    {
        if (!Object.HasStateAuthority) return;

        IsBeingKnockedBack = true;
        KnockbackDirection = direction;
        KnockbackStartTime = Runner.SimulationTime;
        KnockbackForceMultiplier = intensity; // Scale Ã§arpanÄ±nÄ± kaydet

        // TÃ¼m client'lara bildir
        RPC_OnCartoonKnockbackReceived(direction, force, intensity);
    }

    /// <summary>
    /// Eski knockback metodu - geriye uyumluluk iÃ§in
    /// </summary>
    public void ApplyKnockback(Vector3 direction)
    {
        ApplyCartoonKnockback(direction, 1f, 1f);
    }

    #endregion

    #region SCALE SYSTEM

    /// <summary>
    /// Scale deÄŸerine gÃ¶re hÄ±z ve zÄ±plama gÃ¼cÃ¼nÃ¼ gÃ¼nceller
    /// </summary>
    private void UpdateStatsBasedOnScale()
    {
        if (_cc == null) return;

        // Scale farkÄ±nÄ± hesapla (startScale'e gÃ¶re)
        float scaleDiff = CurrentScale - startScale;

        // HÄ±z ve zÄ±plama deÄŸerlerini gÃ¼ncelle
        moveSpeed = _baseMoveSpeed + (scaleDiff * speedChangePerScale);
        sprintSpeed = _baseSprintSpeed + (scaleDiff * speedChangePerScale);
        jumpForce = _baseJumpForce + (scaleDiff * jumpChangePerScale);

        // Minimum deÄŸerlerin altÄ±na dÃ¼ÅŸmesin
        moveSpeed = Mathf.Max(moveSpeed, 1f);
        sprintSpeed = Mathf.Max(sprintSpeed, 2f);
        jumpForce = Mathf.Max(jumpForce, 3f);

        // NetworkCharacterController'a uygula
        _cc.jumpImpulse = jumpForce;
    }

    /// <summary>
    /// Karakteri bÃ¼yÃ¼tÃ¼r (PowerUp topladÄ±ÄŸÄ±nda Ã§aÄŸrÄ±lÄ±r)
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
    /// Photon Fusion'da physics events HOST'ta iÅŸlenir, bu yÃ¼zden StateAuthority kontrolÃ¼ yapÄ±yoruz.
    /// CurrentScale [Networked] olduÄŸu iÃ§in deÄŸiÅŸiklikler otomatik tÃ¼m client'lara sync olur.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Sadece StateAuthority (Host) collision iÅŸlesin - network sync iÃ§in
        // Physics events host'ta Ã§alÄ±ÅŸÄ±r, client'ta HasInputAuthority false dÃ¶ner
        if (!Object.HasStateAuthority) return;

        // Hareket etmiyorsa toplama
        if (!IsMoving) return;

        // DroppedPickup kontrolÃ¼
        if (other.TryGetComponent<DroppedPickup>(out var droppedPickup))
        {
            // Aktif deÄŸilse toplama
            if (!droppedPickup.CanBeCollected()) return;

            // NetworkObject'i al
            if (!other.TryGetComponent<NetworkObject>(out var netObj)) return;

            // BÃ¼yÃ¼me uygula - StateAuthority'deyiz, direkt gÃ¼ncelleyebiliriz
            float growth = droppedPickup.GrowthAmount;
            CurrentScale = Mathf.Clamp(CurrentScale + growth, minScale, maxScale);
            UpdateStatsBasedOnScale();

            // Despawn - StateAuthority olduÄŸumuz iÃ§in direkt yapabiliriz
            if (Runner != null)
            {
                Runner.Despawn(netObj);
            }
            return;
        }

        // Normal Åimdi  kontrolÃ¼
        if (other.CompareTag("PowerUp"))
        {
            // BÃ¼yÃ¼me uygula - StateAuthority'deyiz, direkt gÃ¼ncelleyebiliriz
            CurrentScale = Mathf.Clamp(CurrentScale + powerUpGrowthAmount, minScale, maxScale);
            UpdateStatsBasedOnScale();

            // PowerUp objesini yok et (network Ã¼zerinden)
            if (Runner != null && other.TryGetComponent<NetworkObject>(out var netObj))
            {
                Runner.Despawn(netObj);
            }
            else
            {
                // Network objesi deÄŸilse normal destroy
                Destroy(other.gameObject);
            }
        }

        if (other.gameObject.CompareTag("Death"))
        {
            // Death zone'a girdi
        }
    }

    /// <summary>
    /// Scale'i sÄ±fÄ±rlar (baÅŸlangÄ±Ã§ deÄŸerine dÃ¶ndÃ¼rÃ¼r)
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
    /// Zemine gÃ¶re pickup spawn eder
    /// </summary>
    private void SpawnDroppedPickup()
    {
        if (!Object.HasStateAuthority || Runner == null || droppedPickupPrefab == null) return;

        float scale = Mathf.Max(CurrentScale, 0.01f);

        // Origin: scale bÃ¼yÃ¼dÃ¼kÃ§e daha yukarÄ±dan at
        float originHeight = Mathf.Max(0.5f * scale, 0.5f);
        Vector3 rayOrigin = transform.position + Vector3.up * originHeight;

        // Ray uzunluÄŸu: origin yÃ¼ksekliÄŸi + ekstra (bÃ¼yÃ¼dÃ¼kÃ§e ray da uzasÄ±n)
        float extra = 2.0f;
        float dynamicCheckDistance = originHeight + groundCheckDistance + extra;

        // GÃ¼venlik clamp (asla 0/negatif olmasÄ±n)
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
    /// Editor'da punch menzilini ve raycast'i gÃ¶sterir
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showPunchGizmos) return;

        Transform model = playerModel != null ? playerModel : transform;
        Vector3 origin = transform.position + model.TransformDirection(punchOriginOffset);
        Vector3 direction = model.forward;

        // Punch menzil kÃ¼resi (saydam turuncu)
        Gizmos.color = gizmoRangeColor;
        Gizmos.DrawWireSphere(origin, punchRange);

        // SphereCast baÅŸlangÄ±Ã§ noktasÄ± (kÃ¼Ã§Ã¼k kÃ¼re)
        Gizmos.color = gizmoRayColor;
        Gizmos.DrawWireSphere(origin, punchRadius);

        // Punch yÃ¶nÃ¼ Ã§izgisi
        Gizmos.DrawLine(origin, origin + direction * punchRange);

        // SphereCast bitiÅŸ noktasÄ±
        Gizmos.DrawWireSphere(origin + direction * punchRange, punchRadius);

        // EÄŸer oyun modundaysa ve hit varsa gÃ¶ster
        if (Application.isPlaying && Physics.SphereCast(origin, punchRadius, direction, out RaycastHit hit, punchRange, punchTargetLayers, QueryTriggerInteraction.Ignore))
        {
            // Hit noktasÄ±
            Gizmos.color = gizmoHitColor;
            Gizmos.DrawWireSphere(hit.point, 0.2f);
            Gizmos.DrawLine(origin, hit.point);
        }
    }

    /// <summary>
    /// Her zaman Gizmos gÃ¶ster (seÃ§ili olmasa bile)
    /// </summary>
    private void OnDrawGizmos()
    {
        // Player Collision radius'u her zaman gÃ¶ster
        float scale = Application.isPlaying && CurrentScale > 0 ? CurrentScale : 1f;
        float dynamicRadius = playerCollisionRadius * Mathf.Max(scale, 0.5f);

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // KÄ±rmÄ±zÄ±, saydam (collision = tehlike)
        Gizmos.DrawWireSphere(transform.position, dynamicRadius);

        // Minimum mesafe gÃ¶ster
        float dynamicMinDistance = minPlayerDistance * Mathf.Max(scale, 0.5f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f); // Turuncu
        Gizmos.DrawWireSphere(transform.position, dynamicMinDistance);
    }

    #endregion

    #region RPC METHODS

    // ===== PUNCH SYSTEM RPC =====

    /// <summary>
    /// Client'tan Host'a punch isteÄŸi
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
                Debug.Log($"[PUNCH RPC] {Nick} -> {targetPlayer.Nick} knockback uygulandÄ±");
            }
        }
    }

    /// <summary>
    /// Knockback bildirimi (eski sistem - geriye uyumluluk)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnKnockbackReceived(Vector3 direction)
    {
        Debug.Log($"[KNOCKBACK] {Nick} knockback aldÄ±! YÃ¶n: {direction}");
    }

    /// <summary>
    /// Client'tan Host'a cartoon punch isteÄŸi
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
    /// Knockback bildirimi - tÃ¼m client'lara bildirir
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnCartoonKnockbackReceived(Vector3 direction, float force, float intensity)
    {
        Debug.Log($"[KNOCKBACK] {Nick} knockback aldÄ±! YÃ¶n: {direction}, GÃ¼Ã§: {force:F1}");
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
        // Host pickup'Ä± despawn eder
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

    // ===== PARTICLE EFFECT SYSTEM (Network Synced) =====
    // InputAuthority TRIGGER eder â†’ Host SPAWN eder â†’ TÃ¼m client'lar gÃ¶rÃ¼r

    private void SpawnParticleLocal(GameObject prefab, Vector3 position, Vector3 groundNormal)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[Player] SpawnParticleLocal: Prefab is NULL! Cannot spawn particle.");
            return;
        }

        Debug.Log($"[Player] SpawnParticleLocal: Spawning {prefab.name} at {position}");
        Quaternion rotation = Quaternion.LookRotation(groundNormal);
        GameObject particleObj = Instantiate(prefab, position, rotation);

        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Destroy(particleObj, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(particleObj, 3f);
        }
    }

    private bool GetGroundInfo(out Vector3 groundPosition, out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;
        groundPosition = transform.position;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, particleGroundCheckDistance, groundLayer))
        {
            groundNormal = hit.normal;
            groundPosition = hit.point;
            return true;
        }
        return false;
    }

    // ===== JUMP PARTICLE =====

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnJumpParticle(Vector3 position, Vector3 normal)
    {
        // Cooldown kontrolÃ¼ - Ã§oklu spawn Ã¶nle
        if (Time.time - _lastJumpSpawnTime < PARTICLE_SPAWN_COOLDOWN) return;
        _lastJumpSpawnTime = Time.time;
        SpawnParticleLocal(jumpParticlePrefab, position, normal);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawnJumpParticle(Vector3 position, Vector3 normal)
    {
        RPC_SpawnJumpParticle(position, normal);
    }

    public void TriggerJumpParticle()
    {
        if (Runner == null) return;

        GetGroundInfo(out Vector3 groundPosition, out Vector3 groundNormal);

        if (Object.HasStateAuthority)
        {
            // HOST: Direkt spawn
            RPC_SpawnJumpParticle(groundPosition, groundNormal);
        }
        else if (Object.HasInputAuthority)
        {
            // CLIENT: Host'a istek gÃ¶nder
            RPC_RequestSpawnJumpParticle(groundPosition, groundNormal);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetRespawnPos(Vector3 pos)
    {
        RespawnPos = pos;
    }

    // Host Ã§aÄŸÄ±rÄ±r: oyuncuyu son respawn noktasÄ±na Ä±ÅŸÄ±nlar
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TeleportToRespawn()
    {
        transform.position = RespawnPos;
        // Interpolation deÄŸerlerini hemen gÃ¼ncelle
        _interpolatedPosition = RespawnPos;
        _positionVelocity = Vector3.zero;

        // Wiggle sistemini kilitle (teleport sonrasÄ± sallanmayÄ± Ã¶nle)
        LockWiggleTemporarily();
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
            Debug.Log($"<color=green> lk spawn noktas na d n yorsun!</color>");
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
                    Debug.Log($"<color=green>Spawn noktas na    nland n z!</color>");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Teleport hatas : {e.Message}");
                transform.position = targetPosition;
            }
        }
        else
        {
            transform.position = targetPosition;
        }

        // Interpolation deÄŸerlerini hemen gÃ¼ncelle (teleport sonrasÄ± jitter Ã¶nleme)
        _interpolatedPosition = targetPosition;
        _positionVelocity = Vector3.zero;

        // Wiggle sistemini kilitle (teleport sonrasÄ± sallanmayÄ± Ã¶nle)
        LockWiggleTemporarily();

        NetIsTouchingWall = false;
        NetIsJumping = false;
    }

    public void TeleportTo(Vector3 pos, Quaternion rot)
    {
        // NetworkCharacterController kullanÄ±yorsan en temiz yÃ¶ntem:
        if (_cc != null)
        {
            _cc.Teleport(pos);
            transform.rotation = rot;
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }

        // Interpolation ve wiggle gÃ¼ncelle
        _interpolatedPosition = pos;
        _positionVelocity = Vector3.zero;
        LockWiggleTemporarily();
    }

    // Player class'Ä±nÄ±n iÃ§ine, RPC bÃ¶lgesine ekle:

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayWin()
    {
        // Win animasyonu baÅŸladÄ±ysa hareket kilitle
        MovementLocked = true;

        // KaymayÄ± tamamen kes
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

    // ===== BOOST SYSTEM =====

    /// <summary>
    /// Boost uygular - Sadece StateAuthority cagirir
    /// </summary>
    public void ApplyBoost(PowerUpType type, float duration, float value)
    {
        if (!Object.HasStateAuthority) return;

        switch (type)
        {
            case PowerUpType.JumpBoost:
                HasJumpBoost = true;
                JumpBoostMultiplier = value;
                JumpBoostTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;

            case PowerUpType.SpeedBoost:
                HasSpeedBoost = true;
                SpeedBoostMultiplier = value;
                SpeedBoostTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;

            case PowerUpType.ScaleBoost:
                HasScaleBoost = true;
                ScaleBoostAmount = value;
                ScaleBoostTimer = TickTimer.CreateFromSeconds(Runner, duration);
                CurrentScale = Mathf.Clamp(CurrentScale + value, minScale, maxScale);
                break;
        }
    }

    /// <summary>
    /// Client -> Host: Boost uygulama istegi
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestApplyBoost(PowerUpType type, float duration, float value)
    {
        ApplyBoost(type, duration, value);
    }

    /// <summary>
    /// Kalan boost suresini dondurur (saniye)
    /// </summary>
    public float GetBoostRemainingTime(PowerUpType type)
    {
        if (Runner == null) return 0f;

        switch (type)
        {
            case PowerUpType.JumpBoost:
                if (!HasJumpBoost) return 0f;
                return JumpBoostTimer.RemainingTime(Runner).GetValueOrDefault(0f);

            case PowerUpType.SpeedBoost:
                if (!HasSpeedBoost) return 0f;
                return SpeedBoostTimer.RemainingTime(Runner).GetValueOrDefault(0f);

            case PowerUpType.ScaleBoost:
                if (!HasScaleBoost) return 0f;
                return ScaleBoostTimer.RemainingTime(Runner).GetValueOrDefault(0f);

            default:
                return 0f;
        }
    }
}
