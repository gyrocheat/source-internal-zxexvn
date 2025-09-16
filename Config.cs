using System.Diagnostics;
using System.Media;
using System.Numerics;
using System.Reflection;
using static AotForms.ESP;

namespace AotForms
{
    internal static class Config
    {

        internal static bool AimBot = false;
        //internal static Keys AimbotKey = Keys.LButton;
        internal static bool AimbotEnabled = false; // Changed from Aimbot to AimbotEnabled for clarity
        internal static Keys AimbotKey = Keys.LButton;
        internal static Keys Silent1 = Keys.LButton;
        internal static Keys Silent2 = Keys.LButton;
        internal static Keys AimbotKeyONOFF = Keys.None;
        internal static bool AimBotLegit = false;
        internal static float AimBotMaxDistance = 120;
        internal static int expsize = 8;
        internal static float AimBotFov = 180f;
        internal static bool AimbotZexEnabled = false;
        internal static int espran = 150;
        internal static bool AimbotZex = false;
        internal static bool AimbotAIv8 = false;
        internal static bool AimbotLegitV2 = false;
        internal static bool AimbotZexAI = false;
        internal static bool AimbotZexV2 = false;
        internal static bool AimbotV3 = false;
        internal static bool AimbotSmoothV3 = false;
        internal static bool CloseEnabled = false;
        internal static float AimWriteCooldown = 30f;
        internal static float AimWriteCooldownV2 = 30f;
        internal static int AimbotSmoothSteps = 5; // Number of steps for smooth aiming
        internal static bool UseDelay = true;
        internal static bool UseSmooth = true;
        internal static bool UseSensi = true;
        internal static float AimbotSpeed = 3f; // Default speed for aimbot
        internal static float AimbotDelayHold = 100f; // Delay for holding the aimbot key
        internal static bool DrawTargetLine = false;
        internal static float AimbotSensi = 5.0f; // Default sensitivity for aimbot aiming
        internal static bool AimbotMouseControl = false; // Whether to control the mouse for aiming
        internal static bool AimbotEnabledV2 = false; // New configuration option for enabling aimbot V2
        internal static float AimbotDelayHoldV2 = 0f; // Delay for holding the aimbot key in V2
        internal static bool PullEnemies = false; // New configuration option for pulling enemies towards the player
        public static float AimSnapThreshold = 2.3f;
        public static float FixRungThreshold = 0.35f;
        public static float TrackingFactor = 0.8f;
        internal static bool TeleKill = false;
        internal static bool NoReload = false;
        internal static int YoloHeadClassId = 0; // Example: Class ID for 'head' in your YOLO model
        internal static float AiConfirmationThreshold = 30f;
        internal static int AIRender = 10; // Distance threshold for AI rendering
        //  internal static bool aimsilent360 = false;
        public static float TeleOffsetX = 0.5f; // Trái/phải
        public static float distancepull = 10.0f; // Trái/phải
        public static float TeleOffsetY = 0.0f; // Trên/dưới
        public static float TeleOffsetZ = 0.0f; // Trước/sau
        internal static bool TeleportV2 = false; // Flag to enable/disable teleportation
        internal static bool TeleportV3 = false;
        internal static float TeleportOffset = 2.0f;
        internal static bool UpPlayer = false;
        internal static bool DownPlayer = false;
        public static bool FollowEnemies = false;
        internal static bool ESPWeapon = false;
        internal static bool ESPWeaponIcon = false;
        public static float AimbotDelay = 200f;
        public static float AimbotDelay1 = 0f;
        public static float AimbotDelayV2 = 0f;
        public static float FollowDelay = 2f;
        public static float AimbotDelayV3 = 0f;
        public static float AimbotCooldown = 50f;
        internal static bool teli = false;
        internal static float AimBotSmooth1 = 16;
        internal static float AimbotSmoothness = 3.0f; // Default smoothness value
        internal static bool FixCrashRender = false; // Flag to fix crash render issues
        internal static float test = 40f;
        internal static float test1 = 2f;
        public static float TeleportRange = 10;
        internal static bool ESPLevel = false;
        // Flag to toggle the particle effect on/off
        public static bool PARTICLE_OFF = false;
        internal static int EspLineThickNess = 1;
        public static float GlowRadius = 15;
        public static float FeatherAmount = 2f;
        public static float GlowOpacity = 0.02f;
        public static float AimSpeed = 3.5f;
        internal static bool ShowText = false; // Flag to show/hide text in the UI
        internal static float Smoothness = 5f; // Default smoothness value for aimbot
                                               // internal static bool AimLock360 = false;

