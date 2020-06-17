using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

//////////
// Classes to manage the filesystem in
// which experiment data is stored
/////////

public class FileManager {

    InterfaceManager manager;

    public FileManager(InterfaceManager _manager) {
        manager = _manager;
    }

    public virtual string ExperimentRoot() {

        #if UNITY_EDITOR
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        #else
            return System.IO.Path.GetFullPath(".");
        #endif
    }

    public string ExperimentPath() {
        string root = ExperimentRoot();
        string experiment;
        
        try {
            experiment = (string)manager.GetSetting("experimentName");
        }
        catch(MissingFieldException) {
            manager.Notify(new Exception("No experiment selected"));
            return null;
        }

        string dir = System.IO.Path.Combine(root, "data", experiment);

        return dir;
    }
    public string ParticipantPath(string participant) {
        string dir = ExperimentPath();
        dir = System.IO.Path.Combine(dir, participant);
        return dir;
    }

    public string ParticipantPath() {
        string dir = ExperimentPath();
        string participant;

        try{
            participant = (string)manager.GetSetting("participantCode");
        }
        catch(MissingFieldException) {
            manager.Notify(new Exception("No participant selected"));
            return null;
        }

        dir = System.IO.Path.Combine(dir, participant);
        return dir;
    }
    
    public string SessionPath(string participant, int session) {
        string dir = ParticipantPath(participant);
        dir = System.IO.Path.Combine(dir, "session_" + session.ToString());
        return dir;
    }

    public string SessionPath() {
        string session;
        try{
            session = (string)manager.GetSetting("session");
        }
        catch(MissingFieldException) {
            return null;
        }

        string dir = ParticipantPath();
        dir = System.IO.Path.Combine(dir, "session_" + session.ToString());
        return dir;
    }

    public bool isValidParticipant(string code) {
        if((bool)manager.GetSetting("isTest")) {
            return true;
        }

        string experiment, prefix;
        try{
            experiment = (string)manager.GetSetting("experimentName");
            prefix = (string)manager.GetSetting("prefix");
        }
        catch(MissingFieldException) {
            return false;
        }

        Regex rx = new Regex(@"^" + prefix + @"\d{1,4}[A-Z]?$");

        return rx.IsMatch(code);
    }

    public string GetWordList() {
        string root = ExperimentRoot();
        return System.IO.Path.Combine(root, (string)manager.GetSetting("wordpool"));
    }

    public void CreateSession() {
        Directory.CreateDirectory(SessionPath());
    }

    public void CreateParticipant() {
        Directory.CreateDirectory(ParticipantPath());
    }
    public void CreateExperiment() {
        Directory.CreateDirectory(ExperimentPath());
    }

    public string ConfigPath() {
        string root = ExperimentRoot();
        return System.IO.Path.Combine(root, "configs");
    }

    public int CurrentSession(string participant) {
        int nextSessionNumber = 0;
        Debug.Log(SessionPath(participant, nextSessionNumber));
        while (System.IO.Directory.Exists(SessionPath(participant, nextSessionNumber)))
        {
            nextSessionNumber++;
        }
        return nextSessionNumber;
    }
}