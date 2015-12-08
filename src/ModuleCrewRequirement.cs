using System;
using System.Collections.Generic;
using System.Text;

namespace BetterCrewAssignment
{
    /// <summary>
    /// Placing this module on a part causes it to request that the vehicle it's on be crewed
    /// rather than uncrewed.
    /// </summary>
    public class ModuleCrewRequirement : PartModule
    {
        /// <summary>
        /// Indicates the profession that the part is requesting there be at least 1 of
        /// on the ship. Allowable values include literal profession names (e.g. "Scientist"),
        /// kerbal types (e.g. "Tourist"), or null (meaning "any kerbal is fine").
        /// </summary>
        [KSPField]
        public String profession = null;

        /// <summary>
        /// Indicates the relative importance of this requirement. This is used for resolving
        /// conflicts if different parts are requesting different professions and there aren't
        /// enough crew slots to go round.
        /// </summary>
        [KSPField]
        public int importance = 0;

        /// <summary>
        /// Gets the crew requirements of the specified part, if any.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static List<ModuleCrewRequirement> CrewRequirementsOf(Part part)
        {
            return part.Modules.GetModules<ModuleCrewRequirement>();
        }

        public override string ToString()
        {
            return new StringBuilder()
                .Append((profession == null) ? "Any" : profession)
                .Append("-")
                .Append(importance)
                .ToString();
        }
    }
}
