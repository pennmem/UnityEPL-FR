
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.Collections.Generic;
using UnityEngine;

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
            // if JSON contains arrays, we need to peek at the
            // type of the contents to get the right cast, as
            // C# doesn't implicitly cast the contents of an
            // array when casing the array

            if(prop.Value is Newtonsoft.Json.Linq.JArray) {
                JTokenType jType = JTokenType.None;

                foreach(JToken child in prop.Value.Children()) {
                    Debug.Log((int)child.Type);
                    if(jType == JTokenType.None) {
                        jType = child.Type;
                    }
                    else if (jType != child.Type) {
                        throw new Exception("Mixed type arrays not supported");     
                    }
                }

                Type cType = JTypeConversion((int)jType);
                if(cType  == typeof(string)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<string[]>());
                }
                else {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<object[]>());
                }
            }
            else {
                ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<object>());
            }
        }

        return settings;
    }

    public static Type JTypeConversion(int t) {
        switch(t) {
            case 6:
                return typeof(int);
            case 7:
                return typeof(float);
            case 8:
                return typeof(string);
            case 9: 
                return typeof(bool);
            default:
                throw new Exception("Unsupported Type");
        }
    }
}   
