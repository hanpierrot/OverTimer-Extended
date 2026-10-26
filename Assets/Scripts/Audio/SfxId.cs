/// <summary>
/// Every sound effect the game can ask the AudioService for. Append-only: the AudioCatalog asset stores the numbers, so
/// never reorder, rename or delete a member. An id without a clip in the catalog is simply silent.
/// </summary>
public enum SfxId
{
    Click = 0,              // pressing on a desk object
    ClickRelease = 1,
    MoneyGainSmall = 2,     // MoneyService.OnIncome, tiered by the amount (AudioCatalogSO.moneySmallMax / moneyMediumMax)
    MoneyGainMedium = 3,
    MoneyGainLarge = 4,
    CardPackBuy = 5,
    CardPackOpen = 6,       // the player clicks the pack in the reveal panel
    CardFlip = 7,           // a card of the pack is revealed
    CardPlace = 8,          // a card is put on the desk
    CardCollected = 9,      // a card type is collected for the first time
    BlackjackDeal = 10,
    BlackjackHit = 11,
    PawnSell = 12,
    WirePlug = 13,
    UiToggle = 14,          // the Laptop / Phone view opens or closes
    UiError = 15,           // a refused action (hint toast), no clip yet
    Tease = 16,             // "almost there" hints (Charlie, Mother Lode, cursed ticket)
    WinStinger = 17,
    LoseStinger = 18,       // no clip yet
    BilliardsStart = 19,    // a round starts (difficulty chosen, bet paid)
    BilliardsShot = 20,     // the cue strikes
    BilliardsPocket = 21,
    ParcelSlide = 22,       // a parcel is delivered to the door tower
    CoinRain = 23,          // the coin rain of a mechanic win
}
