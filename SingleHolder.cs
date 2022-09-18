using System;

[Serializable]
public class SingleHolder<T> where T : class
{
    private T Item;

    public void Put(T item)
    {
        AT.NotNull(item);
        AT.Null(this.Item);
        this.Item = item;
    }

    public T Take()
    {
        AT.NotNull(this.Item);
        var ret = this.Item;
        this.Item = null;
        return ret;
    }
}