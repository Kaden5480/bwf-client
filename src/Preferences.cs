using MelonLoader;

namespace Bag_With_Friends {
    public class Preferences {
        private string filePath = "UserData/BagWithFriends.cfg";

        private MelonPreferences_Category server;
        public MelonPreferences_Entry<string> host { get; }
        public MelonPreferences_Entry<int> port    { get; }

        private MelonPreferences_Category dev;
        public MelonPreferences_Entry<bool> encrypted { get; }

        public Preferences() {
            this.server = MelonPreferences.CreateCategory("server");
            this.server.SetFilePath(this.filePath);

            this.host = this.server.CreateEntry<string>("host", "bwf.givo.xyz");
            this.port = this.server.CreateEntry<int>("port", 3000);

            this.dev = MelonPreferences.CreateCategory("dev");
            this.dev.SetFilePath(this.filePath);

            this.encrypted = this.dev.CreateEntry<bool>("encrypted", true);
        }

        public string GetServer() {
            if (this.encrypted.Value == true) {
                return $"wss://{this.host.Value}:{this.port.Value}";
            }

            return $"ws://{this.host.Value}:{this.port.Value}";
        }
    }
}
