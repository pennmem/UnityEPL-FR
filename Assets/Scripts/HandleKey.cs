using System;
using System.Collections.Generic;

// public class KeyListener : EventLoop {
//     List<KeyObserver> observers = new List<KeyObserver>();
//     public void RegisterListener(KeyObserver observer) {
//         Do(() => observers.Add(observer));
//     }

//     public bool RemoveListener(KeyObserver observer) {
//         Do(() => observers.Remove(observer));
//     }

//     public void onKey(string key, bool down) {
//         foreach(KeyObserver observer in observers) {
//             Do(new EventBase<string, bool>(observer.onKey, key, down));
//         }
//     }
// }

// public class KeyObserver {
//     Dictionary<string, Action<string, bool>> callbacks;
//     EventQueue reportQueue;
//     bool enabled = true;

//     KeyObserver() {
//        callbacks = new Dictionary<string, Action<string, bool>>();
//     }

//     public void AddKey(string key, Action<string, bool> callback) {
//         callbacks[key] = callback;
//     }

//     public void onKey(string key, bool down) {
//         if(enabled && callbacks.ContainsKey(key))
//             callbacks[key].Invoke(key, down);
//     }

//     public void Enable() {
//         enabled = True;
//     }

//     public void Disable() {
//         enabled = False;
//     }

//     public void onKey(string key, bool down) {
//         callback.Invoke(key, down);
//     }
// }