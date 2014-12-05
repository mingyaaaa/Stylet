﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Stylet
{
    /// <summary>
    /// Represents a collection which is observasble
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObservableCollection<T> : IList<T>, INotifyPropertyChanged, INotifyCollectionChanged, INotifyPropertyChangedDispatcher
    {
        /// <summary>
        /// Add a range of items
        /// </summary>
        /// <param name="items">Items to add</param>
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Remove a range of items
        /// </summary>
        /// <param name="items">Items to remove</param>
        void RemoveRange(IEnumerable<T> items);
    }

    /// <summary>
    /// Interface encapsulating IReadOnlyList and INotifyCollectionChanged
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyObservableCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChangedDispatcher
    {
    }

    /// <summary>
    /// ObservableCollection subclass which supports AddRange and RemoveRange
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BindableCollection<T> : ObservableCollection<T>, IObservableCollection<T>, IReadOnlyObservableCollection<T>
    {
        private Action<Action> _propertyChangedDispatcher = Execute.DefaultPropertyChangedDispatcher;
        /// <summary>
        /// Dispatcher to use when firing events. Defaults to BindableCollection.DefaultPropertyChangedDispatcher
        /// </summary>
        public Action<Action> PropertyChangedDispatcher
        {
            get { return this._propertyChangedDispatcher; }
            set { this._propertyChangedDispatcher = value; }
        }

        private Action<Action> _collectionChangedDispatcher = Execute.DefaultCollectionChangedDispatcher;

        /// <summary>
        /// Dispatcher to use when firing CollectionChanged events. Defaults to BindableCollection.DefaultCollectionChangedDispatcher
        /// </summary>
        public Action<Action> CollectionChangedDispatcher
        {
            get { return this._collectionChangedDispatcher; }
            set { this._collectionChangedDispatcher = value; }
        }

        /// <summary>
        ///  We have to disable notifications when adding individual elements in the AddRange and RemoveRange implementations
        /// </summary>
        private bool isNotifying = true;

        /// <summary>
        /// Create a new empty BindableCollection
        /// </summary>
        public BindableCollection() : base() { }

        /// <summary>
        /// Create a new BindableCollection with the given members
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied</param>
        public BindableCollection(IEnumerable<T> collection) : base(collection) { }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected override event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyCollectionChanged"/>
        /// </remarks>
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises the System.Collections.ObjectModel.ObservableCollection{T}.PropertyChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            // Avoid doing a dispatch if nothing's subscribed....
            if (this.isNotifying && this.PropertyChanged != null)
            {
                this.PropertyChangedDispatcher(() =>
                {
                    var handler = this.PropertyChanged;
                    if (handler != null)
                        handler(this, e);
                });
            }
        }

        /// <summary>
        /// Raises the System.Collections.ObjectModel.ObservableCollection{T}.CollectionChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // Avoid doing a dispatch if nothing's subscribed....
            if (this.isNotifying && this.CollectionChanged != null)
            {
                this.PropertyChangedDispatcher(() =>
                {
                    var handler = this.CollectionChanged;
                    if (handler != null)
                    {
                        using (this.BlockReentrancy())
                        {
                            handler(this, e);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Add a range of items
        /// </summary>
        /// <param name="items">Items to add</param>
        public virtual void AddRange(IEnumerable<T> items)
        {
           var previousNotificationSetting = this.isNotifying;
            this.isNotifying = false;
            var index = Count;
            foreach (var item in items)
            {
                this.InsertItem(index, item);
                index++;
            }
            this.isNotifying = previousNotificationSetting;
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            // Can't add with a range, or it throws an exception
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Remove a range of items
        /// </summary>
        /// <param name="items">Items to remove</param>
        public virtual void RemoveRange(IEnumerable<T> items)
        {
            var previousNotificationSetting = this.isNotifying;
            this.isNotifying = false;
            foreach (var item in items)
            {
                var index = IndexOf(item);
                if (index >= 0)
                    this.RemoveItem(index);
            }
            this.isNotifying = previousNotificationSetting;
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            // Can't remove with a range, or it throws an exception
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Raise a change notification indicating that all bindings should be refreshed
        /// </summary>
        public void Refresh()
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
