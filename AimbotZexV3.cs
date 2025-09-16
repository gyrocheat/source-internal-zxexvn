using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class AimbotZexV3
    {
        private static Thread MouseThead;
        private static bool Started = false;
        private static Entity _currentTarget = null;
        private static uint AimTargetOriginalValue;

        private static DateTime KeyHeldTimer = DateTime.MinValue;
        private static bool IsKeyHeld = false;
        private static readonly Random random = new Random();
        private static Vector2 AimSway = Vector2.Zero;
        private static DateTime LastTargetLost = DateTime.MinValue;
        private static bool IsCooldown = false;
        private static int ConsecutiveHits = 0;

        private static float MIN_DELAY = 20f;
        private static float MAX_DELAY = 150f;
        private static float AIM_SWAY = 1.5f;
        private static int COOLDOWN_DURATION = 500;
        private static int MAX_CONSECUTIVE_HITS = 5;
        private static float OVERSHOOT_CHANCE = 0.15f;

        public static Entity CurrentTarget => _currentTarget;

        public static void Work()
        {
            if (Started) return;
            Started = true;

            MouseThead = new Thread(() =>
            {
                while (Started)
                {
                    try
                    {
                        if (!Config.AimbotZexV3Enabled || Core.Width <= 0 || Core.Height <= 0 || !Core.HaveMatrix)
                        {
                            ReleaseAimAndRestore();
                            Thread.Sleep(100);
                            continue;
                        }

                        bool isAimKeyPressedNow = (WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) != 0;

                        if (isAimKeyPressedNow)
                        {
                            HandleAimingLogic();
                        }
                        else
                        {
                            if (IsKeyHeld)
                            {
                                ReleaseAimAndRestore();
                            }
                        }

                        IsKeyHeld = isAimKeyPressedNow;
                        Thread.Sleep(Config.AIRender);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in AimbotZexV3: {ex.Message}");
                        ReleaseAimAndRestore();
                        Thread.Sleep(100);
                    }
                }
            });
            MouseThead.IsBackground = true;
            MouseThead.Start();
        }

        private static void HandleAimingLogic()
        {
            if (!IsKeyHeld)
            {
                KeyHeldTimer = DateTime.Now;
                if (Config.DelayMode)
                {
                    int randomDelay = random.Next((int)MIN_DELAY, (int)MAX_DELAY);
                    KeyHeldTimer = KeyHeldTimer.AddMilliseconds(randomDelay);
                }
            }
            double elapsedMilliseconds = (DateTime.Now - KeyHeldTimer).TotalMilliseconds;
            bool isReactionTimeOver = Config.DelayMode ? DateTime.Now >= KeyHeldTimer : elapsedMilliseconds >= Config.AimbotDelayHold;

            if (!isReactionTimeOver)
            {
                return;
            }
            if (IsCooldown)
            {
                if ((DateTime.Now - LastTargetLost).TotalMilliseconds >= COOLDOWN_DURATION)
                {
                    return;
                }
                IsCooldown = false;
            }

            if (_currentTarget == null || _currentTarget.IsDead)
            {
                _currentTarget = xZexVN();
                if (_currentTarget == null)
                {
                    UpdateAimSway();
                    ConsecutiveHits = 0;
                    AimTargetOriginalValue = 0;
                }
            }

            if (_currentTarget != null && !_currentTarget.IsDead)
            {
                if (IsCrosshairNearTarget(_currentTarget))
                {
                    PerformAiming();
                }
                else
                {
                    ReleaseAimAndRestore();
                }
            }
        }

        private static void PerformAiming()
        {
            Vector2 screenPos = W2S.WorldToScreen(Core.CameraMatrix, _currentTarget.Head, Core.Width, Core.Height);
            Vector2 centerScreen = new Vector2(Core.Width / 2f, Core.Height / 2f);
            float distanceToTarget = Vector2.Distance(centerScreen, screenPos);
            if (distanceToTarget < (Config.AimBotFov * 0.1f))
            {
                return;
            }

            if (ConsecutiveHits >= MAX_CONSECUTIVE_HITS)
            {
                if (random.NextDouble() < 0.5)
                {
                    ConsecutiveHits = 0;
                    return;
                }
            }
            float dynamicSmoothness = (float)Config.Smoothness * (distanceToTarget / Config.AimBotFov);
            if (new Random().Next(0, 100) > dynamicSmoothness)
            {
                if (SetAimTargetTransform())
                {
                    ConsecutiveHits++;
                }
            }
        }

        private static bool SetAimTargetTransform()
        {
            if (_currentTarget == null || _currentTarget.IsDead) return false;
            if (random.NextDouble() < OVERSHOOT_CHANCE)
            {
                return false;
            }

            nuint Address = _currentTarget.Address + 0x50;
            nuint targetTransformAddress = _currentTarget.Address + 0x3F0;

            if (AimTargetOriginalValue == 0)
            {
                if (!InternalMemory.Read<uint>(Address, out AimTargetOriginalValue))
                {
                    AimTargetOriginalValue = 0;
                    return false;
                }
            }

            if (InternalMemory.Read<uint>(targetTransformAddress, out uint targetTransform) && targetTransform != 0)
            {
                InternalMemory.Write(Address, targetTransform);
                return true;
            }
            return false;
        }

        private static void UpdateAimSway()
        {
            AimSway = new Vector2(
                (float)(random.NextDouble() - 0.5f) * AIM_SWAY,
                (float)(random.NextDouble() - 0.5f) * AIM_SWAY
            );
        }

        private static void ReleaseAimAndRestore()
        {
            if (_currentTarget != null && AimTargetOriginalValue != 0)
            {
                nuint Address = _currentTarget.Address + 0x50;
                InternalMemory.Write(Address, AimTargetOriginalValue);
                LastTargetLost = DateTime.Now;
                IsCooldown = true;
            }
            _currentTarget = null;
            AimTargetOriginalValue = 0;
        }

        private static bool IsCrosshairNearTarget(Entity target)
        {
            if (target == null) return false;
            var screenPos = W2S.WorldToScreen(Core.CameraMatrix, target.Head, Core.Width, Core.Height);
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);
            return Vector2.Distance(centerScreen, screenPos) <= Config.AimBotFov;
        }

        private static Entity xZexVN()
        {
            Entity bestTarget = null;
            float closestScore = float.MaxValue;
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values.ToList())
            {
                if (!IsEntityValid(entity)) continue;
                if (!IsTargetTrulyVisible(entity)) continue;

                var screenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (screenPos.X <= 1 || screenPos.Y <= 1 || screenPos.X >= Core.Width || screenPos.Y >= Core.Height) continue;

                float dist2D = Vector2.Distance(centerScreen, screenPos);
                if (dist2D > Config.AimBotFov) continue;

                float dist3D = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (dist3D > Config.AimBotMaxDistance) continue;

                float score = dist2D * 0.7f + dist3D * 0.3f;

                if (score < closestScore)
                {
                    closestScore = score;
                    bestTarget = entity;
                }
            }
            return bestTarget;
        }

        private static bool IsEntityValid(Entity entity)
        {
            if (entity == null || entity.Address == 0 || entity.IsDead) return false;
            if (Config.IgnoreKnocked && entity.IsKnocked) return false;
            return true;
        }

        private static bool IsTargetTrulyVisible(Entity target)
        {
            return true;
        }
    }
}