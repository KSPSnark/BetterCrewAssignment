using System;
using System.Collections.Generic;

namespace BetterCrewAssignment
{
    /// <summary>
    /// Encodes the overall semantics of assigning crew to slots.
    /// </summary>
    static class AssignmentLogic
    {
        private static readonly string PILOT_PROFESSION = "Pilot";
        private static readonly string ENGINEER_PROFESSION = "Engineer";
        private static readonly string SCIENTIST_PROFESSION = "Scientist";

        /// <summary>
        /// Here when we need to make a pass through the ship and assign kerbals everywhere.
        /// </summary>
        /// <param name="construct"></param>
        public static CrewableList AssignKerbals(ShipConstruct construct)
        {
            // Find what we need to crew
            CrewableList crewables = new CrewableList(construct);

            // Clear out any prior assignments
            crewables.ClearAssignments();

            int iteration = 0;
            int iterationLimit = 10;
            while (crewables.NeedsCrew && (iteration < iterationLimit))
            {
                // Fill out all slots that specify a kerbal by name. This takes
                // priority over everything else, since these represent conscious choices by the player.
                Logging.Log("<Assigning kerbals by name>");
                AssignKerbalsByName(crewables.All);
                if (!crewables.NeedsCrew) break;

                // Make sure we have SAS (assign a pilot if necessary).
                Logging.Log("<Ensuring SAS>");
                int sasLevel = TryEnsureSAS(construct, crewables);
                if (!crewables.NeedsCrew) break;

                // Fill out all command module slots that specify a kerbal by profession.
                Logging.Log("<Assigning kerbals to command modules by profession>");
                AssignKerbalsByProfession(crewables.Command, sasLevel);
                if (!crewables.NeedsCrew) break;

                // Fill all remaining slots that specify a kerbal by profession.
                Logging.Log("<Assigning kerbals to all modules by profession>");
                AssignKerbalsByProfession(crewables.All, int.MaxValue); // pretend like we have infinite SAS so it will pick low-level pilots
                if (!crewables.NeedsCrew) break;

                // Add any crew that we need to fulfill requirements of various parts.
                Logging.Log("<Fulfilling part requirements>");
                FulfillPartRequirements(crewables, construct);
                if (!crewables.NeedsCrew) break;

                // Fill all remaining command slots that want a warm body
                Logging.Log("<Assigning any kerbals to command modules>");
                AssignAnyKerbals(crewables.Command);
                if (!crewables.NeedsCrew) break;

                // And everything else
                Logging.Log("<Assigning any kerbals to all modules>");
                AssignAnyKerbals(crewables.All);
                if (!crewables.NeedsCrew) break;

                ++iteration;
            }
            if (iteration >= iterationLimit) Logging.Error("Runaway crew assignment hit iteration limit");

            // Count 'em up
            int slotCount = 0;
            int assignedCount = 0;
            foreach (Crewable crewable in crewables.All)
            {
                foreach (CrewSlot slot in crewable.Slots)
                {
                    ++slotCount;
                    if (slot.Occupant != null) ++assignedCount;
                }
            }
            int emptySlots = slotCount - assignedCount;
            Logging.Log("Crew assignment complete. Assigned " + assignedCount + " kerbals, left " + emptySlots + " slots empty.");
            return crewables;
        }

        /// <summary>
        /// Persist all assignments so that they'll be available after saving/reloading the ship.
        /// </summary>
        /// <param name="crewables"></param>
        public static void PersistKerbalAssignments(ShipConstruct construct)
        {
            CrewableList crewables = new CrewableList(construct);
            foreach (Crewable crewable in crewables.All)
            {
                crewable.PersistAssignments();
            }

            Logging.Log("Persisted assignments.");
        }

        /// <summary>
        /// Go through and attempt to assign kerbals that are requested by name for specific slots.
        /// </summary>
        /// <param name="crewables"></param>
        private static void AssignKerbalsByName(IEnumerable<Crewable> crewables)
        {
            foreach (Crewable crewable in crewables)
            {
                foreach (CrewSlot slot in crewable.Slots)
                {
                    if (!slot.NeedsOccupant) continue; // either there's already someone there, or we want it empty
                    if (slot.Assignment == null) continue; // no assignment for this slot
                    ProtoCrewMember crew = slot.Assignment.PickAvailable(AssignmentType.Name);
                    if (crew != null)
                    {
                        slot.Occupant = crew;
                        Logging.Log(crewable.ToString() + " slot " + slot.Index + ": Assigned " + Logging.ToString(crew) + " (by request)");
                    }
                }
            }
        }


