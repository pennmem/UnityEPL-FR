using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using UnityEngine; // to read resource files packaged with Unity

public class ltpRepFRExperiment : RepFRExperiment {
  public ltpRepFRExperiment(InterfaceManager _manager) : base(_manager) {}

  //////////
  // State Machine Constructor Functions
  //////////

  public override Dictionary<string, List<Action>> GetStateMachine() {
    // TODO: some of these functions could be re imagined with wrappers, where the
    // state machine has functions that take parameters and return functions, such
    // as using a single function for the 'repeatlast' state that takes a prompt
    // to show or having WaitForKey wrap an action. It's not clear whether 
    // this improves clarity or reusability at all,
    // so I've deferred this. If it makes sense to do this or make more use of
    // wrapper functions that add state machine information, please do.
    Dictionary<string, List<Action>> stateMachine = base.GetStateMachine();

    stateMachine["Run"] = new List<Action> {DoIntroductionPrompt,
                                            DoIntroductionVideo,
                                            DoRepeatVideo,
                                            DoMicrophoneTest, // runs MicrophoneTest states
                                            DoRepeatMicTest,
                                            DoQuitorContinue,
                                            MainLoop, // runs MainLoop states
                                            DoFinalRecallInstructions,
                                            DoFinalRecallPrompt,
                                            DoFinalRecall,
                                            FinishExperiment};

    stateMachine["MainLoop"] = new List<Action> {DoStartTrial,
                                                 DoNextListPrompt,
                                                 DoRest,
                                                 DoCountdownVideo,
                                                 DoEncodingDelay,
                                                 DoEncoding,
                                                 DoRest,
                                                 DoRecallPrompt,
                                                 DoRecall,
                                                 DoEndTrial};

    stateMachine["MicrophoneTest"] = new List<Action> {DoMicTestPrompt,
                                                       DoRecordTest};

    return stateMachine;
  }

  //////////
  // Wait Functions
  //////////

  protected override void DoStartTrial() {
    Dictionary<string, object> data = new Dictionary<string, object>();
    data.Add("trial", state.listIndex);
    // data.Add("stim", currentSession[state.listIndex].encoding_stim);

    ReportEvent("start trial", data);

    state.mainLoopIndex++;
    var restLists = manager.GetSetting("restLists");

    if(state.listIndex == (int)manager.GetSetting("practiceLists")) {
      Do(new EventBase(DoConfirmStart));
    }
    // check if this list exists in the configuration rest list
    else if(Array.IndexOf(manager.GetSetting("restLists"), state.listIndex) != -1) {
      Do(new EventBase(DoWaitForResearcher));
    } 
    else {
      Run();
    }
  }

  protected void DoWaitForResearcher() {
    WaitForKey("participant break",
                "It's time for a short break, please " + 
                "wait for the researcher to come check on you " +
                "before continuing the experiment. \n\n" +
                "Researcher: press space to resume the experiment.", 
                PressSpace);
  }

  protected void DoFinalRecallPrompt() {
    state.runIndex++;
    base.RecallPrompt();
  }

  protected void DoFinalRecallInstructions() {
    state.runIndex++;
    WaitForKey("final recall instructions", "You will now have ten minutes to recall as many words as you can from any you studied today. As before, please say any word that comes to mind. \n\n Press Any Key to Continue.", AnyKey); 
  }

  protected void DoFinalRecall() {

    state.runIndex++;
    string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), "ffr.wav");
    FinalRecall(path);
  }
}
