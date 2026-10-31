using System.Collections.Generic;

namespace RIMAPI.Models
{
    // A pawn in a role: the id every write takes and the name a reader needs.
    public class RolePawnDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // One role of the colony's ideoligion (ticket #3186). The role's label is
    // the name the game generated for it ("Pujari", "Guru"), which is what the
    // alert and the mood thought say; the def says what kind of role it is
    // (leader, moral guide, specialist) and survives a reform that renames it.
    public class IdeoRoleDto
    {
        public string Def { get; set; }
        public string Label { get; set; }

        // Active is false for a role the ideoligion carries but does not use
        // (its meme is not selected): such a role is not empty, it is unused.
        public bool Active { get; set; }
        public bool LeaderRole { get; set; }

        // Multi is a role several pawns can hold at once; a single role takes
        // one pawn and Assign replaces the one who held it.
        public bool Multi { get; set; }
        public bool Empty { get; set; }

        public List<RolePawnDto> Assigned { get; set; }

        // Eligible names the colonists whose requirements the role's own check
        // meets, so an empty role comes with the candidates that could fill it.
        public List<RolePawnDto> Eligible { get; set; }
    }

    // What GET /api/v1/ideo/roles answers: the colony's ideoligion and its
    // roles with the pawns that fill them.
    public class IdeoRolesDto
    {
        public string Ideo { get; set; }
        public int ColonistCount { get; set; }
        public int EmptyCount { get; set; }
        public List<IdeoRoleDto> Roles { get; set; }
    }

    // What POST /api/v1/ideo/role/assign answers: who holds the role now, and
    // which pawn the assignment pushed out of it (a single role holds one).
    public class RoleAssignResultDto
    {
        public string Ideo { get; set; }
        public string Role { get; set; }
        public string RoleDef { get; set; }
        public int PawnId { get; set; }
        public string Pawn { get; set; }
        public int PreviousPawnId { get; set; }
        public string PreviousPawn { get; set; }
        public List<RolePawnDto> Assigned { get; set; }
    }
}