        public static Vector4 FOVColor = new Vector4(1f, 1f, 1f, 1f);
        internal static bool ESPMode = false;
        internal static int HeadBoneId = 8; 
        internal static bool RandomReactionTime = false;
        internal static float AimCorrectionDelay = 30f; // Delay for aim correction in milliseconds
        internal static bool HardModeEnabled = false;
        internal static bool ESPWukong = false;
        internal static bool AimFov = false;
        internal static bool IgnoreKnocked = true;
        internal static bool Speed = false;
        internal static bool NoRecoil = false;
        internal static bool MagicBullet = false;
        internal static bool NoCache = false;
        internal static bool aimIsVisible = true;
        internal static bool StreamMode = false;
        internal static bool esptotalplyer = false;
        internal static bool FixEsp = false;

        internal static bool aimbot1 = false;
        internal static bool aimbot2 = false;
        internal static bool aimbot3 = false;
        internal static bool aimbot4 = false;

        internal static bool DelayMode = false; // Flag to enable/disable delay mode
        internal static int DelayMin = 0;
        internal static int DelayMax = 150;

        internal static bool minimap = false;
        internal static bool fovbtn = false;
        internal static bool fovcircle = false;
        internal static bool AimLock = false;
        public static bool espupHasBeenSet = false;
        public static bool espupHasBeenSet1 = true;
        internal static bool ESPLine = false;
        internal static bool ESPLinegr = false;
        internal static bool ESPLineZigzag = false;

        internal static bool ESPLinegrf = false;
        internal static bool BoxGlow = false;
        internal static bool EspBottom = false;
        internal static bool EspUp = false;
        internal static Color ESPLineColor = Color.White;

        internal static bool espboxst = false;

        internal static bool ESPBox = false;
        internal static Color ESPBoxColor = Color.Red;

        internal static bool ESPBox2 = false;
        internal static Color ESPBoxCColor = Color.White;
        internal static bool ESPDistance = false;
        internal static Color ESPFillBoxColor = Color.White;
        internal static bool ESPName = false;
        internal static Color ESPNameColor = Color.White;

        internal static bool ESPHealth = false;
        internal static Color ESPHeath = Color.White;

        internal static bool ESPSkeleton = false;
        internal static Color ESPSkeletonColor = Color.Red;

        internal static bool ESPFillBox = false;

        internal static bool ESPEnabled = false;

        internal static bool ESPCorner = false;
        internal static bool ESPCornerColor = false;

        internal static bool ESPInfo = false;

        internal static bool ESPFove = false;
        internal static bool espbg = false;
        internal static bool Aimfovc = false;
        internal static Color Aimfovcolor = Color.White;

        internal static bool espcfx = false;
        internal static bool sound = false;
        // New configuration option for aimbot mode
        internal static string AimBotMode = "Aggressive"; // Default to "normal" mode
        internal static int AimBotSmooth = 0;
        internal static float AimSmoothness = 0f;
        internal static int thread = 20;

        public static bool WaitingForKeybind = false;      // Waiting for user to set a keybind
        public static Keys AimBotKey = Keys.None;          // The selected key for toggling AimBot
        public static string AimBotKeyLabel = "None";      // Label for the selected key
        public static bool KeyAlreadyPressed = false;

        public static bool WaitingForKeybind1 = false;
        public static Keys PullPlayerKey = Keys.None;          // The selected key for toggling AimBot
        public static string PullKeyLabel = "None";      // Label for the selected key
        public static bool KeyAlreadyPressed1 = false;

        public static bool WaitingForKeybind2 = false;
        public static Keys TeleportKey = Keys.None;          // The selected key for toggling Teleport
        public static string TeleportKeyLabel = "None";      // Label for the selected key
        public static bool KeyAlreadyPressed2 = false;

        public static bool WaitingForKeybind3 = false;
        public static Keys TeleportV3Key = Keys.None;          // The selected key for toggling Teleport
        public static string TeleportV3KeyLabel = "None";      // Label for the selected key
        public static bool KeyAlreadyPressed3 = false;

        public static bool WaitingForKeybind4 = false;      // Waiting for user to set a keybind
        public static Keys AimBotKey1 = Keys.None;          // The selected key for toggling AimBot
        public static string AimBot1KeyLabel = "None";      // Label for the selected key
        public static bool KeyAlreadyPressed4 = false;

