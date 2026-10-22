using UnityEngine;

public static class HintCatalog
{
    public static string Get(HintId id)
    {
        switch (id)
        {
            case HintId.LaptopNoPower: return "The laptop has no power. Check the wiring.";
            case HintId.CatBlocking: return "The cat is in the way. Pick it up and move it.";
            case HintId.NotEnoughMoney: return "Not enough money.";
            case HintId.MeterMinFeed: return "The Meter needs at least ${0}.";
            case HintId.MeterLocked: return "The Meter is locked.";
            case HintId.TooManyTickets: return "Too many tickets on the desk. Cash one out first.";
            case HintId.HandFull: return "Your hand is full ({0}). Pawn a card to make room.";
            case HintId.NoRoomToPlace: return "No room to place it. Pawn a card first.";
            case HintId.NotEnoughTime: return "Not enough time to play. You need {0} s on the clock.";
            case HintId.TaxCannotAfford: return "Not enough money to pay. Ignoring costs clock time.";
            case HintId.ClickCooling: return "Still cooling down.";
            case HintId.PowerBankRecharging: return "Recharging...";
            case HintId.CannotPawn: return "This can't be pawned.";
            case HintId.LaptopIconDoubleClick: return "Double-click an icon to open it.";
            case HintId.MiningOpen: return "Drag the pickaxe into the block and back out to swing.";
            case HintId.MiningCloseLosesOre: return "Closing now loses your unconfirmed ore. Finish the vein to bank it.";
            case HintId.BlackjackOpen: return "Betting costs clock time, wins pay money.";
            case HintId.BlackjackLeaveMidHand: return "Leaving now forfeits your ante and hand.";
            case HintId.WireOpen: return "Drag each plug to the socket of the same colour.";
            case HintId.PhoneFirstRing: return "The phone is ringing! Click it. The clock runs faster until it stops.";
            case HintId.MailTrayFirstBill: return "A tax bill arrived. Pay it in the Taxes app or click the mail tray.";
            case HintId.CatFirstBlock: return "Drag the cat to move it.";
            case HintId.ShopPurchased: return "Your item is on the desk.";
            case HintId.PackageArrived: return "A package is at the door. Click the door.";
            case HintId.ParcelOpen: return "Click the box to open it.";
            case HintId.JunkFlyer: return "{0}";
            case HintId.PhoneFirstOpen: return "Open an app: Call, Inbox, Taxes or Shop.";
            case HintId.PausedCall: return "This caller pauses the clock and locks the Laptop until you hang up.";
            case HintId.ShopFirstOpen: return "Add items to the cart, pick shipping, then Buy.";
            case HintId.LicenseBought: return "The license is in your Inbox. Press Activate to unlock the app.";
            case HintId.DebuffDismissCost: return "Not enough money to dismiss it. Pawning an active debuff costs ${0}.";
            case HintId.FirstPawn: return "The Printer pays for what you drop on it. A pawned item is gone.";
            case HintId.MotherLodeTease: return "The ore is singing. One more full vein.";
            case HintId.TicketCursed: return "Something is wrong with this ticket. Cash Out now.";
            case HintId.TicketCashOut: return "You can Cash Out any time. Windows you haven't scratched pay nothing.";
            case HintId.CharlieTease: return "99% of gamblers quit right before the big win.";
            default: return "";
        }
    }
}
