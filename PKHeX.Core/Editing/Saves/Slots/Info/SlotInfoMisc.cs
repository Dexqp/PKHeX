using System;

namespace PKHeX.Core;

/// <summary>
/// Miscellaneous origination <see cref="ISlotInfo"/>
/// </summary>
public sealed record SlotInfoMisc(byte[] Data, int Slot, int Offset, bool PartyFormat = false, bool Mutable = false) : ISlotInfo
{
    public SlotOrigin Origin => PartyFormat ? SlotOrigin.Party : SlotOrigin.Box;
    public bool CanWriteTo(SaveFile sav) => Mutable;
    public WriteBlockedMessage CanWriteTo(SaveFile sav, PKM pk) => Mutable ? WriteBlockedMessage.None : WriteBlockedMessage.InvalidDestination;
    public StorageSlotType Type { get; init; }

    public SlotInfoMisc(SaveFile sav, int slot, int offset, bool party = false) : this(GetBuffer(sav), slot, offset, party) { }

    private static byte[] GetBuffer(SaveFile sav) => sav switch
    {
        SAV4 s => s.General,
        SAV3 s3 => s3.Large,
        _ => sav.Data,
    };

    public bool WriteTo(SaveFile sav, PKM pk, PKMImportSetting setting = PKMImportSetting.UseDefault)
    {
        var span = Data.AsSpan(Offset);
        if (PartyFormat)
            sav.SetSlotFormatParty(pk, span, setting, setting);
        else
            sav.SetSlotFormatStored(pk, span, setting, setting);
        return true;
    }

    public PKM Read(SaveFile sav)
    {
        var span = Data.AsSpan(Offset);
        return PartyFormat ? sav.GetPartySlot(span) : sav.GetStoredSlot(span);
    }
}
