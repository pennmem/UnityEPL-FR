using System.Collections;

public abstract class ExperimentBase : EventLoop {
    public ExperimentManager manager;

    public ExperimentBase(ExperimentManager _manager) {
        manager = _manager;
    }
    public abstract void Run();
}
