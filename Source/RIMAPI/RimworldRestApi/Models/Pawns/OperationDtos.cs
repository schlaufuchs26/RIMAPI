using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The Health tab's operations (fox ticket #4212, gap 4): the bill that makes
    /// a doctor amputate, install a bionic or remove a scar, and the medicine
    /// policy that decides what the doctor may use while doing it.
    ///
    /// A surgery bill names a recipe and, when the recipe targets a body part, the
    /// part. A part is named the way the game labels it ("left eye"), because a
    /// def name alone cannot tell two eyes apart.
    /// </summary>
    public class OperationDto
    {
        /// <summary>The bill's own id, e.g. "Bill_InstallBionicEye_123".</summary>
        [ParameterDescription("The bill's id string, as the operation list reports it.")]
        public string Id { get; set; }

        [ParameterDescription("The recipe's def name.")]
        public string Recipe { get; set; }
        public string Label { get; set; }

        /// <summary>The part the operation works on, as the game labels it.</summary>
        [ParameterDescription("The body part the operation works on.")]
        public string BodyPart { get; set; }

        [ParameterDescription("The body part's def name.")]
        public string BodyPartDef { get; set; }

        [ParameterDescription("Whether the bill is suspended.")]
        public bool Suspended { get; set; }

        /// <summary>Whether the game can still run this bill at all.</summary>
        [ParameterDescription("Whether the game will let the operation run now.")]
        public bool Completable { get; set; }
    }

    public class PawnOperationsDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }

        /// <summary>The pawn's medicine policy, e.g. "Best".</summary>
        [ParameterDescription("The medicine care level the pawn's bills use.")]
        public string MedicineCare { get; set; }

        [ParameterDescription("One entry per operation bill on the pawn.")]
        public List<OperationDto> Operations { get; set; }
    }

    /// <summary>One surgery a pawn could be given, with the parts it can target.</summary>
    public class SurgeryRecipeDto
    {
        [ParameterDescription("The recipe's def name.")]
        public string Recipe { get; set; }
        public string Label { get; set; }
        [ParameterDescription("Whether the recipe names a body part.")]
        public bool TargetsBodyPart { get; set; }

        /// <summary>Whether the recipe is available for this pawn at all.</summary>
        [ParameterDescription("Whether the recipe can be added now.")]
        public bool Available { get; set; }

        /// <summary>Whether a colonist has the skill the recipe needs.</summary>
        [ParameterDescription("Whether the doctor's skill is enough for it.")]
        public bool SkillOk { get; set; }

        /// <summary>The parts this recipe can work on, as the game labels them.</summary>
        [ParameterDescription("The body parts the recipe can target.")]
        public List<string> BodyParts { get; set; }
    }

    public class AddOperationRequest
    {
        public int PawnId { get; set; }

        /// <summary>The surgery's recipe def name, e.g. "InstallBionicEye".</summary>
        [ParameterDescription("The recipe's def name.")]
        public string Recipe { get; set; }

        /// <summary>
        /// The part, as the game labels it ("left eye") or by def name ("Eye").
        /// Required when the recipe targets a body part.
        /// </summary>
        [ParameterDescription("The body part to operate on, by def name.")]
        public string BodyPart { get; set; }

        /// <summary>
        /// Whether the game may post its own messages about the new bill, as the
        /// dialog does; defaults to true.
        /// </summary>
        [ParameterDescription("Whether the game writes a letter about it.")]
        public bool? SendMessages { get; set; }
    }

    public class MedicinePolicyRequest
    {
        public int PawnId { get; set; }

        /// <summary>NoCare, NoMeds, HerbalOrWorse, NormalOrWorse or Best.</summary>
        [ParameterDescription("The care level: none, herbal, normal or best.")]
        public string Care { get; set; }
    }
}
