namespace PartyIcons.View
{
    using System.Numerics;
    public enum NameplateMode
    {
        Default,
        JobIconAndName,
        JobIcon,
        JobIconAndPartySlot,
        RoleLetters,
        JobIconAndRoleLettersUncolored
    }

    public readonly record struct NameplateModeConfig(float Scale, bool Offset, Vector2 Coords);
}