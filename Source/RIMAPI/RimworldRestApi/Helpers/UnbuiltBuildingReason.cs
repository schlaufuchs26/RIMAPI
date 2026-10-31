namespace RIMAPI.Helpers
{
    // The sentence a building route answers for an id that names no built
    // building on any map (ticket #3295). The stored read lists things that
    // are not in allBuildingsColonist: a building taken apart is a
    // MinifiedThing in a stockpile, and a corpse or a stack can sit inside a
    // container. Every route answers the same reason, so a run that took an id
    // from that read learns what to do instead of reading a bare "not found".
    // Game-free, so the wording is tested without the game.
    public static class UnbuiltBuildingReason
    {
        // found is the description of what the id actually names, or null when
        // it names nothing the stored read lists; packed marks a building that
        // has to be installed first; onlyBuilt is the route's own clause about
        // what only a built building can do.
        public static string Describe(string found, bool packed, string onlyBuilt)
        {
            if (found == null)
            {
                return null;
            }
            return packed
                ? found + "; install it first, " + onlyBuilt
                : found + "; " + onlyBuilt;
        }

        // The answer a route gives when its own lookup found nothing: the
        // stored reason when the id names a held or a packed-up thing, else the
        // route's own "not found" (ticket #3354). A route that skips this drops
        // the reason the building routes carry and sends a run back to a bare
        // id it cannot act on.
        public static string OrNotFound(string reason, string notFound)
        {
            return reason ?? notFound;
        }
    }
}
