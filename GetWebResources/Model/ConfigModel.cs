using System.Collections.Generic;

using Newtonsoft.Json;

namespace GetWebResources.Model
{
    public class ConfigModel
    {
        [JsonProperty("SavedPath")]
        public string BasePath { get; set; }
        public List<string> ContainsHostList { get; set; }

    }
}
