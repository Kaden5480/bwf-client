using MelonLoader;

namespace Bag_With_Friends {
    public class Preferences {
        private MelonPreferences_Category server;
        public MelonPreferences_Entry<string> host { get; }
        public MelonPreferences_Entry<int> port    { get; }

        public Preferences() {
            this.server = MelonPreferences.CreateCategory("server");
            this.server.SetFilePath("UserData/BagWithFriends.cfg");

            this.host = this.server.CreateEntry<string>("host", "bwf.givo.xyz");
            this.port = this.server.CreateEntry<int>("port", 3000);
        }

        public string GetServer() {
            return $"ws://{this.host.Value}:{this.port.Value}";
        }
    }
}
