using CMS.Core;
using CMS.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CMS.Helpers
{
    public class Result<T>
    {
        public T Data
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
                if (this.Data is IEnumerable<object>)
                {
                    return this.State == ResultState.Success && this.Data != null && ((IEnumerable<object>)((object)this.Data)).Any<object>();
                }
                return this.State == ResultState.Success && this.Data != null;
            }
        }

        public Result()
        {
            this.State = ResultState.Exception;
            this.Data = Activator.CreateInstance<T>();
        }

        public virtual Result<K> Convert<K>()
        {
            base.GetType();
            return new Result<K>
            {
                ExDetail = this.ExDetail,
                Message = this.Message,
                State = this.State
            };
        }
    }
}
