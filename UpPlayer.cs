using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AotForms
{
    internal static class UpPlayer
    {
        internal static void Work()
        {
            while (true)
            {
                if (!Config.UpPlayer)
                {
                    Thread.Sleep(1);
                    continue;
                }
                if (!Config.Aimfovc)
                {
                   
                    continue;
                }

                if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                {
                    Thread.Sleep(1);
                    continue;
                }
                foreach (var entity in Core.Entities.Values)
                {
                    if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;


                    var playerDistance = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                    if (playerDistance > Config.AimBotMaxDistance) continue;

                    var EntityHeadBone = InternalMemory.Read<uint>(entity.Address + (uint)Bones.Head, out var EntityheadBone);
                    var transform = InternalMemory.Read<uint>(EntityheadBone + 0x8, out var transformValue);
                    var transformObj = InternalMemory.Read<uint>(transformValue + 0x8, out var headBoneclass);
                    var matrix = InternalMemory.Read<uint>(headBoneclass + 0x20, out var headmatrixValuelist);


                    var HeadTrans = Transform.GetNodePosition(EntityheadBone, out var headTransform);

                    InternalMemory.Write<Vector3>(headmatrixValuelist + 0x80, headTransform + new Vector3(0, 0.1f, 0));

                    Thread.Sleep((int)Config.test);
                }


            }
        }
    }
}
