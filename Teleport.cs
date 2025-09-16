using AotForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class Teleport
    {
        private static void LogError(string methodName, Exception ex)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now} - Method: {methodName} - Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            string logPath = Path.Combine(@"C:\Windows\Temp", "teleport_error_log.txt");
            File.AppendAllText(logPath, $"[ERROR] {DateTime.Now} - Method: {methodName} - Exception: {ex.Message}\r\n{ex.StackTrace}\r\n\r\n");
        }
        internal static void Work()
        {
            Entity lockedEnemy = null;
            int retryCount = 0;
            const int MAX_RETRIES = 3;
            bool useSpineBone = false;

            while (true)
            {
                try
                {
                    if (!Config.TeleportV2)
                    {
                        lockedEnemy = null;
                        retryCount = 0;
                        useSpineBone = false;
                        Thread.Sleep(3);
                        continue;
                    }

                    try
                    {
                        if (lockedEnemy == null ||
                            lockedEnemy.IsDead ||
                            (Config.IgnoreKnocked && lockedEnemy.IsKnocked) ||
                            Vector3.Distance(Core.LocalMainCamera, lockedEnemy.Head) > 200 ||
                            string.IsNullOrEmpty(lockedEnemy.Name))
                        {
                            try
                            {
                                lockedEnemy = Core.Entities.Values
                                    .Where(entity =>
                                        entity != null &&
                                        !entity.IsDead &&
                                        (!Config.IgnoreKnocked || !entity.IsKnocked) &&
                                        Vector3.Distance(Core.LocalMainCamera, entity.Head) <= 500 &&
                                        Vector3.Distance(Core.LocalMainCamera, entity.Head) >= 1 &&
                                        !string.IsNullOrEmpty(entity.Name))
                                    .OrderBy(e => Vector3.Distance(Core.LocalMainCamera, e.Head))
                                    .FirstOrDefault();

                                retryCount = 0;
                                useSpineBone = false;
                            }
                            catch (Exception ex)
                            {
                                LogError("Work - Finding new target", ex);
                                lockedEnemy = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError("Work - Target validation", ex);
                        lockedEnemy = null;
                    }

                    if (lockedEnemy != null && !lockedEnemy.IsDead)
                    {
                        try
                        {
                            var localRootBone = InternalMemory.Read<uint>(Core.LocalPlayer + (uint)Bones.Root, out var localRootBonePtr);
                            if (localRootBonePtr == 0)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            var localTransform = InternalMemory.Read<uint>(localRootBonePtr + 0x8, out var localTransformValue);
                            if (localTransformValue == 0)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            var localTransformObj = InternalMemory.Read<uint>(localTransformValue + 0x8, out var localTransformObjPtr);
                            if (localTransformObjPtr == 0)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            var localMatrix = InternalMemory.Read<uint>(localTransformObjPtr + 0x20, out var localMatrixValue);
                            if (localMatrixValue == 0)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            if (lockedEnemy == null || lockedEnemy.Address == 0)
                            {
                                retryCount = 0;
                                useSpineBone = false;
                                Thread.Sleep(5);
                                continue;
                            }

                            uint enemyBoneOffset = useSpineBone ? (uint)Bones.Spine : (uint)Bones.Root;
                            uint enemyBonePtr = 0;

                            var enemyBone = InternalMemory.Read<uint>(lockedEnemy.Address + enemyBoneOffset, out enemyBonePtr);

                            if (enemyBonePtr == 0)
                            {
                                if (!useSpineBone)
                                {
                                    useSpineBone = true;
                                    continue;
                                }

                                retryCount++;
                                if (retryCount >= MAX_RETRIES)
                                {
                                    lockedEnemy = null;
                                    retryCount = 0;
                                    useSpineBone = false;
                                }

                                Thread.Sleep(10);
                                continue;
                            }

                            retryCount = 0;
                            var enemyPosition = Transform.GetNodePosition(enemyBonePtr, out var enemyTransform);

                            if (float.IsNaN(enemyTransform.X) || float.IsNaN(enemyTransform.Y) || float.IsNaN(enemyTransform.Z) ||
                                float.IsInfinity(enemyTransform.X) || float.IsInfinity(enemyTransform.Y) || float.IsInfinity(enemyTransform.Z))
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            InternalMemory.Write<Vector3>(localMatrixValue + 0x80, enemyTransform);
                        }
                        catch (Exception ex)
                        {
                            LogError("Work - Teleport operation", ex);
                            Thread.Sleep(10);
                        }
                    }

                    Thread.Sleep(2);
                }
                catch (Exception ex)
                {
                    LogError("Work - Main loop", ex);
                    Thread.Sleep(500);
                }
            }
        }
    }
}