        public static bool WaitingForKeybind5 = false;      // Waiting for user to set a keybind
        public static Keys SpeedHacksK = Keys.None;          // The selected key for toggling AimBot
        public static string SpeedKeyLabel = "None";      // Label for the selected key
        public static bool KeyAlreadyPressed5 = false;
        public static string AimTargetPart { get; set; } = "Head"; // Valor padrão
        internal static bool FastReload = false;
        internal static bool Silent = false;
        internal static bool enableAimBot = false;
        internal static bool SilentAim = false;
        internal static bool AimBotRage = false;
        //  internal static AimBotType AimBotType;
        internal static float AimbotFarTightness = 80f;      // Độ chặt khi ở xa mục tiêu (ví dụ: 80% -> rất nhanh)
        internal static float AimbotCloseTightness = 15f;    // Độ chặt khi ở gần mục tiêu (ví dụ: 15% -> chậm và chính xác)
        internal static float AimbotSlowdownDistance = 150f; // Khoảng cách (pixel) mà aimbot bắt đầu giảm độ chặt
        internal static bool ESPLineTren = false;
        internal static Color ESPLineTrenColor = Color.Red;
        internal static bool SilentAimV2 = false;
        internal static float TeleportSpeed = 0.1f;
        internal static TargetingMode TargetingMode = TargetingMode.ClosestToCrosshair;
        internal static TargetingMode TargetingMode1 = TargetingMode.Target360;
        internal static TargetingMode TargetingMode2 = TargetingMode.ClosestToPlayer;
        internal static TargetingMode TargetingMode3 = TargetingMode.LowestHealth;
        internal static float PullSpeed = 0.05f;
        internal static float AimbotFOV = 90f;
        internal static bool FakeLag = false;
        internal static bool Teleport = false;
        internal static bool SpeedHacks = false;

        public static bool SilentAimEnabled = false;
        public static int SilentAimMode = 0; // 0 = 360, 1 = V1
        internal static bool Slient = false;
        internal static bool Slient2 = false;

        internal static bool TeleportNewV2 = false;
        internal static bool PullEnemiesV2 = false;
        internal static bool Ghost = false;

        internal static bool AimbotZexV2Enabled = false;
        public static float GetAimSpeedDelay()
        {
            return AimSpeed <= 0.01f ? 1000f : 1000f / AimSpeed; // tránh chia 0
        }
        internal static Keys ActivationKey = Keys.LButton; // Key to activate the aimbot
        // FOV Mouse Configuration
        internal static bool FOVMouseEnabled = false; // Whether FOV Mouse is enabled
        internal static float FOVMouseRadius = 10f;
        internal static Color ColorFOVMouse = Color.Red;
        internal static float FOVSmoothness = 20.0f; // Smoothness for FOV Mouse
        internal static float SoftnessFactor = 0.4f;
        internal static bool VisibilityCheck1 = false;
        public static KeybindState ActiveKeybind = KeybindState.None;

        public static float AimbotSmoothing = 0.2f;
        public static float AimbotRandomness = 2.5f;
        // Aimbot Mouse Configuration
        internal static bool AimMouseEnabled = false; // Whether Aimbot Mouse is enabled
        public static float AimMouseSensi = 2.0f; // Sensitivity for Aimbot Mouse
        public static float AimMouseSmooth = 33.0f; // Smoothness for Aimbot Mouse

        // Aimbot Speed Configuration
        internal static float AimSpeedLoader = 2.5f; // Default speed for aimbot aiming

        internal static bool AimbotV2Enabled = false; // Whether Aimbot V2 is enabled

        // Aimbot Zex V3 Configuration
        internal static bool AimbotZexV3Enabled = false; // Whether Aimbot Zex V3 is enabled    

        internal static bool AimbotZexLegitEnabled = false; // Whether Aimbot Zex Legit's enabled
        internal static bool AimbotNewEnabled = false; // Whether Aimbot New is enabled

        internal static float AimStrength = 1.0f; // Strength of the aimbot's pull towards the target
        internal static int AimbotTransformOffset = 0x3F0;

        internal static float FOVTapSmoothness = 3.0f;
        internal static float FOVHoldSmoothness = 12.0f;

        internal static float AimbotRadius = 5f;
        internal static int FireDelay = 0; // Delay before firing after aiming

        public static float AimbotDeadzone = 1.5f;
        internal static float VerticalBoost= 1.5f; // Boost for vertical aiming smoothness
    }

    public enum TargetingMode
    {
        ClosestToCrosshair,
        Target360,
        ClosestToPlayer,
        LowestHealth,
    }
}
