using System;
using System.Collections.Generic;
using System.Text;

namespace BetterCrewAssignment
{
    /// <summary>
    /// Adding this module to a crewed command pod specifies how the crew slots in the
    /// pod should be assigned.
    /// 
    /// An assignment is a string, which can be any of the following:
    /// 
    /// - A specific kerbal's name (e.g. "Jebediah Kerman").
    /// - A profession (e.g. "Engineer", "Scientist").
    /// - A kerbal type (e.g. "Crew", "Tourist")
    /// - "Empty" (this means "leave the slot without any crew in it").
    /// 
    /// Multiple values can be concatenated with "|", in which case each successive
    /// one will be tried until success is found or the end of the chain is reached.
    /// If no match is found, the slot is left empty.
    ///
    /// Examples:
    /// 
    /// "Jebediah Kerman|Pilot|Crew" means "assign Jeb if he's available,
    /// otherwise assign a pilot if available, otherwise assign any crew."
    /// 
    /// "Scientist|Engineer" means "assign a scientist if available, otherwise
    /// assign an engineer if available, otherwise use the default".
    /// 
    /// "Scientist|Engineer|Empty" means "assign a scientist if available, otherwise
    /// assign an engineer if available, otherwise leave the slot empty".
    /// </summary>
    public class ModuleCrewAssignment : PartModule
    {
        /// <summary>
        /// The default assignment for all slots in the command pod.
        /// </summary>
        [KSPField]
        public string defaultAssignment;

        /// <summary>
        /// A comma-delimited list of assignments for specific slots in the command pod.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string slotAssignments;

        /// <summary>
        /// Find a ModuleCrewAssignment on the part, or null if there isn't one.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static ModuleCrewAssignment Find(Part part)
        {
            List<ModuleCrewAssignment> modules = part.Modules.GetModules<ModuleCrewAssignment>();
            return (modules.Count == 0) ? null : modules[0];
        }
    }
}
