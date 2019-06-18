namespace CMS.Interface
{
    public interface IPagedResult
    {
        int ActivePage
        {
            get;
            set;
        }

        int PageSize
        {
            get;
            set;
        }

        int PageCount
        {
            get;
        }

        int TotalRowCount
        {
            get;
            set;
        }

        string Title
        {
            get;
            set;
        }

        string Link
        {
            get;
            set;
        }
    }
}
