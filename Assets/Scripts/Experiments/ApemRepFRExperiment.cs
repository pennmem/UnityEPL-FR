using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApemRepFRExperiment : RepFRExperiment {
    public ApemRepFRExperiment(InterfaceManager _manager) : base(_manager) {}

    //////////
    // State Machine Constructor Functions
    //////////

    public override StateMachine GetStateMachine() {
        StateMachine stateMachine = new StateMachine(currentSession);

        // TODO: reformat
        stateMachine["Run"] = new ExperimentTimeline(
            new List<Action<StateMachine>> {
                IntroductionPrompt,
                IntroductionVideo,
                RepeatVideo,
                MicrophoneTest, // runs MicrophoneTest states
                QuitPrompt,
                Practice, // runs Practice states
                ConfirmStart,
                MainLoop, // runs MainLoop states
                FinishExperiment});

        // though it is largely the same as the main loop,
        // practice is a conceptually distinct state machine
        // that just happens to overlap with MainLoop
        stateMachine["Practice"] = new LoopTimeline(
            new List<Action<StateMachine>> {
                StartTrial,
                NextPracticeListPrompt,
                PreCountdownRest,
                CountdownVideo,
                EncodingDelay,
                Encoding,
                Rest,
                RecallPrompt,
                Recall,
                EndPracticeTrial});

        stateMachine["MainLoop"] = new LoopTimeline(
            new List<Action<StateMachine>> {
                StartTrial,
                NextListPrompt,
                PreCountdownRest,
                CountdownVideo,
                EncodingDelay,
                Encoding,
                Rest,
                RecallPrompt,
                Recall,
                EndTrial});

        stateMachine["MicrophoneTest"] = new LoopTimeline(
            new List<Action<StateMachine>> {
                MicTestPrompt,
                RecordTest,
                RepeatMicTest});

        stateMachine.PushTimeline("Run");
        return stateMachine;
    }

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



    protected void PreCountdownRest(StateMachine state) {
        int duration = (int)manager.GetSetting("preCountdownRestDuration");
        state.IncrementState();
        manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));
        ReportEvent("pre-countdown rest", null);
        SendHostPCMessage("REST", null);

        DoIn(new EventBase(() => {
            ReportEvent("pre-countdown rest end", null);
            Run();
        }), duration);
    }
}
