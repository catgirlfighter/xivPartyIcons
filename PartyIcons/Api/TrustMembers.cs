using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIconsEx.Api
{
    [StructLayout(LayoutKind.Explicit, Size = 1792)]
    public struct TrustMembers
    {
        [FieldOffset(0)]
        public AddonPartyList.PartyListMemberStruct Trust0;

        [FieldOffset(256)]
        public AddonPartyList.PartyListMemberStruct Trust1;

        [FieldOffset(512)]
        public AddonPartyList.PartyListMemberStruct Trust2;

        [FieldOffset(768)]
        public AddonPartyList.PartyListMemberStruct Trust3;

        [FieldOffset(1024)]
        public AddonPartyList.PartyListMemberStruct Trust4;

        [FieldOffset(1280)]
        public AddonPartyList.PartyListMemberStruct Trust5;

        [FieldOffset(1536)]
        public AddonPartyList.PartyListMemberStruct Trust6;

        public AddonPartyList.PartyListMemberStruct this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return Trust0;
                    case 1:
                        return Trust1;
                    case 2:
                        return Trust2;
                    case 3:
                        return Trust3;
                    case 4:
                        return Trust4;
                    case 5:
                        return Trust5;
                    case 6:
                        return Trust6;
                    default:
                        throw new IndexOutOfRangeException("Index should be in range of 0-6");
                }
            }
        }
    }
}
