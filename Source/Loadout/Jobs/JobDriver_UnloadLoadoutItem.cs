using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.ExtendedLoadout;

public class JobDriver_UnloadLoadoutItem : JobDriver
{
    private const TargetIndex ItemIndex = TargetIndex.A;
    private const TargetIndex StoreCellIndex = TargetIndex.B;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(
            job.GetTarget(StoreCellIndex),
            job,
            1,
            -1,
            null,
            errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        Toil goToStoreCell = Toils_Goto.GotoCell(StoreCellIndex, PathEndMode.Touch);
        goToStoreCell.FailOn(() => !InventoryContainsTarget());
        yield return goToStoreCell;

        Toil moveToCarryTracker = ToilMaker.MakeToil("MoveLoadoutItemToCarryTracker");
        moveToCarryTracker.initAction = delegate
        {
            Thing? item = job.GetTarget(ItemIndex).Thing;
            ThingOwner<Thing>? inventory = pawn.inventory?.innerContainer;
            if (item == null || inventory == null || !inventory.Contains(item))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int count = Math.Min(job.count, item.stackCount);
            if (count <= 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int transferredCount = inventory.TryTransferToContainer(
                item,
                pawn.carryTracker.innerContainer,
                count,
                out Thing transferred);
            if (transferredCount <= 0 || transferred == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            job.count = transferredCount;
            job.SetTarget(ItemIndex, transferred);
            transferred.SetForbidden(false, false);
        };
        moveToCarryTracker.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return moveToCarryTracker;

        Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StoreCellIndex);
        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(StoreCellIndex, carryToCell, true);
    }

    private bool InventoryContainsTarget()
    {
        Thing? item = job.GetTarget(ItemIndex).Thing;
        return item != null && (pawn.inventory?.innerContainer.Contains(item) ?? false);
    }
}
