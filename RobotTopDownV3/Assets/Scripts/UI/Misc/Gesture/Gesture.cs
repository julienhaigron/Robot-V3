using System;

public readonly struct Gesture
{
    public enum Kind { Buy, Sell, Equip, Swap, Remove, TakeBack, Deposit }
    public enum Refusal { None, Silent, TooExpensive, Damaged, DamagedSubComponent, OccupantLocked, TournamentLocked, NoFreeSlot, Recycling, WrongType, SlotOccupied, UsedToday }

    public readonly GestureContainer target;
    public readonly Kind kind;
    public readonly Refusal refusal;

    public Gesture ( GestureContainer _target, Kind _kind, Refusal _refusal )
    {
        target = _target;
        kind = _kind;
        refusal = _refusal;
    }

    public static Gesture Refused ( Refusal _refusal ) => new(null, default, _refusal);

    public bool IsValid => refusal == Refusal.None && target != null;
    public bool IsSilent => refusal == Refusal.Silent;
    public bool IsTransaction => kind == Kind.Buy || kind == Kind.Sell;

    public Tuple<CurrencyType, ulong> GetPrice ( GestureDisplay _display )
    {
        return _display != null ? _display.GetGesturePrice(kind) : null;
    }

    public long GetSignedAmount ( GestureDisplay _display )
    {
        Tuple<CurrencyType, ulong> price = GetPrice(_display);
        return price == null ? 0 : kind == Kind.Buy ? -(long)price.Item2 : (long)price.Item2;
    }

    public string GetLabel ( GestureDisplay _display )
    {
        LocalizationManager localization = LocalizationManager.Instance;
        if (refusal != Refusal.None)
            return localization.Get(KeyOf(refusal));

        Tuple<CurrencyType, ulong> price = GetPrice(_display);
        return string.Format(localization.Get(KeyOf(kind, price != null)), price == null ? 0ul : price.Item2);
    }

    private static LocalizationKey KeyOf ( Kind _kind, bool _hasPrice )
    {
        return _kind switch
        {
            Kind.Buy => LocalizationKey.component_gesture_buy,
            Kind.Sell => LocalizationKey.component_gesture_sell,
            Kind.Equip => LocalizationKey.component_gesture_equip,
            Kind.Swap => LocalizationKey.component_gesture_swap,
            Kind.Remove => LocalizationKey.component_gesture_remove,
            Kind.TakeBack => LocalizationKey.component_gesture_take_back,
            _ => _hasPrice ? LocalizationKey.component_gesture_deposit : LocalizationKey.component_gesture_drop_in
        };
    }

    private static LocalizationKey KeyOf ( Refusal _refusal )
    {
        return _refusal switch
        {
            Refusal.TooExpensive => LocalizationKey.component_refusal_too_expensive,
            Refusal.Damaged => LocalizationKey.component_refusal_damaged,
            Refusal.DamagedSubComponent => LocalizationKey.component_refusal_damaged_sub_component,
            Refusal.OccupantLocked => LocalizationKey.component_refusal_occupant_locked,
            Refusal.TournamentLocked => LocalizationKey.component_refusal_tournament_locked,
            Refusal.NoFreeSlot => LocalizationKey.component_refusal_no_free_slot,
            Refusal.Recycling => LocalizationKey.component_refusal_recycling,
            Refusal.SlotOccupied => LocalizationKey.component_refusal_slot_occupied,
            Refusal.UsedToday => LocalizationKey.component_refusal_used_today,
            _ => LocalizationKey.component_refusal_wrong_type
        };
    }
}
