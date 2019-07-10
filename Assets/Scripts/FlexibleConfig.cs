
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.Collections.Generic;

public class FlexibleConfig {
    public static dynamic loadFromText(string json) {
        JObject cfg = JObject.Parse(json);
        dynamic settings = new ExpandoObject();

        foreach(JProperty prop in cfg.Properties()) {
            Type valueType = prop.Value.GetType();
            ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<object>());
        }

        return settings;
    }
}