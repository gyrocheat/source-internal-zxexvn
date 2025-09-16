using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class AimbotZexV2
    {
        #region Fields
        private static Thread AimThread;
        private static bool _running = false;

        private static Entity _currentTarget = null;
        private static uint _originalValue;
        private static int _aimCycleCounter = 0;
        private static DateTime _aimActivationStartTime = DateTime.MinValue;

        private const int AIM_ENABLED_FRAMES = 15;
        private const int AIM_DISABLED_FRAMES = 2;
        private const int TOTAL_AIMBOT_FRAMES = AIM_ENABLED_FRAMES + AIM_DISABLED_FRAMES;

        public static Entity CurrentTarget => _currentTarget;
        #endregion

        public static void Work()
        {
            if (_running) return;
            _running = true;

            AimThread = new Thread(() =>
            {
                while (_running)
                {
                    try
                    {
                        if (!Config.AimbotZexV2Enabled || Core.Width <= 0 || Core.Height <= 0 || !Core.HaveMatrix)
                        {
                            ReleaseAndRestoreMemory();
                            Thread.Sleep(40);
                            continue;
                        }

                        _currentTarget = xZexVN();

                        bool isAimKeyPressed = (WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) != 0;
                        if (isAimKeyPressed && _currentTarget != null)
                        {
                            float dist3D = Vector3.Distance(Core.LocalMainCamera, _currentTarget.Head);

                            if (IsCrosshairNearTarget(_currentTarget, dist3D))
                            {
                                if (_aimActivationStartTime == DateTime.MinValue)
                                {
                                    _aimActivationStartTime = DateTime.Now;
                                }

                                if ((DateTime.Now - _aimActivationStartTime).TotalMilliseconds >= Config.AimbotDelayHold)
                                {
                                    _aimCycleCounter++;
                                    if (_aimCycleCounter % TOTAL_AIMBOT_FRAMES < AIM_ENABLED_FRAMES)
                                    {
                                        PerformAim();
                                    }
                                }
                            }
                            Thread.Sleep(12);
                        }
                        else
                        {
                            ReleaseAndRestoreMemory();
                            _aimActivationStartTime = DateTime.MinValue;
                            Thread.Sleep(35);
                        }

                        Thread.Sleep(Config.AIRender);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Aimbot Error: {ex.Message}");
                        ReleaseAndRestoreMemory();
                        Thread.Sleep(80);
                    }
                }
            });

            AimThread.IsBackground = true;
            AimThread.Start();
        }

        #region Core Logic Methods
        private static void PerformAim()
        {
            if (_currentTarget == null) return;
            nuint Address = _currentTarget.Address + 0x50;

            if (_originalValue == 0)
            {
                if (!InternalMemory.Read<uint>(Address, out _originalValue))
                {
                    ReleaseAndRestoreMemory();
                    return;
                }
            }

            if (InternalMemory.Read<uint>(_currentTarget.Address + 0x3F0, out var targetTransform) && targetTransform != 0)
            {
                InternalMemory.Write(Address, targetTransform);
            }
        }

        private static void ReleaseAndRestoreMemory()
        {
            if (_currentTarget != null && _originalValue != 0)
            {
                nuint Address = _currentTarget.Address + 0x50;
                InternalMemory.Write(Address, _originalValue);
            }

            _originalValue = 0;
            _aimCycleCounter = 0;
        }

        private static Entity xZexVN()
        {
            Entity bestTarget = null;
            float closestDist = float.MaxValue;
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values.ToList())
            {
                if (entity == null || entity.Address == 0 || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked))
                    continue;

                float dist3D = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (dist3D > Config.AimBotMaxDistance)
                    continue;

                var screenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (screenPos.X <= 1 || screenPos.Y <= 1 || screenPos.X >= Core.Width || screenPos.Y >= Core.Height)
                    continue;

                if (!IsTargetTrulyVisible(entity))
                    continue;

                float dist2D = Vector2.Distance(centerScreen, screenPos);
                if (dist2D < closestDist && dist2D <= Config.FOVMouseRadius)
                {
                    closestDist = dist2D;
                    bestTarget = entity;
                }
            }
            return bestTarget;
        }
        #endregion

        #region Helper Methods

        private static bool IsCrosshairNearTarget(Entity target, float dist3D)
        {
            if (target == null) return false;
            float tightnessMultiplier;

            if (dist3D > 150f)
            {
                tightnessMultiplier = 0.7f; 
            }
            else if (dist3D > 80f)
            {
                tightnessMultiplier = 0.8f;
            }
            else
            {
                tightnessMultiplier = 0.95f; 
            }

            var screenPos = W2S.WorldToScreen(Core.CameraMatrix, target.Head, Core.Width, Core.Height);
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);
            float dist2DFromCenter = Vector2.Distance(centerScreen, screenPos);

            float activationFov = Config.FOVMouseRadius * tightnessMultiplier;

            return dist2DFromCenter <= activationFov;
        }
        private static bool IsTargetTrulyVisible(Entity target)
        {
            return true;
        }
        #endregion
    }
}