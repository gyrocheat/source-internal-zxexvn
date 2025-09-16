using AotForms;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal static class PullPlayer
    {
        private static Task upPlayerTask;
        private static CancellationTokenSource cts = new();
        private static bool isRunning = false;
        internal static void Work()
        {
            if (isRunning) return;
            isRunning = true;

            upPlayerTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!Config.PullEnemies)
                    {
                        await Task.Delay(5, cts.Token);
                        continue;
                    }

                    foreach (var entity in Core.Entities.Values)
                    {
                        if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                        var playerDistance = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                        if (playerDistance > Config.distancepull) continue;

                        // Đọc vị trí gốc (Root) của mình
                        var localRootBone = InternalMemory.Read<uint>(Core.LocalPlayer + (uint)Bones.Root, out var localRootBonePtr);
                        var localTransform = InternalMemory.Read<uint>(localRootBonePtr + 0x8, out var localTransformValue);
                        var localTransformObj = InternalMemory.Read<uint>(localTransformValue + 0x8, out var localTransformObjPtr);
                        var localMatrix = InternalMemory.Read<uint>(localTransformObjPtr + 0x20, out var localMatrixValue);

                        var localRootPosition = Transform.GetNodePosition(localRootBonePtr, out var localRootTransform);

                        // Thêm offset để địch đứng lệch phía trên, không bị giật và không chồng vào bạn
                        Vector3 offset = new Vector3(Config.TeleOffsetX, Config.TeleOffsetY, Config.TeleOffsetZ);
                        Vector3 newEnemyPosition = localRootTransform + offset;

                        // Ghi vị trí mới cho enemy
                        var enemyRootBone = InternalMemory.Read<uint>(entity.Address + (uint)Bones.Root, out var enemyRootBonePtr);
                        var enemyTransform = InternalMemory.Read<uint>(enemyRootBonePtr + 0x8, out var enemyTransformValue);
                        var enemyTransformObj = InternalMemory.Read<uint>(enemyTransformValue + 0x8, out var enemyTransformObjPtr);
                        var enemyMatrix = InternalMemory.Read<uint>(enemyTransformObjPtr + 0x20, out var enemyMatrixValue);

                        InternalMemory.Write<Vector3>(enemyMatrixValue + 0x80, newEnemyPosition);
                    }

                    await Task.Delay(5, cts.Token);
                }
            }, cts.Token);
        }

        internal static void Stop()
        {
            if (!isRunning) return;

            cts.Cancel();
            isRunning = false;
        }
    }
}