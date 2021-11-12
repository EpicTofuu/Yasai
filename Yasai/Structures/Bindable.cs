﻿using System;

namespace Yasai.Structures
{
    public abstract class Bindable<T> : IBindable<T>
    {
        public event Action<T> OnSet;
        public event Action<T> OnChanged;
        public event Action OnGet;
        
        public BindStatus BindStatus { get; private set; } 
            = BindStatus.Unbound;
        
        // dependency
        private IBindable<T> dependency;
        public IBindable<T> Dependency
        {
            get => dependency;
            private set
            {
                if (value?.Dependency == this)
                   throw new InvalidOperationException("Circular bindable dependency, try using Bind instead of BindTo");
                
                dependency = value;
            }
        }
        
        // (temporary) internal value
        private T self;
        
        public T Value 
        {
            get
            {
                OnGet?.Invoke();

                if (BindStatus == BindStatus.Unbound)
                    return self;

                return Dependency.Value;
            }
            set
            {
                OnSet?.Invoke(value);
                OnChanged?.Invoke(value);
                
                switch (BindStatus)
                {
                    case BindStatus.Unidirectional:
                        throw new InvalidOperationException(
                        "Tried to set a unidirectional bindable, try unbinding first or using a bidirectional binding");
                    case BindStatus.Bidirectional:
                        if (Dependency == null)
                            self = value;
                        else
                            dependency.Value = value;
                        break;
                    case BindStatus.Unbound:
                        self = value;
                        break;
                }
            } 
        }
        
        public Bindable(T initialSelf) => self = initialSelf;
        
        public Bindable()
        { }
        
        public virtual void Bind(IBindable<T> other, bool secondary = false)
        {
            Unbind();
            
            if (!secondary)
                Dependency = other;
            
            BindStatus = BindStatus.Bidirectional;
        }

        public virtual void BindTo(IBindable<T> master)
        {
            Unbind();
            
            Dependency = master;
            BindStatus = BindStatus.Unidirectional;
        }

        public virtual void Unbind()
        {
            if (BindStatus == BindStatus.Unbound)
                return;
            
            self = Dependency.Value;
            Dependency = null;
            BindStatus = BindStatus.Unbound;
        }
    }
}