using ReactiveUI;
using System.Reactive.Disposables;

namespace Nidikwa.GUI.ViewModels;

public static class DestroyableReactiveObjectExtensions
{
    public static T DestroyWith<T>(this T disposable, DestroyableReactiveObject destroyer) where T : IDisposable
    {
        destroyer.DisposeWhenDestroyed(disposable);

        return disposable;
    }
}
public class DestroyableReactiveObject : ReactiveObject
{
    private List<IDisposable> disposables = new ();
    private CancellationTokenSource tokenSource = new ();
    public CancellationToken Token => tokenSource.Token;
    public void DisposeWhenDestroyed(IDisposable disposable)
    {
        disposables.Add(disposable);
    }
    public void DisposeWhenDestroyed(Action disposable)
    {
        disposables.Add(Disposable.Create(disposable));
    }
    public void Destroy()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
        disposables.Clear();
        tokenSource.Cancel();
    }
}
