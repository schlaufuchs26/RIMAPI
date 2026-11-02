using System.Collections.Generic;

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
        public string Id { get; set; }

        public string Recipe { get; set; }
        public string Label { get; set; }

        /// <summary>The part the operation works on, as the game labels it.</summary>
        public string BodyPart { get; set; }

        public string BodyPartDef { get; set; }

        public bool Suspended { get; set; }

        /// <summary>Whether the game can still run this bill at all.</summary>
        public bool Completable { get; set; }
    }

    public class PawnOperationsDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }

        /// <summary>The pawn's medicine policy, e.g. "Best".</summary>
        public string MedicineCare { get; set; }

        public List<OperationDto> Operations { get; set; }
    }

    /// <summary>One surgery a pawn could be given, with the parts it can target.</summary>
    public class SurgeryRecipeDto
    {
        public string Recipe { get; set; }
        public string Label { get; set; }
        public bool TargetsBodyPart { get; set; }

        /// <summary>Whether the recipe is available for this pawn at all.</summary>
        public bool Available { get; set; }

        /// <summary>Whether a colonist has the skill the recipe needs.</summary>
        public bool SkillOk { get; set; }

        /// <summary>The parts this recipe can work on, as the game labels them.</summary>
        public List<string> BodyParts { get; set; }
    }

    public class AddOperationRequest
    {
        public int PawnId { get; set; }

        /// <summary>The surgery's recipe def name, e.g. "InstallBionicEye".</summary>
        public string Recipe { get; set; }

        /// <summary>
        /// The part, as the game labels it ("left eye") or by def name ("Eye").
        /// Required when the recipe targets a body part.
        /// </summary>
        public string BodyPart { get; set; }

        /// <summary>
        /// Whether the game may post its own messages about the new bill, as the
        /// dialog does; defaults to true.
        /// </summary>
        public bool? SendMessages { get; set; }
    }

    public class MedicinePolicyRequest
    {
        public int PawnId { get; set; }

        /// <summary>NoCare, NoMeds, HerbalOrWorse, NormalOrWorse or Best.</summary>
        public string Care { get; set; }
    }
}
