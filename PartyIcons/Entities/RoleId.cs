namespace PartyNamplates.Entities
{
    public enum RoleId
    {
        Undefined,
        MT, OT, T3, T4, T5, T6, T7, T8,
        H1, H2, H3, H4, H5, H6, H7, H8,
        M1, M2, M3, M4, M5, M6, M7, M8,
        R1, R2, R3, R4, R5, R6, R7, R8,

    }

    public static class RoleIdUtils
    {
        public const int ROLE_COUNT = 8;
        public const int ROLE_TANK = 0;
        public const int ROLE_HEALER = 1;
        public const int ROLE_MELEE = 2;
        public const int ROLE_RANGED = 3;
        public static RoleId Counterpart(RoleId roleId)
        {
            return roleId == 0 ? 0 : ((int)roleId % 2) == 0 ? roleId - 1 : roleId + 1;
        }
    }
}
