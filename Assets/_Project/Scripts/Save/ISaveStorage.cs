namespace RPG.Save
{
    /// <summary>
    /// Where a save lives.
    ///
    /// This interface is the cloud-migration seam the design spec asks for: SaveManager only
    /// deals in strings of JSON, so moving to a server later means writing one more
    /// implementation of this, not touching any game system.
    /// </summary>
    public interface ISaveStorage
    {
        bool Exists();
        string Read();
        void Write(string contents);
        void Delete();

        /// <summary>Human-readable location, for logs and debug UI.</summary>
        string Describe();
    }
}
