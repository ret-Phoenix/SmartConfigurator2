using System.Collections.Generic;

namespace SmartConfigurator
{
    public class RegisteredShortKeys
    {
        private List<ShortKey> shortKeys;

        public RegisteredShortKeys()
        {
            shortKeys = new List<ShortKey>();

        }

        public void Add(ShortKey Skey)
        {
            shortKeys.Add(Skey);
        }

        public List<ShortKey> List()
        {
            return shortKeys;
        }
    }


    public class ShortKey
    {
        public bool Shift = false;
        public bool Alt = false;
        public bool Ctrl = false;
        public bool Win = false;
        public string Key;
        public string Command;
        public string App;
        public string Id;
    }

}
