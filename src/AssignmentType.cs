namespace BetterCrewAssignment
{
    /// <summary>
    /// Indicates the type of assignment for a slot.
    /// </summary>
    enum AssignmentType
    {
        /// <summary>
        /// Assign by name.
        /// </summary>
        Name,

        /// <summary>
        /// Assign by profession (e.g. Scientist, Engineer).
        /// </summary>
        Profession,

        /// <summary>
        /// Assign by kerbal type (e.g. Crew, Tourist).
        /// </summary>
        KerbalType,

        /// <summary>
        /// Don't assign, leave slot empty.
        /// </summary>
        Empty
    }
}
