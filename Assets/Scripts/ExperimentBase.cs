using System.Collections;

public abstract class ExperimentBase : EventLoop {
    public ExperimentManager manager;
    public abstract void Run();

    public string experimentRoot() {
        return manager.experimentConfig.experimentRoot; 
    }

    public string experimentPath() {
        string root = experimentRoot();
        string dir = System.IO.Path.Combine(root, manager.experimentConfig.experimentName);
        return dir;
    }
    public string participantPath(string participant) {
        string dir = experimentPath();
        dir = System.IO.Path.Combine(dir, manager.experimentConfig.participant);
        return dir;
    }

    public string sessionPath(string participant, int session) {
        string dir = participantPath(participant);
        dir = System.IO.Path.Combine(dir, session.ToString() + ".session");
        return dir;
    }
}
