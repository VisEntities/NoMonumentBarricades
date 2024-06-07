using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("No Monument Barricades", "VisEntities", "1.0.0")]
    [Description("Prevents placing barricades in certain monuments.")]
    public class NoMonumentBarricades : RustPlugin
    {
        #region Fields

        private static NoMonumentBarricades _plugin;
        private static Configuration _config;
        
        private static readonly List<string> _barricadePrefabs = new List<string>
        {
            "assets/prefabs/deployable/barricades/barricade.stone.prefab",
            "assets/prefabs/deployable/barricades/barricade.concrete.prefab",
            "assets/prefabs/deployable/barricades/barricade.cover.wood_double.prefab",
            "assets/prefabs/deployable/barricades/barricade.sandbags.prefab"
        };

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Monument Blacklist")]
            public List<string> MonumentBlacklist { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                MonumentBlacklist = new List<string>
                {
                    "sphere_tank",
                    "airfield_1",
                    "launch_site_1",
                    "trainyard_1",
                    "water_treatment_plant_1",
                    "ferry_terminal_1"
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null)
                return null;

            if (!_barricadePrefabs.Contains(prefab.fullName))
                return null;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return null;

            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                foreach (string blacklistedMonument in _config.MonumentBlacklist)
                {
                    if (monument.name.Contains(blacklistedMonument) && monument.IsInBounds(target.position))
                    {
                        SendMessage(player, Lang.CannotPlaceBarricade);
                        return true;
                    }
                }
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Localization

        private class Lang
        {
            public const string CannotPlaceBarricade = "CannotPlaceBarricade";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.CannotPlaceBarricade] = "You cannot place barricades in this monument.",
            }, this, "en");
        }

        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}