using ICities;

namespace EnhancedGarbageTruckAI
{
    public class Identity : IUserMod
    {
        public string Name
        {
            get { return Settings.Instance.Tag; }
        }

        public string Description
        {
            get { return "Oversees trash services to ensure that garbage trucks are dispatched in an efficient manner."; }
        }
    }
}