/// A pawnable catalog item that can be bought back. OnPawned() (IPawnable) deactivates it and releases what it holds;
/// OnRestored() runs when the buyback parcel is delivered, BEFORE the object is shown again, and puts it back in a
/// clean state (cooldowns and progress reset). Purchases such as licenses are not touched: they live in Ownership.
public interface IRestorable
{
    void OnRestored();
}
