using System;
using System.Collections.Generic;

namespace BetterCrewAssignment
{
    /// <summary>
    /// Represents the assignment of crew for a particular slot on a particular crewable part.
    /// </summary>
    class CrewSlot
    {
        /// <summary>
        /// Gets the desired assignment for this slot.
        /// </summary>
        public readonly Assignment Assignment;

        private readonly PartCrewManifest manifest;
        private readonly int slotIndex;

        private CrewSlot(Assignment assignment, PartCrewManifest manifest, int index)
        {
            this.Assignment = assignment;
            this.manifest = manifest;
            this.slotIndex = index;
        }

        /// <summary>
        /// Gets the crew slots for the specified part & manifest.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static List<CrewSlot> GetSlots(Part part, PartCrewManifest manifest)
        {
            Assignment[] assignments = Assignment.GetSlotAssignments(part);
            int capacity = manifest.GetPartCrew().Length;
            if (assignments.Length != capacity)
            {
                throw new Exception("Mismatched capacity when making slots");
            }
            List<CrewSlot> slots = new List<CrewSlot>(capacity);
            for (int index = 0; index < capacity; ++index)
            {
                slots.Add(new CrewSlot(assignments[index], manifest, index));
            }
            return slots;
        }

        /// <summary>
        /// Gets or sets the occupant of the slot.
        /// </summary>
        public ProtoCrewMember Occupant
        {
            get { return manifest.GetPartCrew()[slotIndex]; }
            set { manifest.AddCrewToSeat(value, slotIndex); }
        }

        /// <summary>
        /// Gets the index of this slot within its crewable.
        /// </summary>
        public int Index { get { return slotIndex; } }

        /// <summary>
        /// Indicates whether the slot has any assignment available.
        /// </summary>
        public bool NeedsOccupant
        {
            get
            {
                return (Occupant == null)
                    && (Assignment != null)
                    && !Assignment.IsEmpty;
            }
        }
    }
}
