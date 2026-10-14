namespace eCommerce.Services.Database
{
    /// <summary>
    /// Fixed primary keys of the activity types seeded with the database.
    /// Former ActivityType enum values were Feeding=0 … Adventure=4; these ids are those ordinals plus one
    /// so an existing Activities.Type column can be copied with ActivityTypeId = Type + 1.
    /// </summary>
    public static class ActivityTypeSeedIds
    {
        public const int Feeding = 1;
        public const int Safari = 2;
        public const int Workshop = 3;
        public const int Experience = 4;
        public const int Adventure = 5;
    }
}
