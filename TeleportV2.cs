using AotForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal static class TeleportV2
    {
        private static Task tele;
        private static CancellationTokenSource cts = new();
        private static bool isRunning = false;

        private static Entity? targetEnemy = null;

        public static void Work()
        {
            if (isRunning) return;

            cts = new CancellationTokenSource();
            isRunning = true;

            tele = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!Config.TeleportV2)
                    {
                        targetEnemy = null;
                        await Task.Delay(1, cts.Token);
                        continue;
                    }

                    if (targetEnemy == null || targetEnemy.IsDead || (Config.IgnoreKnocked && targetEnemy.IsKnocked))
                    {
                        targetEnemy = Core.Entities.Values
                            .Where(entity => !entity.IsDead
                                             && (!Config.IgnoreKnocked || !entity.IsKnocked)
                                             && Vector3.Distance(Core.LocalMainCamera, entity.Head) <= 500)
                            .OrderBy(e => Vector3.Distance(Core.LocalMainCamera, e.Head))
                            .FirstOrDefault();
                    }
                    if (targetEnemy != null)
                    {
                        var localRootBone = InternalMemory.Read<uint>(Core.LocalPlayer + (uint)Bones.Root, out var localRootBonePtr);
                        var localTransform = InternalMemory.Read<uint>(localRootBonePtr + 0x8, out var localTransformValue);
                        var localTransformObj = InternalMemory.Read<uint>(localTransformValue + 0x8, out var localTransformObjPtr);
                        var localMatrix = InternalMemory.Read<uint>(localTransformObjPtr + 0x20, out var localMatrixValue);

                        var enemyRootBone = InternalMemory.Read<uint>(targetEnemy.Address + (uint)Bones.Root, out var enemyRootBonePtr);
                        var enemyRootPosition = Transform.GetNodePosition(enemyRootBonePtr, out var enemyRootTransform);

                        InternalMemory.Write<Vector3>(localMatrixValue + 0x80, enemyRootTransform);
                    }

                    await Task.Delay(5, cts.Token);
                }
            }, cts.Token);
        }
    }
}
