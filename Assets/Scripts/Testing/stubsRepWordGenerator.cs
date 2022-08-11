using System;
using System.Threading;

#if TESTING
public class InterfaceManager {
  public static ThreadLocal<System.Random> rnd = new ThreadLocal<System.Random>(() => new System.Random());
}

namespace Newtonsoft {
  namespace Json {
    class JsonConvert {
      public static string SerializeObject(object value) {
        return "";
      }

      public static T DeserializeObject<T>(string value) where T : class, new() {
        return new T();
      }
    }
  }
}

public static class ErrorNotification {
    public static void Notify(Exception e) {}
}
#endif
