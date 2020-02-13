using System;
using UnityEngine;

public class ErrorNotification {
    public static IInterfaceManager mainThread = null;
    public ErrorNotification() {}

    public void Notify(Exception e) {
        UnityEngine.Debug.Log(e);
        if(mainThread == null) {

            throw e;
           // throw new ApplicationException("Main thread not registered to event notifier.");
        }

        mainThread.Do(new EventBase<Exception>(mainThread.Notify, e));
    }
}

public class ErrorPopup : MonoBehaviour {
    public Rect windowRect;

    void OnGUI() {

    }

}