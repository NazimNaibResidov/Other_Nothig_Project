using CMS.Enums;

namespace CMS.Core
{
    public class Result
    {
        public object Data
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public ExceptionDetail ExDetail
        {
            get;
            set;
        }

        public ResultState State
        {
            get;
            set;
        }

        public bool DataReady
        {
            get
            {
                if (State == ResultState.Success)
                {
                    return Data != null;
                }
                return false;
            }
        }
    }
}
