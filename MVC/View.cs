namespace System.Mvc
{
    public partial interface IView
    {
        void Render(object model);
        object Content { get; }
        ViewDataDictionary ViewBag { get; set; }
    }

    public interface IAsyncView
    {
    }
}
