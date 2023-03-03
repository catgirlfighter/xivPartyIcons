using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace PartyNamplates.Utils
{
    public static class PartyListHUD
    {
        public static unsafe uint? GetPartySlotNumber(uint objectId)
        {
            var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
            var list = (HudPartyMember*)hud->PartyMemberList;

            if (hud->PartyMemberCount > 8)
            {
                return null;
            }

            var result = new List<uint>();
            for (var i = 0; i < hud->PartyMemberCount; i++)
            {
                if (list[i].ObjectId == objectId)
                {
                    return (uint)i + 1;
                }
            }

            return null;
        }
    }
}