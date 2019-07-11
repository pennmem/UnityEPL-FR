
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.Collections.Generic;

public class FlexibleConfig {
    // laod all key value pairs from a 
    // non hierarchical JSON file into 
    // a dynamically created object
    public static dynamic loadFromText(string json) {
        JObject cfg = JObject.Parse(json);
        dynamic settings = new ExpandoObject();

        foreach(JProperty prop in cfg.Properties()) {

            // convert from JObject types to .NET internal types
            // and add to dynamic settings object

            if(prop.Value is Newtonsoft.Json.Linq.JArray) {
                ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<string[]>());
                break;
            }
            else {
                ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<object>());
            }
        }

        return settings;
    }
}