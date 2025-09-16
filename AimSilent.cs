using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AotForms;

namespace Client
{
    internal static class AimSilent
    {


        internal static void Work()
        {
            while (true)
            {
                if (!Config.Aimfovc) continue;
                if (!Config.enableAimBot)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if ((WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }


                Entity target = GetTarget(Config.TargetingMode);


                if (target != null)
                {

                    var playerLook = MathUtils.GetRotationToLocation(target.Head, 0.1f, Core.LocalMainCamera);

                    if (Config.SilentAimV2)
                    {

                        var Unknown = InternalMemory.Read<bool>(Core.LocalPlayer + Offsets.sAim1, out var unknown);

                        if (unknown)
                        {
                            var Unknown1 = InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.sAim2, out var unknown1);
                            if (unknown1 != 0)
                            {
                                InternalMemory.Read<Vector3>(unknown1 + Offsets.sAim3, out var Pos);
                                InternalMemory.Write<Vector3>(unknown1 + Offsets.sAim4, target.Head - Pos);

                            }
                        }

                    }

                    else if (Config.AimBotRage)
                    {

                        InternalMemory.Write(Core.LocalPlayer + Offsets.AimRotation, playerLook);

                    }

                    if (Config.SilentAim)
                    {
                        Thread.Sleep(0);
                    }
                    else
                    {
                        Thread.Sleep((int)Config.AimBotSmooth);
                    }
                }
            }
        }

        static Entity GetTarget(TargetingMode mode)
        {
            if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix) return null;

            switch (mode)
            {
                case TargetingMode.ClosestToCrosshair:
                    return GetTargetByClosestToCrosshair();
                case TargetingMode.Target360:
                    return GetTarget360();
                case TargetingMode.LowestHealth:
                    return GetTargetByLowestHealth();
                case TargetingMode.ClosestToPlayer:
                    return GetTargetByClosestToPlayer();
                default:
                    return GetTargetByClosestToCrosshair();
            }
        }
        static Entity GetTargetByClosestToCrosshair()
        {
            Entity target = null;
            float distance = float.MaxValue;
            var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var target2D = W2S.WorldToScreen(Core.CameraMatrix, targetPosition, Core.Width, Core.Height);
                if (target2D.X < 1 || target2D.Y < 1) continue;

                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;

                var x = target2D.X - screenCenter.X;
                var y = target2D.Y - screenCenter.Y;
                var crosshairDist = (float)Math.Sqrt(x * x + y * y);

                if (crosshairDist >= distance || crosshairDist > Config.AimbotFOV) continue;

                distance = crosshairDist;
                target = entity;
            }

            return target;
        }


        static Entity GetTargetByLowestHealth()
        {
            Entity target = null;
            float lowestHealth = float.MaxValue;

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var target2D = W2S.WorldToScreen(Core.CameraMatrix, targetPosition, Core.Width, Core.Height);
                if (target2D.X < 1 || target2D.Y < 1) continue;

                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;


                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
                var x = target2D.X - screenCenter.X;
                var y = target2D.Y - screenCenter.Y;
                var crosshairDist = (float)Math.Sqrt(x * x + y * y);
                if (crosshairDist > Config.AimbotFOV) continue;


                if (entity.Health < lowestHealth)
                {
                    lowestHealth = entity.Health;
                    target = entity;
                }
            }

            return target;
        }


        static Entity GetTargetByClosestToPlayer()
        {
            Entity target = null;
            float closestDistance = float.MaxValue;

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var target2D = W2S.WorldToScreen(Core.CameraMatrix, targetPosition, Core.Width, Core.Height);
                if (target2D.X < 1 || target2D.Y < 1) continue;


                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;


                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
                var x = target2D.X - screenCenter.X;
                var y = target2D.Y - screenCenter.Y;
                var crosshairDist = (float)Math.Sqrt(x * x + y * y);
                if (crosshairDist > Config.AimbotFOV) continue;


                if (playerDistance < closestDistance)
                {
                    closestDistance = playerDistance;
                    target = entity;
                }
            }

            return target;
        }
        static Entity GetTarget360()
        {
            Entity target = null;
            float closestDistance = float.MaxValue;

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;

                if (playerDistance < closestDistance)
                {
                    closestDistance = playerDistance;
                    target = entity;
                }
            }

            return target;
        }


    }
}
