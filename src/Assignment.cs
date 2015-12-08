using System.Collections.Generic;
using System.Text;

namespace BetterCrewAssignment
{
    /// <summary>
    /// Represents the semantics for deciding how to assign a kerbal to a slot in a crewable part.
    /// Perhaps a better (though longer) name would be "AssignmentPreference".
    /// 
    /// An Assignment is a chain of AssignmentNodes.  A single AssignmentNode indicates a
    /// simple preference (e.g. "pick this kerbal by name", "pick any kerbal of such-and-such profession",
    /// "leave empty", etc. The Assignment uses a chain of AssignmentNodes to provide if-else logic:
    /// it will try to assign a kerbal who matches the first node in the chain, but if nobody is available
    /// who meets the criteria, it will move on to the next node in the chain and try that.
    /// 
    /// This allows modeling semantics such as, for example, "Assign Bob Kerman if available, any
    /// scientist if Bob's not there, or any crew member if no scientist is available."
    /// </summary>
    class Assignment
    {
        public static readonly Assignment Empty = createEmptyAssignment();

        private readonly List<AssignmentNode> nodes;

        private Assignment(List<AssignmentNode> nodes)
        {
            this.nodes = nodes;
            TruncateAfterEmpty(nodes);
            CollapseDuplicates(nodes);
        }

        /// <summary>
        /// Get slot assignments for a part. The returned array will have as many elements as there are crew
        /// slots for the part. If a slot has no assignment, it will be null.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static Assignment[] GetSlotAssignments(Part part)
        {
            Assignment[] slots = new Assignment[part.CrewCapacity];
            if (slots.Length == 0) return slots;
            for (int index = 0; index < slots.Length; ++index)
            {
                slots[index] = null;
            }
            ModuleCrewAssignment module = ModuleCrewAssignment.Find(part);
            if (module != null)
            {
                Assignment defaultAssignment = Assignment.Parse(module.defaultAssignment);
                List<Assignment> assignments = ParseList(module.slotAssignments);
                for (int index = 0; index < slots.Length; ++index)
                {
                    Assignment slotAssignment = (index < assignments.Count) ? assignments[index] : null;
                    slots[index] = Append(slotAssignment, defaultAssignment);
                }
            }
            return slots;
        }

        /// <summary>
        /// Parse an assignment from a text string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Assignment Parse(string text)
        {
            List<AssignmentNode> nodes = new List<AssignmentNode>();
            if (text != null)
            {
                text = text.Trim();
                string[] tokens = text.Split('|');
                foreach (string token in tokens)
                {
                    AssignmentNode node = AssignmentNode.Parse(token);
                    if (node == null) break;
                    nodes.Add(node);
                    if (node.Type == AssignmentType.Empty) break;
                }
            }
            return (nodes.Count == 0) ? null : new Assignment(nodes);
        }

        /// <summary>
        /// Parse a comma-delimited list of assignments from a text string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<Assignment> ParseList(string text)
        {
            List<Assignment> assignments = new List<Assignment>();
            if (text != null)
            {
                string[] tokens = text.Split(',');
                foreach (string token in tokens)
                {
                    Assignment assignment = Parse(token);
                    assignments.Add(assignment);
                }
            }
            return assignments;
        }

        /// <summary>
        /// Get an assignment designed to match a given crew member.
        /// </summary>
        /// <param name="crew"></param>
        /// <returns></returns>
        public static Assignment ForCrew(ProtoCrewMember crew)
        {
            if (crew == null) return Empty;
            List<AssignmentNode> nodes = new List<AssignmentNode>(3);
            nodes.Add(AssignmentNode.ByName(crew));
            nodes.Add(AssignmentNode.ByProfession(crew));
            nodes.Add(AssignmentNode.ByType(crew));
            return new Assignment(nodes);
        }

        /// <summary>
        /// Pick an available, unassigned kerbal matching the assignment, or null if none is available.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="chooser"></param>
        /// <returns></returns>
        public ProtoCrewMember PickAvailable(AssignmentType type, KerbalChooser chooser = null)
        {
            while ((Head != null) && (Head.Type == type))
            {
                ProtoCrewMember crew = Pop().PickAvailable(chooser);
                if (crew != null) return crew;
            }
            return null;
        }

        /// <summary>
        /// Gets whether this assignment is requesting that the slot be left empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return (Head != null) && (Head.Type == AssignmentType.Empty); }
        }

        public override string ToString()
        {
            if (nodes.Count == 0) return string.Empty;
            if (nodes[0].Type == AssignmentType.Empty) return nodes[0].ToString();
            StringBuilder builder = new StringBuilder(nodes[0].ToString());
            for (int index = 1; index < nodes.Count; ++index)
            {
                AssignmentNode node = nodes[index];
                builder.Append("|").Append(node.ToString());
                if (node.Type == AssignmentType.Empty) break;
            }
            return builder.ToString();
        }

        public static string ToString(List<Assignment> assignments)
        {
            if (assignments.Count == 0) return string.Empty;
            if (assignments[0] == null) return string.Empty;
            StringBuilder builder = new StringBuilder(assignments[0].ToString());
            for (int index = 1; index < assignments.Count; ++index)
            {
                builder.Append(",");
                if (assignments[index] != null)
                {
                    builder.Append(assignments[index].ToString());
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Gets the head of the node list (i.e. the first-choice for assigning), or null if there's none.
        /// </summary>
        private AssignmentNode Head
        {
            get { return (nodes.Count > 0) ? nodes[0] : null; }
        }

        /// <summary>
        /// Pops off the head of the assignment node list and returns it (null if none left).
        /// </summary>
        /// <returns></returns>
        private AssignmentNode Pop()
        {
            if (nodes.Count == 0) return null;
            AssignmentNode node = nodes[0];
            if (node.Type != AssignmentType.Empty) nodes.RemoveAt(0);
            return node;
        }

        /// <summary>
        /// Make a new assignment by appending two.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private static Assignment Append(Assignment assignment, Assignment suffix)
        {
            if ((assignment == null) && (suffix == null)) return null;
            List<AssignmentNode> combined = new List<AssignmentNode>();
            if (assignment != null) combined.AddRange(assignment.nodes);
            if (suffix != null) combined.AddRange(suffix.nodes);
            return new Assignment(combined);
        }

        /// <summary>
        /// Remove any trailing nodes that follow an "Empty" node, because they will
        /// never be reached (since "leave this empty" is always available).
        /// </summary>
        /// <param name="nodes"></param>
        private static void TruncateAfterEmpty(List<AssignmentNode> nodes)
        {
            for (int index = 0; index < nodes.Count - 1; ++index)
            {
                if (nodes[index].Type == AssignmentType.Empty)
                {
                    nodes.RemoveRange(index + 1, nodes.Count - index - 1);
                }
            }
        }

        /// <summary>
        /// Collapse an assignment's chain to its simplest representation by combining
        /// successive duplicates (e.g. there's no point in having one "Scientist" node
        /// after another "Scientist" node).
        /// </summary>
        /// <param name="nodes"></param>
        private static void CollapseDuplicates(List<AssignmentNode> nodes)
        {
            while (true)
            {
                for (int index = 1; index < nodes.Count; ++index)
                {
                    if (nodes[index-1].Equals(nodes[index]))
                    {
                        nodes.RemoveAt(index);
                        continue;
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Create an assignment that requests that a slot be left empty.
        /// </summary>
        /// <returns></returns>
        private static Assignment createEmptyAssignment()
        {
            List<AssignmentNode> nodes = new List<AssignmentNode>();
            nodes.Add(AssignmentNode.Empty);
            return new Assignment(nodes);
        }
    }
}
