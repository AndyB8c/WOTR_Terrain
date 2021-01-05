using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Scriptables
{
    public abstract class RuntimeCollection<T> : ScriptableObject where T : Object
    {
        protected List<T> items = new List<T>();

        /// <summary>Ammount of object on the list</summary>
        public int Count => items.Count;

        public List<T> Items { get => items; set => items = value; }

        /// <summary> Clears the list of objects </summary>
        public void Clear() => items.Clear();

        /// <summary>Gets an object on the list by an index </summary>
        public T Item_Get(int index) => items[index % items.Count];

        /// <summary>Gets the first object of the list</summary>
        public T Item_GetFirst() => items[0];

        public T Item_Get(string name) => items.Find(x => x.name == name);

       


        /// <summary>Gets a rando first object of the list</summary>
        public T Item_GetRandom()
        {
            if (items != null && items.Count > 0)
            {
                var rnd = new System.Random();
                return items[rnd.Next(items.Count)];
            }
            return default;
        }

        public void Item_Add(T newItem)
        {
            if (!items.Contains(newItem))    items.Add(newItem);
        }

        public void Item_Remove(T newItem)
        {
            if (items.Contains(newItem))     items.Remove(newItem);
        }
    }

    public abstract class RegisterRunTimeCollection<T> : MonoBehaviour
    {
        public RuntimeGameObjects Collection;

        private void OnEnable() => Collection.Item_Add(gameObject);

        private void OnDisable() => Collection.Item_Remove(gameObject);
    }
}