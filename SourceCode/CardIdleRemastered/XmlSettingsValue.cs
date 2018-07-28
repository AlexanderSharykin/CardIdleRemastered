using System;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Linq;

namespace CardIdleRemastered
{
    public interface IXmlSettings
    {
        string Key { get; }

        event EventHandler ValueChanged;

        void FromXml(XElement xe);

        XElement ToXml();
    }

    public abstract class XmlSettingsValue<T> : IXmlSettings
    {
        public string Key { get; set; }

        private T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                if (Equals(_value, value))
                    return;
                _value = value;
                OnValueChanged();
            }
        }

        public T DefaultValue { get; set; }

        public abstract void FromXml(XElement xe);

        public virtual XElement ToXml()
        {
            return new XElement(Key, Value);
        }

        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged()
        {
            if (ValueChanged != null)
                ValueChanged(this, EventArgs.Empty);
        }
    }

    public class StringSettingsValue : XmlSettingsValue<string>
    {
        public override void FromXml(XElement xe)
        {
            Value = (string)xe ?? DefaultValue;
        }

        public override XElement ToXml()
        {
            return new XElement(Key, Value ?? string.Empty);
        }
    }

    public class StringCollectionSettingsValue : XmlSettingsValue<StringCollection>
    {
        public StringCollectionSettingsValue()
        {
            Value = new StringCollection();
        }

        public override void FromXml(XElement xe)
        {
            Value.Clear();
            Value.AddRange(GetStringList(xe));
        }

        private string[] GetStringList(XElement xe)
        {
            if (xe == null)
                return new string[0];
            return xe.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public override XElement ToXml()
        {
            return new XElement(Key, string.Join(",", Value.OfType<string>()));
        }
    }

    public class IntSettingsValue : XmlSettingsValue<int>
    {
        public int Minimum { get; set; }

        public override void FromXml(XElement xe)
        {
            int? i = (int?)xe;
            Value = i.HasValue && i >= Minimum ? i.Value : DefaultValue;
        }
    }

    public class BoolSettingsValue : XmlSettingsValue<bool>
    {
        public override void FromXml(XElement xe)
        {
            bool? b = (bool?)xe;
            Value = b ?? DefaultValue;
        }
    }

    public class ByteSettingsValue : XmlSettingsValue<byte>
    {
        public byte Minimum { get; set; }

        public override void FromXml(XElement xe)
        {
            int? i = (int?)xe;
            Value = i.HasValue && i >= Minimum ? (byte)i.Value : DefaultValue;
        }
    }

    public class DoibleSettingsValue : XmlSettingsValue<double>
    {
        public double Minimum { get; set; }

        public override void FromXml(XElement xe)
        {
            double? d = (double?)xe;
            Value = d.HasValue && d >= Minimum ? d.Value : DefaultValue;
        }
    }
}
