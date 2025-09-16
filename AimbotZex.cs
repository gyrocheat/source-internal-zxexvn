using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class AimbotZex
    {
        private static Thread AimbotLegit;
        private static bool _running = false;

        private static Entity _lockedTarget = null;
        private static uint _originalValue;

        public static Entity CurrentTarget => _lockedTarget;
        public static void Work()
        {
            if (_running) return;
            _running = true;

            AimbotLegit = new Thread(() =>
            {
                while (_running)
                {
                    try
                    {
                        if (!Config.AimbotZexEnabled || Core.Width <= 0 || Core.Height <= 0 || !Core.HaveMatrix)
                        {
                            ReleaseLockAndRestoreMemory();
                            Thread.Sleep(100);
                            continue;
                        }

                        bool isAimLegitActive = (WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) != 0;

                        if (isAimLegitActive)
                        {
                            if (!IsTargetStillValid(_lockedTarget))
                            {
                                ReleaseLockAndRestoreMemory();
                                _lockedTarget = FindTarget();
                            }

                            if (_lockedTarget != null)
                            {
                                nuint patchAddress = _lockedTarget.Address + 0x50;

                                if (_originalValue == 0)
                                {
                                    if (!InternalMemory.Read<uint>(patchAddress, out _originalValue))
                                    {
                                        ReleaseLockAndRestoreMemory();
                                        continue;
                                    }
                                }

                                if (InternalMemory.Read<uint>(_lockedTarget.Address + 0x3F0, out var LegitheadTrans) && LegitheadTrans != 0)
                                {
                                    InternalMemory.Write(patchAddress, LegitheadTrans);
                                }
                            }
                        }
                        else
                        {
                            ReleaseLockAndRestoreMemory();
                        }

                        Thread.Sleep(Config.AIRender);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Aimbot Error: {ex.Message}");
                        ReleaseLockAndRestoreMemory();
                        Thread.Sleep(100);
                    }
                }
            });

            AimbotLegit.IsBackground = true;
            AimbotLegit.Start();
        }
        private static void ReleaseLockAndRestoreMemory()
        {
            if (_lockedTarget != null && _originalValue != 0)
            {
                nuint patchAddress = _lockedTarget.Address + 0x50;
                InternalMemory.Write(patchAddress, _originalValue);
            }
            _lockedTarget = null;
            _originalValue = 0;
        }

        private static bool IsTargetStillValid(Entity target)
        {
            if (target == null || target.Address == 0 || target.IsDead || (Config.IgnoreKnocked && target.IsKnocked))
                return false;

            float dist3D = Vector3.Distance(Core.LocalMainCamera, target.Head);
            if (dist3D > Config.AimBotMaxDistance)
                return false;

            var screenPos = W2S.WorldToScreen(Core.CameraMatrix, target.Head, Core.Width, Core.Height);
            if (screenPos.X <= 1 || screenPos.Y <= 1 || screenPos.X >= Core.Width || screenPos.Y >= Core.Height)
                return false;

            if (!IsTargetTrulyVisible(target))
                return false;

            return true;
        }
        private static Entity FindTarget()
        {
            Entity bestTarget = null;
            float closestDist = float.MaxValue;
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values.ToList())
            {
                if (IsTargetStillValid(entity))
                {
                    var screenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                    float dist2D = Vector2.Distance(centerScreen, screenPos);

                    if (dist2D < closestDist && dist2D <= Config.AimBotFov)
                    {
                        closestDist = dist2D;
                        bestTarget = entity;
                    }
                }
            }

            return bestTarget;
        }

        private static bool IsTargetTrulyVisible(Entity target)
        {
            return true;
        }
    }
}