        /// <summary>
        /// If the ship has no probe cores with SAS, try to make sure that there's at least one pilot.
        /// Returns the available SAS level (-1 if no SAS present).
        /// </summary>
        /// <param name="construct"></param>
        /// <param name="crewables"></param>
        private static int TryEnsureSAS(ShipConstruct construct, CrewableList crewables)
        {
            // Do we have SAS?
            int sasLevel = GetHighestSASLevel(construct);

            // get the highest-level pilot
            ProtoCrewMember highestPilot = GetHighestAssignedLevel(crewables.Command, PILOT_PROFESSION);
            int pilotLevel = (highestPilot == null) ? -1 : highestPilot.experienceLevel;

            int maxSas = (sasLevel > pilotLevel) ? sasLevel : pilotLevel;
            if (maxSas >= 0)
            {
                // we already have SAS on the ship, don't need to add a pilot
                if ((sasLevel < 0) && (pilotLevel > sasLevel))
                {
                    Logging.Log(Logging.ToString(highestPilot) + " is already assigned and will provide SAS");
                }
                return maxSas;
            }

            // There's no SAS control, we need to add a pilot somewhere.

            // Try to find a slot to put a pilot
            CrewSlot pilotSlot = Crewable.FindEmptySlot(crewables.Command);
            if (pilotSlot == null)
            {
                Logging.Warn("SAS will be unavailable (no probe cores, no open slots to add a pilot)");
                return maxSas;
            }

            // Try to find a pilot to assign.
            ProtoCrewMember lowestPilot;
            if (!FindHighestLowestAvailable(PILOT_PROFESSION, crewables, out highestPilot, out lowestPilot))
            {
                Logging.Warn("SAS will be unavailable (no probe cores, no available pilots)");
                return maxSas;
            }

            Logging.Log("Assigning " + Logging.ToString(highestPilot) + " to provide SAS");
            pilotSlot.Occupant = highestPilot;
            return highestPilot.experienceLevel;
        }


        /// <summary>
        /// Go through and attempt to fill empty slots that have requested a particular profession.
        /// </summary>
        /// <param name="crewables"></param>
        private static void AssignKerbalsByProfession(IEnumerable<Crewable> crewables, int sasLevel)
        {
            foreach (Crewable crewable in crewables)
            {
                foreach (CrewSlot slot in crewable.Slots)
                {
                    if (!slot.NeedsOccupant) continue; // either there's already someone there, or we want it empty
                    if (slot.Assignment == null) continue; // no assignment for this slot
                    KerbalChooser chooser = PilotChooser.ForSasLevel(sasLevel);
                    ProtoCrewMember crew = slot.Assignment.PickAvailable(AssignmentType.Profession, chooser);
                    if (crew != null)
                    {
                        slot.Occupant = crew;
                        Logging.Log(crewable.ToString() + " slot " + slot.Index + ": Need " + crew.trait.ToLower() + ", assigned " + Logging.ToString(crew));
                    }
                }
            }
        }

