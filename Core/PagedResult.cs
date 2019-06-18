using CMS.Helpers;
using CMS.Interface;

namespace CMS.Core
{


    public class PagedResult<T> : Result<T>, IPagedResult
    {
        public int ActivePage
        {
            get;
            set;
        }

        public int PageSize
        {
            get;
            set;
        }

        public int PageCount
        {
            get
            {
                if (PageSize == 0)
                {
                    return 0;
                }
                double num = (double)TotalRowCount / (double)PageSize;
                double num2 = num;
                if (num2 > (double)(int)num2)
                {
                    num += 1.0;
                }
                return (int)num;
            }
        }

        public int TotalRowCount
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Link
        {
            get;
            set;
        }

        public PagedResult()
        {
            ActivePage = 1;
            PageSize = 30;
        }

        public PagedResult<K> PagedConvert<K>()
        {
            return new PagedResult<K>
            {
                ActivePage = ActivePage,
                ExDetail = base.ExDetail,
                Link = Link,
                Message = base.Message,
                PageSize = PageSize,
                State = base.State,
                Title = Title,
                TotalRowCount = TotalRowCount
            };
        }
    }
}
