using System.Collections.Generic;

namespace BetterCrewAssignment
{
    /// <summary>
    /// Represents the set of all crewable components on a ship.
    /// </summary>
    class CrewableList
    {
        private List<Crewable> allCrewables;
        private List<Crewable> commandCrewables;

        public CrewableList(ShipConstruct construct)
        {
            allCrewables = Crewable.List(construct);
            commandCrewables = new List<Crewable>();
            foreach (Crewable crewable in allCrewables)
            {
                if (crewable.IsCommand) commandCrewables.Add(crewable);
            }
        }

        public int Count { get { return allCrewables.Count; } }

        /// <summary>
        /// Determines whether the specified crew member is assigned to anything in the list.
        /// </summary>
        /// <param name="crewMember"></param>
        /// <returns></returns>
        public bool IsAssigned(ProtoCrewMember crewMember)
        {
            return Crewable.IsAssigned(allCrewables, crewMember);
        }

        /// <summary>
        /// Clears all crew assignments from the list.
        /// </summary>
        public void ClearAssignments()
        {
            foreach (Crewable crewable in allCrewables)
            {
                crewable.Clear();
            }
        }

        /// <summary>
        /// Determines whether any assignments remain to attempt.
        /// </summary>
        public bool NeedsCrew
        {
            get
            {
                foreach (Crewable crewable in allCrewables)
                {
                    if (crewable.NeedsCrew) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get all the crewables in the list.
        /// </summary>
        public IEnumerable<Crewable> All {  get { return allCrewables; } }

        /// <summary>
        /// Get all the command-pod crewables in the list.
        /// </summary>
        public IEnumerable<Crewable> Command {  get { return commandCrewables; } }
    }
}
