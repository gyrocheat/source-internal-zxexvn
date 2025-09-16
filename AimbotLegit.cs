using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class AimbotLegit
    {
        private static Thread AimbotThread;
        private static bool _isRunning = false;
        private static Entity _currentTarget = null;
        private static uint _originalAimTargetValue;

        private static readonly Random _random = new Random();

        private static DateTime _keyHeldStartTime = DateTime.MinValue;
        private static bool _isAimKeyHeld = false;
        public static Entity CurrentTarget => _currentTarget;
        internal static void Work()
        {
            if (_isRunning) return;

            _isRunning = true;
            AimbotThread = new Thread(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (!Config.AimbotZexLegitEnabled || !Core.HaveMatrix)
                        {
                            ReleaseAimAndRestore();
                            Thread.Sleep(200);
                            continue;
                        }

                        bool isAimKeyPressedNow = (WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) != 0;

                        if (isAimKeyPressedNow)
                        {
                            if (!_isAimKeyHeld)
                            {
                                _keyHeldStartTime = DateTime.Now;
                            }
                            float effectiveDelay = Math.Max(Config.AimbotDelayHold, 30);

                            if ((DateTime.Now - _keyHeldStartTime).TotalMilliseconds >= effectiveDelay)
                            {
                                if (_currentTarget == null || _currentTarget.IsDead)
                                {
                                    FindAndSetNewTarget();
                                }

                                if (_currentTarget != null && !_currentTarget.IsDead)
                                {
                                    PerformSmoothedAim();
                                }
                            }
                        }
                        else
                        {
                            ReleaseAimAndRestore();
                        }

                        _isAimKeyHeld = isAimKeyPressedNow;
                        Thread.Sleep(Config.AIRender > 0 ? Config.AIRender : 1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LỖI] AimbotV2: {ex.Message}");
                        ReleaseAimAndRestore();
                        Thread.Sleep(100);
                    }
                }
            });
            AimbotThread.IsBackground = true;
            AimbotThread.Start();
        }
        private static void FindAndSetNewTarget()
        {
            _currentTarget = xZexVN();
            if (_currentTarget != null)
            {
                _originalAimTargetValue = 0;
            }
        }
        private static void PerformSmoothedAim()
        {
            if (_currentTarget == null || _currentTarget.IsDead) return;

            if (!IsCrosshairNearTarget(_currentTarget))
            {
                ReleaseAimAndRestore();
                return;
            }

            Vector2 screenPos = W2S.WorldToScreen(Core.CameraMatrix, _currentTarget.Head, Core.Width, Core.Height);
            Vector2 centerScreen = new Vector2(Core.Width / 2f, Core.Height / 2f);
            float distance = Vector2.Distance(centerScreen, screenPos);

            float smoothnessFactor = (distance / Config.AimBotFov) * (1 - Config.Smoothness);

            if (_random.NextDouble() < smoothnessFactor)
            {
                return;
            }

            SetAimTargetTransform();
        }

        private static void SetAimTargetTransform()
        {
            nuint aimTargetAddress = _currentTarget.Address + 0x50;
            nuint sourceTransformAddress = _currentTarget.Address + 0x3F0;

            if (_originalAimTargetValue == 0)
            {
                if (!InternalMemory.Read<uint>(aimTargetAddress, out _originalAimTargetValue))
                {
                    _originalAimTargetValue = 0;
                    return;
                }
            }

            if (InternalMemory.Read<uint>(sourceTransformAddress, out uint visiblePlayerTransform) && visiblePlayerTransform != 0)
            {
                InternalMemory.Write(aimTargetAddress, visiblePlayerTransform);
            }
        }

        private static void ReleaseAimAndRestore()
        {
            if (_currentTarget != null && _originalAimTargetValue != 0)
            {
                nuint aimTargetAddress = _currentTarget.Address + 0x50;
                InternalMemory.Write(aimTargetAddress, _originalAimTargetValue);
            }

            _currentTarget = null;
            _originalAimTargetValue = 0;
        }

        private static Entity xZexVN()
        {
            Entity bestTarget = null;
            float closestDist = float.MaxValue;
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values.ToList())
            {
                if (entity == null || entity.Address == 0 || entity.IsDead) continue;
                if (Config.IgnoreKnocked && entity.IsKnocked) continue;

                var screenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (screenPos.X <= 1 || screenPos.Y <= 1 || screenPos.X >= Core.Width || screenPos.Y >= Core.Height) continue;

                float dist2D = Vector2.Distance(centerScreen, screenPos);
                if (dist2D > Config.AimBotFov) continue;

                if (dist2D < closestDist)
                {
                    closestDist = dist2D;
                    bestTarget = entity;
                }
            }
            return bestTarget;
        }


        private static bool IsCrosshairNearTarget(Entity target)
        {
            if (target == null) return false;
            var screenPos = W2S.WorldToScreen(Core.CameraMatrix, target.Head, Core.Width, Core.Height);
            if (screenPos.X <= 0 || screenPos.Y <= 0) return false;
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);
            return Vector2.Distance(centerScreen, screenPos) <= Config.AimBotFov;
        }
    }
}
