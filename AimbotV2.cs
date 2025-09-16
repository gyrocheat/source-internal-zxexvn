using AotForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal static class AimbotV2
    {
        #region Fields
        private static Thread AimThread;
        private static bool _running = false;

        private static readonly Dictionary<nuint, uint> _originalValues = new();
        private static readonly object _lock = new();

        private static readonly Dictionary<nuint, DateTime> _lastPatchedAt = new();

        private static readonly Random _random = new();

        private static bool _isMemoryPatched = false;

        private static DateTime _keyHeldStartTime = DateTime.MinValue;
        private static bool _keyHeld = false;

        public static Entity CurrentTarget { get; private set; } = null;

        private static int _readFailureCount = 0;
        private const int MaxReadFailuresLongRange = 4;
        private const int MaxReadFailuresMidRange = 10;
        private const int MaxReadFailuresShortRange = 7;

        private const int MinPatchCooldownMs = 40;

        private static Thread _patchThread = null;
        private static volatile bool _patchThreadRunning = false;
        private static readonly Dictionary<nuint, uint> _patchedValues = new();

        private static bool _prevKeyDown = false;
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
                        CurrentTarget = null;
                        AimbotAIRender();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AIMBOT ERROR] {ex.Message}");
                        RestoreAllMemory();
                        Thread.Sleep(100);
                    }
                }
            })
            {
                IsBackground = true
            };
            AimThread.Start();
        }

        #region Core Logic Methods

        private static void AimbotAIRender()
        {
            if (!Config.AimbotV2Enabled || Core.Width <= 0 || Core.Height <= 0 || !Core.HaveMatrix)
            {
                Thread.Sleep(250);
                return;
            }

            bool isKeyDown = (WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) != 0;
            bool justPressed = isKeyDown && !_prevKeyDown;
            _prevKeyDown = isKeyDown;

            if (isKeyDown)
            {
                if (justPressed || Config.AimbotDelayHold <= 0)
                {
                    _keyHeldStartTime = DateTime.Now;
                    _keyHeld = true;

                    if (CurrentTarget != null && !IsEntityStateValid(CurrentTarget))
                    {
                        CurrentTarget = null;
                        StopPatch();
                    }

                    CurrentTarget = AIRender();
                    if (CurrentTarget != null)
                    {
                        _readFailureCount = 0;

                        nuint patchAddress = CurrentTarget.Address + 0x50;
                        if (InternalMemory.Read<uint>(CurrentTarget.Address + 0x3F0, out var Target_AI_Transform_Head) && Target_AI_Transform_Head != 0)
                        {
                            StartPatch(patchAddress, Target_AI_Transform_Head);
                        }
                        else
                        {
                            _readFailureCount++;

                            float dist3D = Vector3.Distance(Core.LocalMainCamera, CurrentTarget.Head);
                            int currentFailureThreshold;

                            if (dist3D > 150f)
                            {
                                currentFailureThreshold = MaxReadFailuresLongRange;
                            }
                            else if (dist3D > 80f)
                            {
                                currentFailureThreshold = MaxReadFailuresMidRange;
                            }
                            else
                            {
                                currentFailureThreshold = MaxReadFailuresShortRange;
                            }

                            if (_readFailureCount > currentFailureThreshold)
                            {
                                CurrentTarget = null;
                                _readFailureCount = 0;
                                StopPatch();
                            }
                        }
                    }
                    else
                    {
                        StopPatch();
                    }
                }
                else
                {
                    if (!_keyHeld)
                    {
                        _keyHeldStartTime = DateTime.Now;
                        _keyHeld = true;
                    }

                    if ((DateTime.Now - _keyHeldStartTime).TotalMilliseconds >= Config.AimbotDelayHold)
                    {
                        if (CurrentTarget != null && !IsEntityStateValid(CurrentTarget))
                        {
                            CurrentTarget = null;
                            StopPatch();
                        }

                        if (CurrentTarget == null)
                        {
                            CurrentTarget = AIRender();
                            if (CurrentTarget != null)
                            {
                                _readFailureCount = 0;
                            }
                        }

                        if (CurrentTarget != null)
                        {
                            nuint patchAddress = CurrentTarget.Address + 0x50;
                            if (InternalMemory.Read<uint>(CurrentTarget.Address + 0x3F0, out var Target_AI_Transform_Head) && Target_AI_Transform_Head != 0)
                            {
                                StartPatch(patchAddress, Target_AI_Transform_Head);
                                _readFailureCount = 0;
                            }
                            else
                            {
                                _readFailureCount++;

                                float dist3D = Vector3.Distance(Core.LocalMainCamera, CurrentTarget.Head);
                                int currentFailureThreshold;

                                if (dist3D > 150f)
                                {
                                    currentFailureThreshold = MaxReadFailuresLongRange;
                                }
                                else if (dist3D > 80f)
                                {
                                    currentFailureThreshold = MaxReadFailuresMidRange;
                                }
                                else
                                {
                                    currentFailureThreshold = MaxReadFailuresShortRange;
                                }

                                if (_readFailureCount > currentFailureThreshold)
                                {
                                    CurrentTarget = null;
                                    _readFailureCount = 0;
                                    StopPatch();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (_keyHeld)
                {
                    _keyHeld = false;
                    _keyHeldStartTime = DateTime.MinValue;
                    CurrentTarget = null;
                    if (_isMemoryPatched)
                    {
                        StopPatch();
                    }
                }
            }

            if (_keyHeld)
            {
                Thread.Sleep(8);
            }
            else
            {
                Thread.Sleep(Math.Max(1, Config.AIRender + _random.Next(-5, 5)));
            }
        }

        private static Entity AIRender()
        {
            Entity bestTarget = null;
            float closestDist = float.MaxValue;
            Vector2 centerScreen = new(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values)
            {
                if (!IsTargetValid(entity)) continue;

                var screenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);

                float dist3D = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (dist3D > Config.AimBotMaxDistance) continue;

                float dist2D = Vector2.Distance(centerScreen, screenPos);
                if (dist2D <= Config.AimBotFov && dist2D < closestDist)
                {
                    closestDist = dist2D;
                    bestTarget = entity;
                }
            }
            return bestTarget;
        }

        private static bool IsTargetValid(Entity target)
        {
            if (target == null || target.Address == 0 || target.IsDead || (Config.IgnoreKnocked && target.IsKnocked))
            {
                return false;
            }

            const float closeRangeBypass = 30f;
            float dist3D = Vector3.Distance(Core.LocalMainCamera, target.Head);

            if (dist3D < closeRangeBypass)
            {
                return true;
            }

            return IsTargetVisible(target);
        }

        private static bool IsEntityStateValid(Entity target)
        {
            return target != null &&
                   target.Address != 0 &&
                   !target.IsDead &&
                   !(Config.IgnoreKnocked && target.IsKnocked);
        }

        private static bool IsTargetVisible(Entity target)
        {
            var screenPos = W2S.WorldToScreen(Core.CameraMatrix, target.Head, Core.Width, Core.Height);
            return IsOnScreen(screenPos);
        }

        private static bool IsOnScreen(Vector2 screenPos)
        {
            return screenPos.X > 1 && screenPos.Y > 1 && screenPos.X < Core.Width && screenPos.Y < Core.Height;
        }

        #endregion

        #region Memory Management

        private static void StartPatch(nuint address, uint newValue)
        {
            try
            {
                lock (_lock)
                {
                    if (!_originalValues.ContainsKey(address))
                    {
                        if (!InternalMemory.Read<uint>(address, out var orig))
                        {
                            return;
                        }
                        _originalValues[address] = orig;
                    }

                    _patchedValues[address] = newValue;
                    _isMemoryPatched = _patchedValues.Count > 0;
                    _lastPatchedAt[address] = DateTime.Now;

                    if (_patchThread == null || !_patchThreadRunning)
                    {
                        _patchThreadRunning = true;
                        _patchThread = new Thread(PatchThreadLoop)
                        {
                            IsBackground = true
                        };
                        _patchThread.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartPatch ERROR] {ex.Message}");
            }
        }

        private static void PatchThreadLoop()
        {
            try
            {
                while (_patchThreadRunning)
                {
                    try
                    {
                        KeyValuePair<nuint, uint>[] snapshot;
                        lock (_lock)
                        {
                            snapshot = _patchedValues.ToArray();
                        }

                        foreach (var No1Aimbot in snapshot)
                        {
                            try
                            {
                                InternalMemory.Write(No1Aimbot.Key, No1Aimbot.Value);
                                lock (_lock)
                                {
                                    _lastPatchedAt[No1Aimbot.Key] = DateTime.Now;
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PatchThreadLoop ERROR] {ex.Message}");
                    }

                    Thread.Sleep(MinPatchCooldownMs);
                }
            }
            catch (ThreadInterruptedException)
            {

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PatchThreadLoop FATAL] {ex.Message}");
            }
        }

        private static void StopPatch()
        {
            try
            {
                lock (_lock)
                {
                    _patchThreadRunning = false;
                }

                if (_patchThread != null)
                {
                    try
                    {
                        if (!_patchThread.Join(200))
                        {
                            _patchThread.Interrupt();
                        }
                    }
                    catch { }

                    _patchThread = null;
                }

                lock (_lock)
                {
                    foreach (var kv in _originalValues.ToList())
                    {
                        try
                        {
                            InternalMemory.Write(kv.Key, kv.Value);
                        }
                        catch
                        {

                        }
                    }

                    _originalValues.Clear();
                    _patchedValues.Clear();
                    _isMemoryPatched = false;
                    _lastPatchedAt.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StopPatch ERROR] {ex.Message}");
            }
        }
        private static void RestoreAllMemory()
        {
            StopPatch();
        }
        #endregion
    }
}