        /// <summary>
        /// As needed, assign additional crew members to fulfill part requirements.
        /// </summary>
        /// <param name="crewables"></param>
        /// <param name="construct"></param>
        private static void FulfillPartRequirements(CrewableList crewables, ShipConstruct construct)
        {
            // Find any professions that are required on the ship
            Dictionary<string, ModuleCrewRequirement> requirements = FindRequiredProfessions(construct);
            if (requirements.Count == 0) return; // there aren't any requirements

            // We can ignore any that are already provided for
            List<string> ignoreList = new List<string>();
            foreach (string requiredProfession in requirements.Keys)
            {
                if (HasProfession(crewables.All, requiredProfession)) ignoreList.Add(requiredProfession);
            }
            foreach (string ignoreProfession in ignoreList)
            {
                requirements.Remove(ignoreProfession);
            }
            if (requirements.Count == 0) return; // all requirements already taken care of

            // We now have a set of required professions that we want to have on board,
            // but haven't yet satisfied. There might not be enough slots to hold them all,
            // so build a prioritized list with the most-desired profession first.
            List<String> prioritizedRequirements = new List<string>(requirements.Keys);
            if (prioritizedRequirements.Count > 1)
            {
                // Sort our remaining requirements in descending order of importance
                Comparison<string> byPriority = (profession1, profession2) =>
                {
                    // First, sort by *declared* importance from the parts. i.e. if the
                    // Mystery Goo asks for a scientist and assigns importance 0, and the
                    // ISRU asks for an engineer and assigns importance 1, then the ISRU wins.
                    int importance1 = requirements[profession1].importance;
                    int importance2 = requirements[profession2].importance;
                    if (importance1 > importance2) return -1;
                    if (importance2 > importance1) return 1;
                    // In case of a ite (e.g. one part asks for a scientist with importance 1
                    // and the other asks for an engineer with importance 1), then pick the
                    // profession that's "better".
                    importance1 = ProfessionImportance(profession1);
                    importance2 = ProfessionImportance(profession2);
                    if (importance1 > importance2) return -1;
                    if (importance2 > importance1) return 1;
                    return 0;
                };
                prioritizedRequirements.Sort(byPriority);
            }

            // Now go through our prioritized profession requirements and try to find
            // an empty slot (and available unassigned crew member) to satisfy each one.
            foreach (string requiredProfession in prioritizedRequirements)
            {
                string part = Logging.ToString(requirements[requiredProfession].part);
                ProtoCrewMember highest;
                ProtoCrewMember lowest;
                if (FindHighestLowestAvailable(requiredProfession, crewables, out highest, out lowest))
                {
                    // Got a crew member to fulfill the requirement. In the case of pilots,
                    // we want the lowest-level possible (to leave the highest freed up to
                    // satisfy SAS requirements for other ships). But for anyone else, we
                    // want the highest available.
                    ProtoCrewMember crew = PILOT_PROFESSION.Equals(requiredProfession) ? lowest : highest;
                    // Is there a command slot available?
                    CrewSlot slot = Crewable.FindEmptySlot(crewables.Command);
                    if (slot == null)
                    {
                        // okay then, how about a non-command slot?
                        slot = Crewable.FindEmptySlot(crewables.All);
                    }
                    if (slot == null)
                    {
                        Logging.Warn("No open slot is available to assign a " + requiredProfession + " to operate " + part);
                    }
                    else
                    {
                        slot.Occupant = crew;
                        Logging.Log("Assigning " + Logging.ToString(crew) + " to operate " + part);
                    }
                }
                else
                {
                    // there's nobody to fill the slot
                    Logging.Warn("No " + requiredProfession + " is available to operate " + part + ", not assigning anyone");
                }
            } // for each required profession
        }

