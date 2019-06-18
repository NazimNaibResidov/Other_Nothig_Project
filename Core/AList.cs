using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace CMS.Core
{

    public class AList<T, K> where K : IComparable
    {
        public List<T> List
        {
            get;
            set;
        }

        public List<K> ClusteredKeys
        {
            get;
            set;
        }

        public string Key
        {
            get;
            set;
        }

        public Type Type
        {
            get;
            set;
        }

        public PropertyInfo KeyProp
        {
            get;
            set;
        }

        public List<T> this[K key]
        {
            get
            {
                List<T> list = new List<T>();
                int i = GetIndex(key);
                while (ClusteredKeys[i - 1].CompareTo(key) == 0)
                {
                    i--;
                }
                for (; ClusteredKeys[i].CompareTo(key) == 0; i++)
                {
                    list.Add(List[i]);
                }
                return list;
            }
        }

        public int Count => ClusteredKeys.Count;

        public AList(string key)
        {
            Key = key;
            Type = typeof(T);
            KeyProp = Type.GetProperties().FirstOrDefault((PropertyInfo x) => x.Name.ToLower() == Key.ToLower());
            if (KeyProp == null)
            {
                throw new Exception("Key Proeprty not found");
            }
            ClusteredKeys = new List<K>();
            List = new List<T>();
        }

        public void Add(T item)
        {
            K val = (K)KeyProp.GetValue(item);
            if (val != null)
            {
                int index = GetIndex(val);
                ClusteredKeys.Insert(index, val);
                List.Insert(index, item);
            }
        }

        public int GetIndex(K key)
        {
            K val = key;
            int count = ClusteredKeys.Count;
            if (count > 0)
            {
                if (count == 1)
                {
                    if (val.CompareTo(ClusteredKeys[0]) > 0)
                    {
                        return 1;
                    }
                    return 0;
                }
                int i = 0;
                int num = count;
                int num2 = (num - i) / 2;
                while (num - i > 5)
                {
                    K val2 = ClusteredKeys[num2];
                    if (val2.CompareTo(val) > 0)
                    {
                        num = num2;
                    }
                    else
                    {
                        if (val2.CompareTo(val) >= 0)
                        {
                            return num2;
                        }
                        i = num2;
                    }
                    num2 = i + (num - i) / 2;
                    if (num2 == num || num2 == i)
                    {
                        return num2;
                    }
                }
                for (; i < num && ClusteredKeys[i].CompareTo(val) < 0; i++)
                {
                }
                return i;
            }
            return 0;
        }

        public int GetIndex(T item)
        {
            K key = (K)KeyProp.GetValue(item);
            return GetIndex(key);
        }
    }

}
