using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ltpCatRepFRExperiment : CatRepFRExperiment {
    public ltpCatRepFRExperiment(InterfaceManager _manager) : base(_manager) {}

    protected override void StartTrial(StateMachine state) {
        var data = new Dictionary<string, object>();
        data.Add("trial", state.currentSession.GetListIndex());
        data.Add("stim", currentSession.GetState().encoding_stim);

        ReportEvent("start trial", data);
        SendHostPCMessage("TRIAL", data);

        var restLists = manager.GetSetting("restLists");

        // check if this list exists in the configuration rest list
        if (Array.IndexOf(manager.GetSetting("restLists"), state.currentSession.GetListIndex()) != -1) {
            Do(new EventBase<StateMachine>(WaitForResearcher, state));
        } else {
            state.IncrementState();
            Run();
        }
    }

    //////////
    // Wait Functions
    //////////

    protected void WaitForResearcher(StateMachine state) {
        WaitForKey("participant break",
                    "It's time for a short break, please " +
                    "wait for the researcher to come check on you " +
                    "before continuing the experiment. \n\n" +
                    "Researcher: press space to resume the experiment.",
                    "space");
    }
}
