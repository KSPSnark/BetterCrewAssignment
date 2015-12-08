using System;
using System.Collections.Generic;

namespace BetterCrewAssignment
{
    /// <summary>
    /// The simplest, most atomic unit of an assignment.  A chain of these nodes
    /// comprise an assignment. See detailed discussion in the Assignment class
    /// for how this gets used.
    /// </summary>
    class AssignmentNode
    {
        private static readonly string[] PROFESSIONS = { "Pilot", "Scientist", "Engineer" };

        /// <summary>
        /// Kerbal types that can be auto-assigned to slots.
        /// </summary>
        private static readonly ProtoCrewMember.KerbalType[] KERBAL_TYPES =
        {
            ProtoCrewMember.KerbalType.Crew,
            ProtoCrewMember.KerbalType.Tourist
        };

        /// <summary>
        /// The node which requests that a slot be left empty.
        /// </summary>
        public static readonly AssignmentNode Empty = new AssignmentNode(AssignmentType.Empty, null);

        /// <summary>
        /// The type of assignment.
        /// </summary>
        public readonly AssignmentType Type;

        /// <summary>
        /// The value of the assignment: a kerbal name, a profession, a kerbal type, or (in the case of "empty") null.
        /// </summary>
        private readonly string value;

        /// <summary>
        /// Parses an Assignment from a text representation. Returns null for no assignment
        /// (i.e. slot should be empty).
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static AssignmentNode Parse(String text)
        {
            if (text == null) return null;
            text = text.Trim();
            if (text.Length == 0) return null;
            if (AssignmentType.Empty.ToString().ToLower().Equals(text.ToLower())) return Empty;
            string kerbalType = KerbalTypeOf(text);
            if (kerbalType != null) return new AssignmentNode(AssignmentType.KerbalType, kerbalType);
            string profession = ProfessionOf(text);
            if (profession != null) return new AssignmentNode(AssignmentType.Profession, profession);
            return new AssignmentNode(AssignmentType.Name, text);
        }

        public static AssignmentNode ByName(ProtoCrewMember crew)
        {
            return new AssignmentNode(AssignmentType.Name, crew.name);
        }

        public static AssignmentNode ByProfession(ProtoCrewMember crew)
        {
            return new AssignmentNode(AssignmentType.Profession, crew.trait);
        }

        public static AssignmentNode ByType(ProtoCrewMember crew)
        {
            return new AssignmentNode(AssignmentType.KerbalType, crew.type.ToString());
        }

        /// <summary>
        /// Pick an available, unassigned kerbal matching the assignment, or null if none is available.
        /// </summary>
        /// <returns></returns>
        public ProtoCrewMember PickAvailable(KerbalChooser chooser = null)
        {
            bool isTourist = (Type == AssignmentType.KerbalType) && (ProtoCrewMember.KerbalType.Tourist.ToString().Equals(value));
            IEnumerable<ProtoCrewMember> kerbals = isTourist ? Roster.Tourist : Roster.Crew;
            switch (Type)
            {
                case AssignmentType.Name:
                    return PickByName(value, kerbals);
                case AssignmentType.Profession:
                    return PickByProfession(value, kerbals, chooser);
                case AssignmentType.KerbalType:
                    return PickAny(kerbals, chooser);
                case AssignmentType.Empty:
                default:
                    return null;
            }
        }

        public override string ToString()
        {
            if (Type == AssignmentType.Empty) return AssignmentType.Empty.ToString();
            return (value == null) ? string.Empty : value;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj)) return true;
            AssignmentNode other = obj as AssignmentNode;
            if (other == null) return false;
            return (Type == other.Type) && (value == other.value);
        }

        public override int GetHashCode()
        {
            int hashCode = Type.GetHashCode();
            if (value != null) hashCode ^= value.GetHashCode();
            return hashCode;
        }

        private AssignmentNode(AssignmentType type, string value)
        {
            this.Type = type;
            this.value = value;
        }

        /// <summary>
        /// Gets a kerbal type from the string, or null if it isn't a valid kerbal type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string KerbalTypeOf(string value)
        {
            if (value == null) return null;
            foreach (ProtoCrewMember.KerbalType kerbalType in KERBAL_TYPES) {
                if (kerbalType.ToString().ToLower().Equals(value.ToLower()))
                {
                    return kerbalType.ToString();
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a profession from the string, or null if it isn't a profession.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string ProfessionOf(string value)
        {
            if (value == null) return null;
            foreach (string profession in PROFESSIONS)
            {
                if (value.ToLower().Equals(profession))
                {
                    return profession;
                }
            }
            foreach (ProtoCrewMember crew in Roster.Crew)
            {
                if (value.ToLower().Equals(crew.trait.ToLower()))
                {
                    return crew.trait;
                }
            }
            return null;
        }

        private static KerbalRoster Roster { get { return HighLogic.CurrentGame.CrewRoster; } }

        /// <summary>
        /// Pick the first available kerbal whose name matches.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="kerbals"></param>
        /// <returns></returns>
        private static ProtoCrewMember PickByName(String name, IEnumerable<ProtoCrewMember> kerbals)
        {
            foreach (ProtoCrewMember kerbal in kerbals)
            {
                if (kerbal.name.Equals(name) && IsAvailableAndUnassigned(kerbal)) return kerbal;
            }
            return null;
        }

        /// <summary>
        /// Pick the first available kerbal whose profession matches.
        /// </summary>
        /// <param name="profession"></param>
        /// <param name="kerbals"></param>
        /// <returns></returns>
        private static ProtoCrewMember PickByProfession(String profession, IEnumerable<ProtoCrewMember> kerbals, KerbalChooser chooser)
        {
            ProtoCrewMember best = null;
            foreach (ProtoCrewMember kerbal in kerbals)
            {
                if (kerbal.trait.Equals(profession) && IsAvailableAndUnassigned(kerbal))
                {
                    if (chooser == null) return kerbal;
                    best = chooser.Choose(best, kerbal);
                }
            }
            return best;
        }

        /// <summary>
        /// Pick the first available kerbal.
        /// </summary>
        /// <param name="kerbals"></param>
        /// <returns></returns>
        private static ProtoCrewMember PickAny(IEnumerable<ProtoCrewMember> kerbals, KerbalChooser chooser)
        {
            ProtoCrewMember best = null;
            foreach (ProtoCrewMember kerbal in kerbals)
            {
                if (IsAvailableAndUnassigned(kerbal))
                {
                    if (chooser == null) return kerbal;
                    best = chooser.Choose(best, kerbal);
                }
            }
            return best;
        }

        private static bool IsAvailableAndUnassigned(ProtoCrewMember kerbal)
        {
            return (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                && (ShipConstruction.ShipManifest != null)
                && (!ShipConstruction.ShipManifest.Contains(kerbal));
        }
    }
}