        /// <summary>
        /// Attempt to fill any open slots that have any requirement at all. We'll take any warm body we can get.
        /// </summary>
        /// <param name="crewables"></param>
        private static void AssignAnyKerbals(IEnumerable<Crewable> crewables)
        {
            foreach (Crewable crewable in crewables)
            {
                foreach (CrewSlot slot in crewable.Slots)
                {
                    if (!slot.NeedsOccupant) continue; // either there's already someone there, or we want it empty
                    if (slot.Assignment == null) continue; // no assignment for this slot
                    ProtoCrewMember crew = slot.Assignment.PickAvailable(AssignmentType.KerbalType, HighExperienceChooser.Instance);
                    if (crew != null)
                    {
                        slot.Occupant = crew;
                        Logging.Log(crewable.ToString() + " slot " + slot.Index + ": Assigned " + Logging.ToString(crew));
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified profession already has an assigned crewmember.
        /// </summary>
        /// <param name="crewables"></param>
        /// <param name="profession"></param>
        /// <returns></returns>
        private static bool HasProfession(IEnumerable<Crewable> crewables, string profession)
        {
            foreach (Crewable crewable in crewables)
            {
                foreach (CrewSlot slot in crewable.Slots)
                {
                    ProtoCrewMember crew = slot.Occupant;
                    if ((crew != null) && (profession.ToLower().Equals(crew.trait.ToLower())))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the highest SAS level supplied. Returns -1 if no SAS functionality is available.
        /// </summary>
        /// <param name="construct"></param>
        /// <returns></returns>
        private static int GetHighestSASLevel(ShipConstruct construct)
        {
            int highestLevel = -1;
            foreach (Part part in construct.parts)
            {
                foreach (ModuleSAS module in part.Modules.GetModules<ModuleSAS>())
                {
                    if (module.SASServiceLevel > highestLevel)
                    {
                        highestLevel = module.SASServiceLevel;
                    }
                }
            }
            return highestLevel;
        }

        /// <summary>
        /// Get the highest-level assigned crew member of the specified profession (null if none).
        /// </summary>
        /// <param name="crewables"></param>
        /// <param name="profession"></param>
        /// <returns></returns>
        private static ProtoCrewMember GetHighestAssignedLevel(IEnumerable<Crewable> crewables, string profession)
        {
            float highestExperience = float.NegativeInfinity;
            ProtoCrewMember highest = null;
            foreach (Crewable crewable in crewables)
            {
                foreach (CrewSlot slot in crewable.Slots)
                {
                    ProtoCrewMember candidate = slot.Occupant;
                    if ((candidate != null)
                        && profession.ToLower().Equals(candidate.trait.ToLower())
                        && (candidate.experience > highestExperience))
                    {
                        highest = candidate;
                        highestExperience = candidate.experience;
                    }
                }
            }
            return highest;
        }

        /// <summary>
        /// Find the highest and lowest level kerbal of the specified profession that's available for assignment.
        /// Returns true if found, false if not.
        /// </summary>
        private static bool FindHighestLowestAvailable(
            String profession,
            CrewableList alreadyAssigned,
            out ProtoCrewMember highest,
            out ProtoCrewMember lowest)
        {
            highest = null;
            lowest = null;
            foreach (ProtoCrewMember candidate in Roster.Crew)
            {
                if (IsAssignable(candidate, alreadyAssigned)
                    && profession.Equals(candidate.trait)
                    && !alreadyAssigned.IsAssigned(candidate))
                {
                    if ((highest == null) || (candidate.experience > highest.experience))
                    {
                        highest = candidate;
                    }
                    if ((lowest == null) || (candidate.experience > lowest.experience))
                    {
                        lowest = candidate;
                    }
                }
            }
            return highest != null;
        }

        /// <summary>
        /// Find all professions required by parts on the ship. Key is profession name, value is
        /// the requirement that introduced it.
        /// </summary>
        /// <param name="construct"></param>
        /// <returns></returns>
        private static Dictionary<string, ModuleCrewRequirement> FindRequiredProfessions(ShipConstruct construct)
        {
            Dictionary<string, ModuleCrewRequirement> requirements = new Dictionary<string, ModuleCrewRequirement>();
            foreach (Part part in construct.parts)
            {
                foreach (ModuleCrewRequirement requirement in part.Modules.GetModules<ModuleCrewRequirement>())
                {
                    string profession = requirement.profession;
                    if ((profession == null) || (profession.Length == 0)) continue;
                    if (requirements.ContainsKey(profession))
                    {
                        ModuleCrewRequirement previousRequirement = requirements[profession];
                        if (requirement.importance > previousRequirement.importance)
                        {
                            requirements[profession] = requirement; // it's more important, replace it
                        }
                    }
                    else
                    {
                        // first requirement for this profession, add it
                        requirements[profession] = requirement;
                    }
                }
            }
            return requirements;
        }

        /// <summary>
        /// Gets whether the kerbal is available for assignment.
        /// </summary>
        /// <param name="crew"></param>
        /// <returns></returns>
        private static bool IsAssignable(ProtoCrewMember crew, CrewableList alreadyAssigned)
        {
            return (crew != null)
                && (crew.type == ProtoCrewMember.KerbalType.Crew) // we never want to assign tourists
                && (crew.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                && !alreadyAssigned.IsAssigned(crew);
        }

        private static KerbalRoster Roster { get { return HighLogic.CurrentGame.CrewRoster; } }

        /// <summary>
        /// This is used as a tie-breaker when there are conflicting parts of equal importance
        /// that are requesting different professions to operate them. It returns an indication
        /// of how important (desirable) it is to have a kerbal of the specified profession on
        /// your ship.
        /// 
        /// This is completely arbitrary, e.g. who's to say which is more valuable, scientist or
        /// engineer?  Mainly I just needed a way to say "pilots are less valuable than everyone else."
        /// </summary>
        /// <param name="profession"></param>
        /// <returns></returns>
        private static int ProfessionImportance(string profession)
        {
            if (ENGINEER_PROFESSION.ToLower().Equals(profession.ToLower())) return 3;
            if (SCIENTIST_PROFESSION.ToLower().Equals(profession.ToLower())) return 2;
            if (PILOT_PROFESSION.ToLower().Equals(profession.ToLower())) return 1;
            return 0;
        }

    }
}
