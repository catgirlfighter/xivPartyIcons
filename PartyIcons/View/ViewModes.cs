using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartyNamplates.View
{
    [Flags]
    public enum ChatMode
    {
        GameDefault,
        NameColor,
        Role,
    }
   public enum NameplateMode
    {
        Default,
        JobIconAndName,
        JobIcon,
        JobIconAndPartySlot,
        RoleLetters,
        JobIconAndRoleLettersUncolored
    }
    public enum NameplateSizeMode
    {
        Basic,
        Large,
        Larger
    }
    public enum IconSetMode
    {
        None,
        GlowingColored,
        Framed
    }
}
