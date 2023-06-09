﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace WpfDataUi.DataTypes
{
    public class MemberCategory : INotifyPropertyChanged
    {

        #region Properties

        public string Name { get; set; }

        public Visibility Visibility
        {
            get
            {
                if (Members.Count == 0)
                {
                    return System.Windows.Visibility.Collapsed;
                }
                else
                {
                    return System.Windows.Visibility.Visible;

                }
            }
        }

        public ObservableCollection<InstanceMember> Members
        {
            get;
            private set;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public MemberCategory() 
        {
            InstantiateAll();
        }

        public MemberCategory(string name) 
        {
            InstantiateAll();
            Name = name; 
        }

        void InstantiateAll()
        {
            Members = new ObservableCollection<InstanceMember>();

            Members.CollectionChanged += HandleMembersChanged;
        }

        void HandleMembersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Visibility");
        }

        void NotifyPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return Name + " (" + Members.Count + ")";
        }

        #endregion
    }

}
