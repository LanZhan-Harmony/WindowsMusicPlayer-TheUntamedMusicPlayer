namespace UntamedMediaPlayer.Contracts.Activation;

internal abstract class ActivationHandler<T> : IActivationHandler
    where T : class
{
    // Override this method to add the logic for whether to handle the activation.
    protected virtual bool CanHandleInternal(T args)
    {
        return true;
    }

    // Override this method to add the logic for your activation handler.
    protected abstract ValueTask HandleInternalAsync(T args);

    public bool CanHandle(object args)
    {
        return args is T arguments && CanHandleInternal(arguments);
    }

    public async ValueTask HandleAsync(object args)
    {
        await HandleInternalAsync((args as T)!);
    }
}
