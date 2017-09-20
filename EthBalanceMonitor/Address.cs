using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EthBalanceMonitor
{
    public class Address : INotifyPropertyChanged
    {
        string _Addr;
        public string Addr
        {
            get { return _Addr; }
            set
            {
                _Addr = value;
                this.NotifyPropertyChanged();
            }
        }

        bool _AuditPassed;
        public bool AuditPassed
        {
            get { return _AuditPassed; }
            set
            {
                _AuditPassed = value;
                this.NotifyPropertyChanged();
            }
        }

        float _BalEth;
        public float BalEth
        {
            get { return _BalEth; }
            set
            {
                _BalEth = value;
                this.NotifyPropertyChanged();
            }
        }

        float _BalTnt;
        public float BalTnt
        {
            get { return _BalTnt; }
            set
            {
                _BalTnt = value;
                this.NotifyPropertyChanged();
            }
        }
        DateTime _LastUpdated;
        public DateTime LastUpdated
        {
            get { return _LastUpdated; }
            set
            {
                _LastUpdated = value;
                this.NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Address(string addr)
        {
            Addr = addr;
        }
    }
}
