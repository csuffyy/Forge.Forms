using System;
using System.Windows;

namespace Forge.Forms.DynamicExpressions
{
    /// <summary>
    /// Encapsulates a string bound to a resource.
    /// </summary>
    public class StringProxy : Freezable, IStringProxy, IProxy
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register(
                nameof(Key),
                typeof(object),
                typeof(StringProxy),
                new UIPropertyMetadata());

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(StringProxy),
                new UIPropertyMetadata(PropertyChangedCallback));

        public object Key
        {
            get => GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        object IProxy.Value => Value;

        public Action ValueChanged { get; set; }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((StringProxy)dependencyObject).ValueChanged?.Invoke();
        }

        public override string ToString()
        {
            return Value;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new StringProxy();
        }
    }
}
