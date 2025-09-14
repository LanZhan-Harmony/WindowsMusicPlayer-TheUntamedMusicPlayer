namespace The_Untamed_Music_Player.Activation;

public abstract class ActivationHandler<T> : IActivationHandler
    where T : class
{
    /// <summary>
    /// 重写此方法以添加是否处理激活的逻辑
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected virtual bool CanHandleInternal(T args) => true;

    /// <summary>
    /// 重写此方法以添加激活处理程序的逻辑
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected abstract Task HandleInternalAsync(T args);

    public bool CanHandle(object args) => args is T && CanHandleInternal((args as T)!);

    public async Task HandleAsync(object args) => await HandleInternalAsync((args as T)!);
}
