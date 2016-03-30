using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterCrewAssignment
{
    /// <summary>
    /// This is the main class that drives the mod.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorBehaviour : MonoBehaviour
    {
        public void Start()
        {
            Logging.Log("Start");
            GameEvents.onEditorLoad.Add(OnShipLoaded);
            GameEvents.onEditorShipModified.Add(OnShipModified);
            GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
            CrewPanelMonitor.Started += OnEnterCrewPanel;
            CrewPanelMonitor.Stopped += OnExitCrewPanel;
            CrewPanelMonitor.CrewChanged += OnCrewChanged;
        }

        /// <summary>
        /// Here when a ship is loaded in the editor.
        /// </summary>
        /// <param name="construct"></param>
        /// <param name="loadType"></param>
        private void OnShipLoaded(ShipConstruct construct, CraftBrowserDialog.LoadType loadType)
        {
            try
            {
                Logging.Log("Ship loaded, " + construct.Count + " parts. ");
                LogPreferredAssignments(construct);
                AssignmentLogic.AssignKerbals(construct);
                LogVesselManifest();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        /// <summary>
        /// Here when the ship is modified.
        /// </summary>
        /// <param name="construct"></param>
        private void OnShipModified(ShipConstruct construct)
        {
            try
            {
                if (Crewable.CanList(construct))
                {
                    Logging.Log("Ship modified, " + construct.Count + " parts.");
                    AssignmentLogic.AssignKerbals(construct);
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        /// <summary>
        /// Here when an event happens to the part in the editor.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="part"></param>
        private void OnEditorPartEvent(ConstructionEventType eventType, Part part)
        {
            try
            {
                if (eventType != ConstructionEventType.PartAttached) return;
                if (Crewable.CanList(CurrentShipConstruct))
                {
                    Logging.Log("Attached " + Logging.ToString(part) + ".");
                    AssignmentLogic.AssignKerbals(CurrentShipConstruct);
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        /// <summary>
        /// Here when entering the crew panel.
        /// </summary>
        /// <param name="construct"></param>
        private void OnEnterCrewPanel(ShipConstruct construct)
        {
            try
            {
                Logging.Log("Entering crew screen, persisting kerbal assignments.");
                AssignmentLogic.PersistKerbalAssignments(CurrentShipConstruct);
                LogVesselManifest();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        /// <summary>
        /// Here when exiting the crew panel.
        /// </summary>
        /// <param name="construct"></param>
        private void OnExitCrewPanel(ShipConstruct construct)
        {
            try
            {
                Logging.Log("Leaving crew screen, persisting kerbal assignments.");
                AssignmentLogic.PersistKerbalAssignments(CurrentShipConstruct);
                LogVesselManifest();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        /// <summary>
        /// Here when the player manually changes the crew assignments in the editor.
        /// </summary>
        /// <param name="construct"></param>
        private void OnCrewChanged(ShipConstruct construct)
        {
            if (Crewable.CanList(construct))
            {
                Logging.Log("Crew edited, persisting kerbal assignments.");
                AssignmentLogic.PersistKerbalAssignments(construct);
                LogVesselManifest();
            }
        }

        private static void LogVesselManifest()
        {
            VesselCrewManifest vesselManifest = ShipConstruction.ShipManifest;
            if (vesselManifest == null)
            {
                Logging.Log("Vessel manifest is unavailable");
            }
            else
            {
                List<PartCrewManifest> partManifests = vesselManifest.GetCrewableParts();
                foreach (PartCrewManifest partManifest in partManifests)
                {
                    ProtoCrewMember[] crewMembers = partManifest.GetPartCrew();
                    for (int index = 0; index < crewMembers.Length; ++index)
                    {
                        Logging.Log("Crew assigned to " + partManifest.PartInfo.name + " slot " + index + ": " + Logging.ToString(crewMembers[index]));
                    }
                }
            }
        }

        /// <summary>
        /// Record all preferred crew assignments for the ship.
        /// </summary>
        /// <param name="construct"></param>
        private void LogPreferredAssignments(ShipConstruct construct)
        {
            foreach (Part part in construct.Parts)
            {
                Assignment[] assignments = Assignment.GetSlotAssignments(part);
                for (int index = 0; index < assignments.Length; ++index)
                {
                    String message = Logging.ToString(part) + " slot " + index + ": ";
                    Assignment assignment = assignments[index];
                    if (assignment == null)
                    {
                        message += "unassigned";
                    }
                    else
                    {
                        message += "assign " + assignment;
                    }
                    Logging.Log(message);
                } // for each crew slot on the part
                foreach (ModuleCrewRequirement requirement in ModuleCrewRequirement.CrewRequirementsOf(part))
                {
                    Logging.Log(Logging.ToString(part) + ": require " + requirement);
                }
            } // for each part on the ship
        }

        private ShipConstruct CurrentShipConstruct
        {
            get
            {
                return (EditorLogic.fetch == null) ? null : EditorLogic.fetch.ship;
            }
        }
    }
}

/*
Some miscellaneous notes to myself.

Crew-related members on Part:
public int CrewCapacity;
public bool crewTransferAvailable;
public List<ProtoCrewMember> protoModuleCrew;
public bool AddCrewmember(ProtoCrewMember crew);
public bool AddCrewmemberAt(ProtoCrewMember crew, int seatIndex);
public void DespawnAllCrew();
public void RegisterCrew();
public void RemoveCrewmember(ProtoCrewMember crew);
public void SpawnCrew();
public void UnregisterCrew();

Interesting classes:
PartCrewManifest
VesselCrewManifest
ShipConstruction
ProtoVessel

The only reference to VesselCrewManifest I could find:
http://forum.kerbalspaceprogram.com/threads/69240-NullReferenceException-Having-a-lot-of-trouble-here?highlight=VesselCrewManifest

For checking available kerbals: I think I use HighLogic.CurrentGame.Roster
*/
